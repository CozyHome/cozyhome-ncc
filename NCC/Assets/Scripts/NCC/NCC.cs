using UnityEngine;

public class NCC {
    public const int FLG_DOSNAP   = 0x1; // don't snap if not desireable
    public const int FLG_DOSTEP   = 0x2; // don't step if not desireable
    public const int FLG_DOGROUND = 0x4; // skip ground check if not needed
    public const int FLG_ALL = FLG_DOSNAP | FLG_DOSNAP | FLG_DOGROUND;

    public const float DEF_STP_HEIGHT = 0.6F;
    public const float DEF_STBL_ANGLE = 45F;

    private const float m_offs = 1e-2f / 2F;

    public static NCCMove Move(NCCMove m, NCCBuffer nb, NCCRelay re) {
        Slide(ref m, nb, re);
        return m;
    }

// collide & slide
    private static void Slide(ref NCCMove m, NCCBuffer nb, NCCRelay re) {
        ClipHull hull = nb.Clips;
        hull.Clear();

        var hits = nb.Hits;
        var cols = nb.Colliders;

// resolve pushbacks
        OverlapBox(ref m, cols, ref hull, re);

        Vector3 old_vel = m.vel;
        m.vel = hull.ClipVector(m.vel);
        hull.Trim();

// report all overlap pushbacks
        for(int i = 0; i < hull.GetCount();i++) {
            re.Clip(in m, ClipType.Overlap, hull.Get(i));
        }

// trace primitive
        HullTrace(ref m, nb, re);
    }

