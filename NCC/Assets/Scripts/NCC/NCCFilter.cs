using System;
using UnityEngine;

// simple stateless filters used by NCC
public class NCCFilter {

    // filter self (overlap)
    public static int OverlapFilterSelf(int cnt, Collider self, Collider[] cbuf) {
        for(int i = cnt - 1; i >= 0;i--) {
// are we ourself?
            if(cbuf[i] == self) {
                cnt--;
// i is now below cnt, swap it with last entry 
                if(i < cnt)
                    cbuf[i] = cbuf[cnt];
            }else {
                continue;
            }
        }

        return cnt;
    }

// filter self
    public static int TraceFilterSelf(int cnt, Collider self, RaycastHit[] hbuf) {
        for(int i = cnt - 1; i >= 0;i--) {
// are we ourself?
            if(hbuf[i].collider == self) {
                cnt--;
// i is now below cnt, swap it with last entry 
                if(i < cnt)
                    hbuf[i] = hbuf[cnt];
            }else {
                continue;
            }
        }

        return cnt;
    }

// filter invalids and return index of closest hit 
    public static int FindClosest(int cnt, RaycastHit[] hbuf) {
        var min = float.MaxValue;
        int i0 = -1;
        for(int i = cnt - 1; i >= 0;i--) {
            float dist = hbuf[i].distance;
            if(dist > 0) {
                if(dist < min) {
                    min = dist;
                    i0 = i;
                }
            }
        }

        return i0;
    }

// find all distances that are relatively close to this distance and append them to clip list
    public static int ClipNearest(int i0, int cnt, ClipHull chull, RaycastHit[] hbuf) {
        var min = hbuf[i0].distance;
        var eps = 1e-3f;
        var n = 0;

        for(int i = cnt -1;i>=0;i--) {
            if(i == i0)
                continue;
            
            var dist = hbuf[i].distance;
            if(dist >= 0 && Mathf.Abs(min - dist) < eps) {
                chull.AppendHit(in hbuf[i]);
                n++;
            }
        }

        return n;
    }

// cache all triggers while removing them from the main hits buffer
    public static int TraceFilterTriggers(int cnt, NCCBuffer nb, RaycastHit[] hits, bool incl= false) {
        for(int i = cnt - 1; i>=0; i--) {
            var o = hits[i].collider;
            if(o.isTrigger) {
                cnt--;

// only append to the alternative hits buffer if specified
                if(incl)
                    nb.AddTrigger(hits[i]);

                if(i < cnt) {
                    hits[i] = hits[cnt];
                }
            }
        }

        return cnt;
    }
}