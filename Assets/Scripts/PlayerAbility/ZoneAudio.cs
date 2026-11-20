using UnityEngine;

// An AudioSource for a placed superpower, routed like the caster's own SFX
// source (same mixer group and blend), so its sounds still play after the
// caster's ship is gone.
public static class ZoneAudio
{
    public static AudioSource CloneOnto(AudioSource template, GameObject host)
    {
        if (template == null) return null;
        AudioSource source = host.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = template.outputAudioMixerGroup;
        source.spatialBlend = template.spatialBlend;
        source.playOnAwake = false;
        return source;
    }
}
