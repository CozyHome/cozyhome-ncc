using UnityEngine;

public enum ClipType {
    Overlap = 0,
    Trace = 1
}

public interface NCCRelay {
    void Clip(in NCCMove m, ClipType t, Clip c);
    void Trigger(in NCCMove m, Clip c);
}