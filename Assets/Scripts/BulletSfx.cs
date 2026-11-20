using UnityEngine;

// On an enemy bullet prefab: the sound it makes when fired (from its own
// AudioSource, capped by SfxVoiceLimit so a volley plays once) and the one
// the ship it hits plays (ShipSfx.PlayBulletHit), since the bullet is
// destroyed on impact.
public class BulletSfx : MonoBehaviour
{
    public AudioSource source;
    public SfxCue spawn;
    public SfxCue impact;

    void Start()
    {
        SfxPlayback.Play(source, spawn);
    }
}
