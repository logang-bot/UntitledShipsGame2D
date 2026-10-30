using UnityEngine;

// Shows Warden's Big Shot charge on the model while WardenBigShot
// telegraphs: the flash ring grows from 0.3x to 1.6x over the charge, four
// spikes converge on the core, and a faint lane marks the orb's path. The
// core and shutters are WardenModelStates'. Lives on the boss root beside
// WardenBigShot and only reads it.
public class WardenBigShotCharge : MonoBehaviour
{
    private static readonly string[] Spikes = { "warden_bigshot_spike_0", "warden_bigshot_spike_1", "warden_bigshot_spike_2", "warden_bigshot_spike_3" };

    public BossModel model;

    private WardenBigShot bigShot;

    void Awake()
    {
        bigShot = GetComponent<WardenBigShot>();
    }

    void LateUpdate()
    {
        model.Show("warden_bigshot", bigShot.IsTelegraphing);
        if (bigShot.IsTelegraphing) ShowCharge(bigShot.TelegraphProgress);
    }

    private void ShowCharge(float progress)
    {
        model.Node("warden_bigshot_flash").localScale = Vector3.one * (0.3f + 1.3f * progress);
        ConvergeSpikes(10f - 6f * progress);
    }

    /// <summary>The spikes sit on the diagonals (45 degrees + 90 degree
    /// steps); x is negated because glTFast mirrors the design's X axis.</summary>
    private void ConvergeSpikes(float radius)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = Mathf.PI / 4f + i * Mathf.PI / 2f;
            model.Node(Spikes[i]).localPosition = new Vector3(-Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }
    }
}
