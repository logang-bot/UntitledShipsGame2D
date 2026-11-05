// A boss attack's cooldown progress for its HUD tile (0 = just used, 1 =
// ready). The total is learned as the largest remaining time seen since the
// attack was last ready, so phase-dependent and jittered cooldowns (Warden's
// Big Shot) need no extra getters on the boss.
public class CooldownClock
{
    private float total;

    public float Progress(float remaining)
    {
        if (remaining <= 0f)
        {
            total = 0f;
            return 1f;
        }
        if (remaining > total) total = remaining;
        return 1f - remaining / total;
    }
}
