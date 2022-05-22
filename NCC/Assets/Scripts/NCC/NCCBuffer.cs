using UnityEngine;
using System.Collections.Generic;

// this is mostly here to handle writing to internal buffers to avoid unnecessary allocation in unity's
// standard physics calls. 
public class NCCBuffer {

// Alloc() version 
    public NCCBuffer() {
        m_tbuf = new RaycastHit[16];
        m_hbuf = new RaycastHit[16];
        m_cbuf = new Collider[8];
        m_chull = new ClipHull(new List<Clip>());
    }

// NonAlloc() version
    public NCCBuffer(ClipHull chull, RaycastHit[] hbuf, RaycastHit[] tbuf, Collider[] cbuf) {
        Debug.Assert(!(chull == null || hbuf == null || tbuf == null || cbuf == null));

        this.m_hbuf  = hbuf;
        this.m_cbuf  = cbuf;
        this.m_chull = chull;
        this.m_tbuf  = tbuf;
    }

    private int numtriggers;
    private RaycastHit[] m_hbuf; // hit buffer
    private RaycastHit[] m_tbuf; // trigger buffer
    private Collider[]   m_cbuf; // collider buffer
    private Collider     m_self; // self collider
    private NCCGround    m_grnd; // ground features
    private NCCGround    m_lgrnd; // last frame's ground features
    private ClipHull    m_chull; // clip hull

    public RaycastHit[] Hits {
        get {
            return m_hbuf;
        }
    }

    public RaycastHit[] AHits {
        get {
            return m_tbuf;
        }
    }

    public Collider[] Colliders {
        get {
            return m_cbuf;
        }
    }

    public NCCGround Ground {
        get {
            return m_grnd;
        }
    }

    public NCCGround LastGround {
        get {
            return m_lgrnd;
        }
    }

    public ClipHull Clips {
        get {
            return m_chull;
        }
    }

    public void SetGround(NCCGround grnd) {
        this.m_lgrnd = this.m_grnd;
        this.m_grnd = grnd;
    }

    public int GetTriggerCount() {
        return numtriggers;
    }

    public void AddTrigger(RaycastHit hit) {
        Debug.Assert(hit.collider.isTrigger);
        
        if(numtriggers < m_tbuf.Length) {
            m_tbuf[numtriggers++] = hit;
        }
    }

    public void ResetTriggers() {
        numtriggers = 0;
    }
}