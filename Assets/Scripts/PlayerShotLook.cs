using UnityEngine;

// Dresses a freshly spawned player bullet: the owner's role-coloured streak,
// or one of the Attacker's Big Shot / combo sprites. A wider shot widens the
// collider, not the transform, because the wide sprites are already drawn
// wide.
public class PlayerShotLook : MonoBehaviour
{
    [Header("Regular shot per role")]
    public Sprite attackerShot;
    public Sprite tankShot;
    public Sprite medicShot;
    public Sprite supportShot;

    [Header("Attacker")]
    public Sprite bigShot;
    public Sprite combo1;
    public Sprite combo2;
    public Sprite combo3;
    public Sprite comboBroken;

    public void Dress(GameObject bullet, PlayerShotKind kind, float widthMultiplier)
    {
        Sprite sprite = SpriteFor(kind);
        if (sprite != null && bullet.TryGetComponent(out SpriteRenderer sr)) sr.sprite = sprite;
        if (bullet.TryGetComponent(out BoxCollider2D box)) box.size = new Vector2(box.size.x * widthMultiplier, box.size.y);
    }

    private Sprite SpriteFor(PlayerShotKind kind)
    {
        switch (kind)
        {
            case PlayerShotKind.BigShot: return bigShot;
            case PlayerShotKind.Combo1: return combo1;
            case PlayerShotKind.Combo2: return combo2;
            case PlayerShotKind.Combo3: return combo3;
            case PlayerShotKind.ComboBroken: return comboBroken;
            default: return RoleShot();
        }
    }

    private Sprite RoleShot()
    {
        PlayerRole role = TryGetComponent(out PlayerRoleComponent rc) ? rc.role : PlayerRole.Attacker;
        switch (role)
        {
            case PlayerRole.Tank: return tankShot;
            case PlayerRole.Medic: return medicShot;
            case PlayerRole.Support: return supportShot;
            default: return attackerShot;
        }
    }
}
