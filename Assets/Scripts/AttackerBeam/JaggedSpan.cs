using UnityEngine;

// The two ends of one lightning line, in the beam's local space.
public readonly struct JaggedSpan
{
    public readonly Vector3 from;
    public readonly Vector3 to;

    public JaggedSpan(Vector3 from, Vector3 to)
    {
        this.from = from;
        this.to = to;
    }
}
