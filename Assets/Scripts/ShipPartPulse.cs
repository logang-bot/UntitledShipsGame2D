using UnityEngine;

// Gently pulses one part of a ship model's scale (Aegis's reactor core,
// Echo's EMP core), as in the design's animated preview.
public class ShipPartPulse : MonoBehaviour
{
    public float amplitude = 0.06f;
    public float cyclesPerSecond = 0.95f;

    private Vector3 restingScale;

    void Awake()
    {
        restingScale = transform.localScale;
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.time * cyclesPerSecond * 2f * Mathf.PI);
        transform.localScale = restingScale * (1f + wave * amplitude);
    }
}
