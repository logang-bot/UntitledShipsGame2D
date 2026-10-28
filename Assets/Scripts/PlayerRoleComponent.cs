using UnityEngine;

// The ship's role. Read once at startup by the other ship components; the
// role's look (its 3D model and neon color) comes from ShipVisual.
public class PlayerRoleComponent : MonoBehaviour
{
    public PlayerRole role = PlayerRole.Attacker;

    public RoleStats Stats => PlayerRoleStats.Get(role);
}
