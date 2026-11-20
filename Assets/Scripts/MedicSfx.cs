// The Medic's sounds: the Sanctuary's open, loop, ending and collapse (from
// ZonePowerSfx), its heal pulse on allies inside, and the passive aura's
// quieter tick.
public class MedicSfx : ZonePowerSfx
{
    public SfxCue healTick;
    public SfxCue auraTick;

    public void PlayHealTick() => SfxPlayback.Play(source, healTick);

    public void PlayAuraTick() => SfxPlayback.Play(source, auraTick);
}
