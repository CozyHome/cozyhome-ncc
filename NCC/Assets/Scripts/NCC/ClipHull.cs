using System.Collections.Generic;
using UnityEngine;

public class ClipHull {
    private const int MAXCLIPS = 3; 
    private const float v_eps = 1e-8f;
    private const float d_eps = 1e-5f;

// all elements below the indexer 'total' are obstructive clips.
    private List<NClip> clist;

// this integer is valuable as it gives an offset along the normals list
// to skip already clipped normals. We don't want to continually clip against
// old normals as it will corrupt our velocity vector.
    private int total;

    public ClipHull(List<NClip> clist) {
        this.clist = clist;
        this.Clear();
    }

// returns a vector that is clipped along all valid clips found in clip list
// the key here is that the order in which you read your planes matters. We cannot assume
// that our clipper will work first try.
// n := # of clips, t := # of total iterations, i := # of sub-iterations
    public Vector3 ClipVector(Vector3 v) {
        int n = clist.Count; // total clips in list
        int t = total; // total obstructive clips
        int i = 0; // sub-iterator
        int x = 0; // iterations taken (useful for debugging)

        do {
            for(i = t;i < n;i++) {
                x++;
                NClip c = Get(i);

                if(!c.clipped && Vector3.Dot(v, c.normal) <= d_eps) {
                    c.clipped = true; 
                    Set(i, c);
                    Swap(t++, i);
                    v = Clip(v, t);
                    break; 
                }

            }
        } while(i < n && v.sqrMagnitude > v_eps);

        total = t;
        return v.sqrMagnitude < v_eps ? Vector3.zero : v;
    }

    public void Set(int i, NClip c) {
        clist[i] = c;
    }

    public NClip Get(int i) {
        return clist[i];
    }

    public void Draw(Vector3 pos, Color c) {
        int n = clist.Count;
        for(int i = 0;i < n;i++) {
            Debug.DrawRay(pos, this.Get(i).normal, c);
        }
    }

// insert raycasthit into clip list
    public void AppendHit(in RaycastHit hit) {
        InsertClip(new NClip(hit.point, hit.normal, hit.collider, hit.distance));
    }

    public void AppendHit(RaycastHit hit) {
        InsertClip(new NClip(hit.point, hit.normal, hit.collider, hit.distance));
    }

// insert an overlap penetration into clip list
    public void AppendOverlap(Vector3 position, Vector3 normal, Collider collider, float d) {
        InsertClip(new NClip(position, normal, collider, d));
    }

// trims the fat (useless clips that were not discovered during a prior clipvector call)
    public void Trim() {
        for(int i = clist.Count - 1;i >= total;i--) {
            clist.RemoveAt(i);
        }
    }

    public void Clear() {
        this.clist.Clear();
        this.total = 0;
    }

// secret method that handles insertion
    private void InsertClip(NClip c) {
// additional operation to prevent semi-parallel planes from being
// inserted into the system. It's O(n) but the working set is usually
// small enough that it isn't a problem.
        for(int i = 0;i < clist.Count;i++) {
            if(Vector3.Dot(clist[i].normal, c.normal) > 1 - 1e-3f)
                return;
        }

        this.Add(c);
    }

// swaps clips around in order for Clip(...) to operate on the right directional information
    private void Swap(int i1, int i2) {
        if(i1 == i2)
            return;
        else {
            NClip c = this.Get(i1);
            Set(i1, this.Get(i2));
            Set(i2, c);
        }
    }

    private void Add(NClip c) {
        clist.Add(c);
    }

    private Vector3 Clip(Vector3 v, int n) {
        switch(n) {
            case 1: // singular plane clip
                v = v - Vector3.Project(v, Get(0).normal);
                return v;
            case 2: // crease clip
                Vector3 c2 = Vector3.Cross(Get(0).normal, Get(1).normal);
                c2.Normalize();
                return Vector3.Project(v, c2);
            case 3:
            default: // corner clip
                return Vector3.zero;
        }
    }

    public Vector3 Orient(Vector3 v, Vector3 g, Vector3 u) {
        Vector3 R = Vector3.Cross(v, u);
        if(R.sqrMagnitude > 0) {
            R.Normalize();
            Vector3 V = Vector3.Cross(g, R);
            if(V.sqrMagnitude > 0) {
                v = V * v.magnitude / V.magnitude;
            } 
        }

        return v;
    }

    public int GetCount() {
        return clist.Count;
    }

    public int GetClipState() {
        return total;
    }
}
