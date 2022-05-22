using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TPlane {
    public GameObject gobject;
    public Vector3 m_point;
    public Vector3 m_normal;

    public float d_m => Vector3.Dot(m_point, m_normal);

    public TPlane(GameObject gobject, Vector3 m_point, Vector3 m_normal) {
        this.gobject = gobject;
        this.m_point = m_point;
        this.m_normal = m_normal;
    }

    public Vector3 Point() {
        return m_normal * d_m;
    }

    public Vector3 SNormal() {
        return m_normal * d_m;
    }
}

public class TriplePlaneIntersection : MonoBehaviour {

    [SerializeField] private Transform[] prim_planes; 

    private TPlane[] t_planes;
    private TPlane[] planes {
        get {
            if(t_planes == null)
                t_planes = new TPlane[3];

            return t_planes;
        }
    }

    void OnDrawGizmos() {
        for(int i = 0;i < prim_planes.Length && i < planes.Length && prim_planes[i] != null;i++) {
            planes[i] = new TPlane(prim_planes[i].gameObject, prim_planes[i].transform.position, prim_planes[i].transform.up);
        }

        Gizmos.color = Color.white;
        for(int i = 0;i < planes.Length;i++) {
            DrawPlane(planes[i]);
        }

// two plane check
        Vector3 v = planes[1].Point() - planes[0].Point();
        Vector3 x = v - Vector3.Project(v, planes[1].SNormal());

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(planes[0].Point(), v);
        Gizmos.DrawRay(planes[1].Point(), x);
    }

    void DrawPlane(TPlane p) {
        if(!p.gobject.activeInHierarchy)
            return;

        Gizmos.matrix = Matrix4x4.Translate(p.Point()) * Matrix4x4.Rotate(Quaternion.LookRotation(p.Point()));
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(5F, 1e-3f, 5F));
        Gizmos.DrawWireSphere(Vector3.zero, 0.125F);
    }
}