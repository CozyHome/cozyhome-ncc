using UnityEngine;

public struct NCCMove {
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 ext;
    
    public readonly Quaternion rot;
    public readonly Collider self;

    public readonly int mask;
    public readonly int flags;

    public readonly float stepheight;
    public readonly float stableangle;
    public readonly float dt;

// default ctor
    public NCCMove(Vector3 pos, Vector3 vel, Vector3 scl, Quaternion rot, Collider self) {
        this.pos = pos;
        this.vel = vel;
        this.ext = scl / 2F;
        this.rot = rot;
        this.self = self;

        this.mask = (1 << 0);
        this.flags = NCC.FLG_ALL;

        this.dt = Time.fixedDeltaTime;
        this.stepheight = NCC.DEF_STP_HEIGHT;
        this.stableangle = NCC.DEF_STBL_ANGLE;
    }

// transform ctor
    public NCCMove(Transform t, BoxCollider box, Vector3 vel, int mask, int flags, float dt, float stepheight = NCC.DEF_STP_HEIGHT, float stableangle = NCC.DEF_STBL_ANGLE) {
        this.pos = t.position;
        this.rot = t.rotation;
        this.ext = Vector3.Scale(box.size, t.localScale) / 2F;
        this.vel = vel;
        this.self = box;

        this.mask = mask;
        this.flags = flags;

        this.dt = dt;
        this.stepheight = stepheight;
        this.stableangle = stableangle;
    }

// verbose ctor
    public NCCMove(Vector3 pos, Vector3 vel, Vector3 scl, Quaternion rot, Collider self, 
                    int mask, int flags, 
                    float dt, float stepheight = NCC.DEF_STP_HEIGHT, float stableangle = NCC.DEF_STBL_ANGLE) {
        this.pos = pos;
        this.vel = vel;
        this.ext = scl / 2F;
        this.rot = rot;
        this.self = self;

        this.mask = mask;
        this.flags = flags;
        
        this.dt = dt;
        this.stepheight = stepheight;
        this.stableangle = stableangle;
    }
}

public struct NCCGround {
    public readonly bool           valid;
    public readonly float       distance;
    public readonly Vector3        point;
    public readonly Vector3       normal;
    public readonly Collider    collider;

    public NCCGround(bool valid, Vector3 point, Vector3 normal, float distance, Collider collider) {
        this.valid = valid;
        this.point = point;
        this.normal = normal;
        this.distance = distance;
        this.collider = collider;
    }
}

public enum ClipType {
    Overlap = 0,
    Trace = 1
}

public interface NCCRelay {
    void Clip(in NCCMove m, ClipType t, NClip c);
    void Trigger(in NCCMove m, NClip c);
}

public struct NClip {
    public Vector3  point;
    public Vector3  normal;
    public Collider collider;
    public float distance;
    public bool clipped;

    public NClip(Vector3 point, Vector3 normal, Collider collider, float distance) {
        this.point = point;
        this.normal = normal;
        this.collider = collider;
        this.distance = distance;
        this.clipped = false;
    }
}
