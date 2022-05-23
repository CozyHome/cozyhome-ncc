using UnityEngine;

public class NController : MonoBehaviour, NCCRelay {

    [SerializeField] private int flags;
    [SerializeField] private LayerMask mask;

    private NCCMove nmove;
    private NCCBuffer nbuf;
    private BoxCollider box;

    public Vector3 m_forw;
    public Transform m_cam;

    private void Start() {
        box = GetComponent<BoxCollider>();
        nbuf = new NCCBuffer();
    
        Application.targetFrameRate = 144;
    }

    private void Update() {
// prevent frame-accumulation deltas on startup
        if(!(Time.time > 0))
            return;

        Vector3 inp = new Vector3(Input.GetAxisRaw("Horizontal"), 0F, Input.GetAxisRaw("Vertical"));

        Vector3 move = m_cam.rotation * inp;
        float mv_mag = move.magnitude;
        m_forw = Vector3.ProjectOnPlane(move, Vector3.up);
        m_forw.Normalize();
        m_forw *= 5F;

        float up = Input.GetKey(KeyCode.Q) ? 1 : -1;
        up += Input.GetKey(KeyCode.E) ? -1 : 1;
        up *= 2F;

        m_forw += Vector3.up * up;

        m_cam.position = Vector3.Lerp(m_cam.position, 
            this.transform.position - m_cam.forward * 10F,
            1 - Mathf.Exp(-30F * Time.deltaTime)
        );
    }

    private void FixedUpdate() {
        Vector3 pos = box.transform.position;
        Vector3 vel = m_forw;
        Vector3 scl = Vector3.Scale(box.size, transform.localScale); 
        Quaternion rot = box.transform.rotation;

        NCCMove nmove = new NCCMove(pos, vel, scl, rot, box, mask, flags, Time.fixedDeltaTime, 0.6F, 65F);
        nmove = NCC.Move(nmove, nbuf, this);
        box.transform.position = nmove.pos;
    }

    public void Clip(in NCCMove m, ClipType t, NClip c) {}
    public void Trigger(in NCCMove m, NClip c) {}
}