    private static void HullTrace(ref NCCMove m, NCCBuffer nb, NCCRelay re) {
        const float bvel     = 1e-8f;
        const int max_bumps  = 8;

// buffers & other shit 
        var hull = nb.Clips;
        var hits = nb.Hits;
        var cols = nb.Colliders;
        var box  = m.self as BoxCollider;
        var ahits = nb.AHits;

        var grnd = (m.flags & FLG_DOGROUND) != 0;
        var snap = (m.flags & FLG_DOSNAP)   != 0;
        var step = (m.flags & FLG_DOSTEP)   != 0;

// ground trace
        if(grnd) {
            SnapTrace(ref m, hull, nb, ahits, snap);
// clear out existing ground buffer for this frame
        }else {
            nb.SetGround(new NCCGround(false, Vector3.zero, Vector3.zero, 0F, null));
        }

// limit velocity to grounding plane (can either use cross projection or just clipping)
        if(snap && nb.Ground.valid) {
            float v = m.vel.magnitude;
            m.vel -= Vector3.Project(m.vel, nb.Ground.normal);
            if(m.vel.sqrMagnitude > 0) {
                m.vel *= v / m.vel.magnitude;
            }
        }
        
// heuristics
        int numbumps = 0;
        float tl = m.vel.magnitude * m.dt;
        Vector3 t_dir = m.vel;

// trace slide
        while(tl > 0 && numbumps++ < max_bumps && t_dir != Vector3.zero) {
            nb.ResetTriggers();
            float tr = tl + 2F * m_offs;

            t_dir.Normalize();
            int n = Physics.BoxCastNonAlloc(m.pos, m.ext, t_dir, hits, m.rot, tr, m.mask,
                QueryTriggerInteraction.Collide
            );

            n = NCCFilter.TraceFilterSelf(n, m.self, hits);
            n = NCCFilter.TraceFilterTriggers(n, nb, hits);
            int i0 = NCCFilter.FindClosest(n, hits);

            var numtriggers = nb.GetTriggerCount();
            for (int i = 0; i < numtriggers; i++) {
                var thit = ahits[i];
                re.Trigger(in m, new NClip(thit.point, thit.normal, thit.collider, thit.distance));
            }

            if (i0 < 0) {
                m.pos = m.pos + t_dir * tl;
                break;
            }

// movement
// instead of 1. moving back along vector line with m_offs, or 2. moving back along normal of surface by m_offs:
// we combine these two approaches: we move backwards along the vector line such that the orthogonal distance to the plane is m_offs.
// this way we ensure we are m_offs units away from the surface while also not resulting in a protrusion into other nearby geometry
// as the vector we have traced ensures a valid path we can sweep along without intersecting other nearby geometry.
            RaycastHit cl = hits[i0];
            Vector3 np = Traceback(m.pos, t_dir, cl.distance, cl.normal);
            tl = tl - (Vector3.Distance(np, m.pos) + cl.distance);
            m.pos = np;

            if (step && StepTrace(ref m, ahits, cl.normal)) {
                continue;
            }

// traverse from [old_len, cur_count) to notify the relay of all newly discovered clips
            int old_len = hull.GetCount();

            NCCFilter.ClipNearest(i0, n, hull, hits);
            hull.AppendHit(cl);

// convert all clips into ground clips (normals are subspace of ground plane)
            for (int i = old_len; i < hull.GetCount(); i++) {
                NClip c = hull.Get(i);
                if (snap && nb.Ground.valid && !DetermineTraceStability(m.stableangle, c.normal, nb.Ground.normal)) {
                    c.normal = c.normal - Vector3.Project(c.normal, nb.Ground.normal);
                    c.normal.Normalize();
                    hull.Set(i, c);
                }

                re.Clip(in m, ClipType.Trace, hull.Get(i));
            }

            m.vel = hull.ClipVector(m.vel);
            m.vel += hull.Get(hull.GetCount() - 1).normal * bvel;
            t_dir = m.vel;
        }
    }

// used to push the primitive into the closest obstruction, as well as back out 'x' units provided
// by m_offs
    private static Vector3 Traceback(Vector3 pos, Vector3 tdir, float dist, Vector3 ndir) {
        pos = pos + tdir * dist;
        Vector3 np = pos + ndir * m_offs;
        pos = pos - tdir * Vector3.Dot(np - pos, ndir);
        return pos;
    }

// snapping subroutine of HullTrace
    private static void SnapTrace(ref NCCMove m, ClipHull hull, NCCBuffer nb, RaycastHit[] ahits, bool snap) {
        float min_sdist = nb.LastGround.valid ? 0.6F : 0.3F;
        float tr = min_sdist + 2 * m_offs;
        int numbumps = 0;
        Vector3 spos = m.pos;
        Vector3 gdir = m.rot * new Vector3(0, -1, 0);
        Vector3 up   = -gdir;

// clip vector along all blocking planes until tr is either zero or we've bounced twice
        while(numbumps++ < 2 && tr > 0) {
            int n = Physics.BoxCastNonAlloc(spos, m.ext, gdir, ahits, m.rot, tr, m.mask,
                QueryTriggerInteraction.Ignore
            );
        
            n = NCCFilter.TraceFilterSelf(n, m.self, ahits);
            int i0 = NCCFilter.FindClosest(n, ahits);

            if(i0 >= 0) {
                RaycastHit hit = ahits[i0];
                Vector3 np = Traceback(spos, gdir, hit.distance, hit.normal);
                tr = tr - (Vector3.Distance(np, spos) + hit.distance);
                spos = np;

                if(!DetermineTraceStability(m.stableangle, hit.normal, up)) {
                    gdir = gdir - Vector3.Project(gdir, hit.normal);
                    gdir.Normalize();

// remember nearby blocking surface geometry
                    hull.AppendHit(hit);
                }else {

// only snap if provided by the args flags
                    if(snap) {
                        m.pos = spos;
                    }

                    nb.SetGround(new NCCGround(true, hit.point, hit.normal, hit.distance, hit.collider));
                    return;
                }
            }else {
                break;
            }
        }

        nb.SetGround(new NCCGround(false, Vector3.zero, Vector3.zero, 0f, null));
    }

