using UnityEngine;

// data class for our solver to keep track of all clipped planes
public struct Clip {
    public Vector3  point;
    public Vector3  normal;
    public Collider collider;
    public float distance;
    public bool clipped;

    public Clip(Vector3 point, Vector3 normal, Collider collider, float distance) {
        this.point = point;
        this.normal = normal;
        this.collider = collider;
        this.distance = distance;
        this.clipped = false;
    }
}
