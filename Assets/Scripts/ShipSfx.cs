using UnityEngine;

// A ship's own one-shot sounds: its role's normal fire, the Attacker's combo
// shots, the hit-confirm when its shots land, the hit it takes from an enemy
// bullet and the Super Shield's free blocks.
public class ShipSfx : MonoBehaviour
{
    public AudioSource source;
    public RoleShotSound[] roleShots;
    public SfxCue combo1;
    public SfxCue combo2;
    public SfxCue combo3Finisher;
    public SfxCue comboBroken;
    public SfxCue shotHit;
    public SfxCue bulletHit;
    public SfxCue superShieldBlock;

    public void PlayShot(PlayerShotKind kind) => SfxPlayback.Play(source, ShotCue(kind));

    public void PlayShotHit() => SfxPlayback.Play(source, shotHit);

    public void PlayBlock() => SfxPlayback.Play(source, superShieldBlock);

    public void PlayBulletHit(BulletSfx bullet)
    {
        bool hasOwnImpact = bullet != null && !bullet.impact.IsEmpty;
        SfxPlayback.Play(source, hasOwnImpact ? bullet.impact : bulletHit);
    }

    private SfxCue ShotCue(PlayerShotKind kind)
    {
        switch (kind)
        {
            case PlayerShotKind.Combo1: return combo1;
            case PlayerShotKind.Combo2: return combo2;
            case PlayerShotKind.Combo3: return combo3Finisher;
            case PlayerShotKind.ComboBroken: return comboBroken;
            default: return RoleShot();
        }
    }

    private SfxCue RoleShot()
    {
        if (roleShots == null || !TryGetComponent(out PlayerRoleComponent roleComponent)) return null;
        foreach (RoleShotSound entry in roleShots)
            if (entry.role == roleComponent.role) return entry.cue;
        return null;
    }
}
