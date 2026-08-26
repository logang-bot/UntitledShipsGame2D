using UnityEngine;

public class PlayerRoleComponent : MonoBehaviour
{
    public PlayerRole role = PlayerRole.Attacker;

    public RoleStats Stats => PlayerRoleStats.Get(role);

    // Start, not Awake: a dynamically-Instantiate()'d ship (co-op spawner)
    // has its role assigned right after Instantiate() returns, but Awake()
    // already ran synchronously inside Instantiate() itself - Start() is
    // the first point guaranteed to see the real, assigned role. Legacy
    // scene-placed ships are unaffected: PartySetupBootstrap's
    // [DefaultExecutionOrder(-1000)] Awake() still sets role well before
    // any Start() runs.
    void Start()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.color = Stats.tintColor;
    }
}
