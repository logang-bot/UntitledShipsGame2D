// The Support's Echo Gate sounds: deploy, loop, ending and collapse (from
// ZonePowerSfx), plus the split tick and the shatter. The gate plays those
// two from its own AudioSource, since the shatter comes after the Support's
// ship is gone.
public class EchoGateSfx : ZonePowerSfx
{
    public SfxCue split;
    public SfxCue shatter;
}
