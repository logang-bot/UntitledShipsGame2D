using UnityEngine;

public class PlayerRoleComponent : MonoBehaviour
{
    public PlayerRole role = PlayerRole.Attacker;

    public RoleStats Stats => PlayerRoleStats.Get(role);

    void Awake()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.color = Stats.tintColor;
    }
}