    private static bool DetermineTraceStability(float angle, Vector3 normal, Vector3 up) {
        return Vector3.Angle(normal, up) < angle;
    }

// stepping subroutine of HullTrace
    private static bool StepTrace(ref NCCMove m, RaycastHit[] ahits, Vector3 sn) {
        const float height = 0.6F;
        const float aux_d = 2e-2f;
        const float min_h = 1e-2f; 

        Vector3 spos       = m.pos;
        Vector3 up         = m.rot * new Vector3(0, 1, 0);

// stable normals are ignored
        if(DetermineTraceStability(m.stableangle, sn, up))
            return false;

// trace upward for ceilings
        int n = Physics.BoxCastNonAlloc(spos, m.ext, up, ahits, m.rot, height + 2F * m_offs, m.mask,
            QueryTriggerInteraction.Ignore
        );

        n = NCCFilter.TraceFilterSelf(n, m.self, ahits);
        int i0 = NCCFilter.FindClosest(n, ahits);

        if(i0 >= 0) {
            spos = Traceback(spos, up, ahits[i0].distance, ahits[i0].normal);
        }else {
            spos += up * height;
        }

// trace downwards for step surfaces
        n = Physics.BoxCastNonAlloc(spos - sn * aux_d, m.ext, -up, ahits, m.rot, height + 2F * m_offs, m.mask,
            QueryTriggerInteraction.Ignore
        );

        n = NCCFilter.TraceFilterSelf(n, m.self, ahits);
        i0 = NCCFilter.FindClosest(n, ahits);

// hit surface geometry (ground?)
        if(i0 < 0)
            return false;

        spos = Traceback(spos, -up, ahits[i0].distance, ahits[i0].normal);
        float hdot = Vector3.Dot(spos - m.pos, up);

        if(hdot < min_h || !DetermineTraceStability(m.stableangle, ahits[i0].normal, up)) {
            return false;
        }
        
// trace forwards to blocking plane for step
        n = Physics.BoxCastNonAlloc(m.pos + up * hdot, m.ext, -sn, ahits, m.rot, aux_d + 2F * m_offs, m.mask,
            QueryTriggerInteraction.Ignore
        );

        n = NCCFilter.TraceFilterSelf(n, m.self, ahits);
        i0 = NCCFilter.FindClosest(n, ahits);

        if(i0 >= 0) {
            if(!DetermineTraceStability(m.stableangle, ahits[i0].normal, up)) {
                return false;
            }
        }else {
            spos = spos - sn * aux_d;
        }

        m.pos = spos;
        return true;
    }

    private static void OverlapBox(ref NCCMove m, Collider[] cols, ref ClipHull hull, NCCRelay re) {
        int i = Physics.OverlapBoxNonAlloc(m.pos, m.ext, cols, m.rot, m.mask,
            QueryTriggerInteraction.Collide
        );
    
        int res = NCCFilter.OverlapFilterSelf(i, m.self as BoxCollider, cols);
        if(res > 0) {
            ResolveIntersections(ref m, res, ref hull, cols, re);
        }
    }

// i assume unity uses some form of "GJK-EPA"-esque closest distance vector algorithm. It's not very useful outside of
// gathering whether we are already inside blocking geometry in most cases. This is usually the last place you want your solver
// to be as it means you didn't prevent a collision. -DC @ April 18th, 2021.
    private static void ResolveIntersections(ref NCCMove m, int res, ref ClipHull hull, Collider[] buf, NCCRelay re) {
        Vector3 ClosestPoint(Vector3 pos, Collider self, Collider other) {
            if(!(other is MeshCollider)) {
                return other.ClosestPoint(pos);
            }else {
                var m = other as MeshCollider;
                if(m.convex) {
                    return other.ClosestPoint(pos);
                }else {
                    return pos;
                }
            }
        }
        
        Transform s_t = m.self.GetComponent<Transform>();
        for(int i = 0;i < res;i++) {
            Collider c = buf[i];
            Transform c_t = c.GetComponent<Transform>();

            bool pen = Physics.ComputePenetration(
                m.self, m.pos, m.rot, // object A (us)
                c, c_t.position, c_t.rotation, // object B (blocking geometry)
                out Vector3 sep,
                out float dist
            );

            if(pen) {
                if(c.isTrigger) {
                    re.Trigger(in m, new NClip(ClosestPoint(m.pos, m.self, c), sep, c, 0F));
                }else {
                    m.pos += sep * dist;
                    hull.AppendOverlap(ClosestPoint(m.pos, m.self, c), sep, c, dist);
                }
            }
        }
    }
}
