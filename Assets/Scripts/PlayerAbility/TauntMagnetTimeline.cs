// The Taunt magnet's frame at a moment, in seconds from the E press:
// the field snaps on, loops its inward pull for the taunt's length, then
// collapses. Pure, so it's unit-tested (TauntMagnetTimelineTests).
public static class TauntMagnetTimeline
{
    public const float ActivateSeconds = 0.15f;
    public const float SustainFrameSeconds = 0.1f;
    public const float EndSeconds = 0.2f;

    private static readonly TauntMagnetFrame[] Sustain =
        { TauntMagnetFrame.SustainA, TauntMagnetFrame.SustainB, TauntMagnetFrame.SustainC };

    public static TauntMagnetFrame FrameAt(float elapsed, float tauntSeconds)
    {
        if (elapsed < 0f || elapsed >= tauntSeconds + EndSeconds) return TauntMagnetFrame.None;
        if (elapsed >= tauntSeconds) return TauntMagnetFrame.End;
        if (elapsed < ActivateSeconds) return TauntMagnetFrame.Activate;
        int step = (int)((elapsed - ActivateSeconds) / SustainFrameSeconds);
        return Sustain[step % Sustain.Length];
    }
}
