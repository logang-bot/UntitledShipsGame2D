// How long a boss telegraph flag (IsTelegraphing, a missile target, a
// barrage pattern) has been on. The boss panel feeds subtract this from the
// mechanic's public telegraphTime to show a countdown, so the mechanics
// don't need to expose their own timers.
public class WarningClock
{
    private bool wasOn;
    private float onSince;

    public float Elapsed { get; private set; }

    public bool Track(bool on, float now)
    {
        if (on && !wasOn) onSince = now;
        wasOn = on;
        Elapsed = on ? now - onSince : 0f;
        return on;
    }
}
