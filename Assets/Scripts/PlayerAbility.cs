using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerAbility : MonoBehaviour
{
    [Header("Tank - Taunt")]
    public float tauntCooldown = 5f;
    public UnityEvent OnTaunt;

    [Header("Medic - Aura")]
    // All 4 ship transforms (self included - filtered out at runtime), so
    // the aura can reach the human Player too, unlike AIController's
    // teammates[] which deliberately excludes Player.
    public Transform[] allies;
    // Ship colliders are ~0.6 world units across, and ship-vs-ship overlap
    // resolution (PlayerController.ResolveShipCollisions) never lets two
    // ships get closer than roughly that - so this must stay comfortably
    // above ~0.6 or the Medic can never physically get an ally in range.
    public float auraRadius = 1.0f;
    public float auraTickInterval = 1f;
    public int auraHealPerTick = 1;
    public int auraShieldPerTick = 1;

    [Header("Medic - Aura Boost (E ability)")]
    public float auraBoostRadius = 2.0f; // drastically larger while active - halved from 3, was flagged overpowered
    public float auraBoostTickInterval = 0.25f; // much faster while active
    public float auraBoostDuration = 4f;
    public float auraBoostCooldown = 10f; // must stay >= duration, same constraint as Support's buff

    [Header("Medic - Aura Visual")]
    public Color auraRingColor = new Color(0.3f, 1f, 0.4f, 0.35f);
    public Color auraRingBoostedColor = new Color(0.3f, 1f, 0.4f, 0.7f);
    public float auraRingWidth = 0.05f;
    public float auraRingBoostedWidth = 0.12f;
    public Color healFlashColor = new Color(0.5f, 1f, 0.6f);
    private const int AuraRingSegments = 32;

    [Header("Support - Speed Boost")]
    public float speedBoostCooldown = 15f; // up from 8s - flagged overpowered, round placeholder, tunable
    public float speedBoostDuration = 4f;
    public float speedBoostMultiplier = 1.5f; // applies to move speed AND fire rate, all 4 allies

    [Header("Attacker - Big Shot")]
    public float bigShotCooldown = 3f;
    public float bigShotWidthMultiplier = 3f;
    public float bigShotDamageMultiplier = 2f; // live multiplier of the caster's current fireDamage, not a flat number
    public float recoilForce = 6f;

    [Header("Tank - Shield Arc")]
    public float shieldArcWidthMultiplier = 3f; // arc width = Tank's own collider width x this
    public float shieldArcHeight = 0.4f; // bulge/thickness of the curve
    public float shieldArcYOffset = 0.3f; // local offset above the ship
    public Color shieldArcColor = new Color(0.3f, 0.5f, 1f, 0.5f);
    public float shieldArcLineWidth = 0.08f;
    private const int ShieldArcSegments = 16;

    [Header("Party Buff Visual")]
    public float partyBuffRingRadius = 0.6f;
    public float partyBuffRingWidth = 0.08f;
    private const int PartyBuffRingSegments = 24;

    private PlayerRoleComponent roleComponent;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private float nextAbilityTime;
    private float nextAuraTickTime;
    private bool auraBoosted;
    private Coroutine speedBoostCoroutine;
    private float speedBoostEndTime;
    private Coroutine auraBoostCoroutine;
    private float auraBoostEndTime;
    private LineRenderer auraRing;
    private LineRenderer partyBuffRing;

    public float CooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
    public bool IsSpeedBoostActive => speedBoostCoroutine != null;
    public float SpeedBoostRemaining => Mathf.Max(0f, speedBoostEndTime - Time.time);
    public bool IsAuraBoosted => auraBoosted;
    public float AuraBoostRemaining => Mathf.Max(0f, auraBoostEndTime - Time.time);

    public string AbilityName
    {
        get
        {
            switch (roleComponent.role)
            {
                case PlayerRole.Tank: return "Taunt";
                case PlayerRole.Medic: return "Aura Boost";
                case PlayerRole.Support: return "Speed Boost";
                case PlayerRole.Attacker: return "Big Shot";
                default: return "None";
            }
        }
    }

    public string StatusText
    {
        get
        {
            if (roleComponent.role == PlayerRole.Support && IsSpeedBoostActive)
                return $"+{(speedBoostMultiplier - 1f) * 100f:0}% Spd/Rate ({SpeedBoostRemaining:0.0}s)";
            if (roleComponent.role == PlayerRole.Medic && IsAuraBoosted)
                return $"Boosted ({AuraBoostRemaining:0.0}s)";
            return CooldownRemaining > 0f ? $"{CooldownRemaining:0.0}s" : "Ready";
        }
    }

    void Awake()
    {
        roleComponent = GetComponent<PlayerRoleComponent>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        CreatePartyBuffRing();
    }

    // Start, not Awake: see PlayerRoleComponent.cs's Start() comment - the
    // role-dependent setup below needs the real assigned role, which a
    // dynamically-Instantiate()'d ship (co-op spawner) only has after
    // Instantiate() returns, i.e. after Awake() already ran. The ref
    // caching/CreatePartyBuffRing above stay in Awake since they're
    // role-agnostic.
    void Start()
    {
        if (roleComponent.role == PlayerRole.Medic) CreateAuraRing();
        if (roleComponent.role == PlayerRole.Tank) CreateShieldArc();

        // Start on cooldown instead of immediately available - same
        // per-role cooldown value TryUseAbility() would apply on first use,
        // just without actually triggering the ability's effects.
        nextAbilityTime = Time.time + InitialCooldown(roleComponent.role);
    }

    float InitialCooldown(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Tank: return tauntCooldown;
            case PlayerRole.Medic: return auraBoostCooldown;
            case PlayerRole.Support: return speedBoostCooldown;
            case PlayerRole.Attacker: return bigShotCooldown;
            default: return 0f;
        }
    }

    void Update()
    {
        if (roleComponent.role != PlayerRole.Medic) return;
        UpdateAuraRing();
        if (Time.time < nextAuraTickTime) return;
        nextAuraTickTime = Time.time + (auraBoosted ? auraBoostTickInterval : auraTickInterval);
        TickAura();
    }

    // Passive proximity heal/shield aura. Tiny radius by default (allies
    // must nearly touch the Medic); TriggerAuraBoost() temporarily expands
    // both the radius and tick rate. Lives here rather than on AIController
    // since it must work identically whether Medic is human- or
    // AI-controlled (see docs/systems/boss.md's "AI teammate behavior").
    private void TickAura()
    {
        if (allies == null) return;
        float radius = auraBoosted ? auraBoostRadius : auraRadius;
        foreach (Transform ally in allies)
        {
            if (ally == null || ally == transform || !ally.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(transform.position, ally.position) > radius) continue;
            PlayerHealth allyHealth = ally.GetComponent<PlayerHealth>();
            if (allyHealth == null) continue;
            bool neededHeal = allyHealth.CurrentHealth < allyHealth.maxHealth || allyHealth.CurrentShield < allyHealth.maxShield;
            allyHealth.Heal(auraHealPerTick);
            allyHealth.RestoreShield(auraShieldPerTick);
            if (neededHeal)
            {
                PlayerDamageFlash allyFlash = ally.GetComponent<PlayerDamageFlash>();
                if (allyFlash != null) allyFlash.Flash(healFlashColor);
            }
        }
    }

    // World-space ring around the Medic showing the aura's current reach -
    // tiny and dim by default, larger and brighter while boosted. Built
    // procedurally (Sprites/Default shader, no art asset) matching the
    // project's current placeholder-art phase.
    private void CreateAuraRing()
    {
        GameObject ringObj = new GameObject("AuraRing");
        ringObj.transform.SetParent(transform, false);
        auraRing = ringObj.AddComponent<LineRenderer>();
        auraRing.useWorldSpace = true;
        auraRing.loop = true;
        auraRing.positionCount = AuraRingSegments;
        auraRing.material = new Material(Shader.Find("Sprites/Default"));
        auraRing.sortingLayerName = "Default";
        auraRing.sortingOrder = -1; // behind the ship sprite
    }

    private void UpdateAuraRing()
    {
        if (auraRing == null) return;
        float radius = auraBoosted ? auraBoostRadius : auraRadius;
        Color color = auraBoosted ? auraRingBoostedColor : auraRingColor;
        float width = auraBoosted ? auraRingBoostedWidth : auraRingWidth;
        auraRing.startColor = color;
        auraRing.endColor = color;
        auraRing.startWidth = width;
        auraRing.endWidth = width;

        Vector3 center = transform.position;
        for (int i = 0; i < AuraRingSegments; i++)
        {
            float angle = (i / (float)AuraRingSegments) * Mathf.PI * 2f;
            auraRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    // Wide, curved shield in front of Tank - both a visual (LineRenderer)
    // and a real trigger (EdgeCollider2D, tagged Player, no PlayerHealth of
    // its own) that blocks enemy bullets across a width wider than Tank's
    // own body, per Bullet.cs's GetComponentInParent fix routing the hit to
    // this ship's own PlayerHealth. Local-space and built once - unlike
    // Medic's ring it never resizes, so it needs no per-frame Update().
    // Always on, independent of Taunt.
    private void CreateShieldArc()
    {
        BoxCollider2D bodyCollider = GetComponent<BoxCollider2D>();
        float tankWidth = bodyCollider != null ? bodyCollider.bounds.size.x : 1f;
        float arcWidth = tankWidth * shieldArcWidthMultiplier;
        float halfWidth = arcWidth / 2f;

        Vector2[] points = new Vector2[ShieldArcSegments];
        for (int i = 0; i < ShieldArcSegments; i++)
        {
            float t = i / (float)(ShieldArcSegments - 1); // 0..1 across the arc
            float x = Mathf.Lerp(-halfWidth, halfWidth, t);
            float y = shieldArcYOffset + shieldArcHeight * (1f - (x / halfWidth) * (x / halfWidth));
            points[i] = new Vector2(x, y);
        }

        GameObject arcObj = new GameObject("ShieldArc");
        arcObj.transform.SetParent(transform, false);
        arcObj.tag = "Player";

        EdgeCollider2D arcCollider = arcObj.AddComponent<EdgeCollider2D>();
        arcCollider.isTrigger = true;
        arcCollider.points = points;

        LineRenderer arcLine = arcObj.AddComponent<LineRenderer>();
        arcLine.useWorldSpace = false;
        arcLine.loop = false;
        arcLine.positionCount = ShieldArcSegments;
        arcLine.material = new Material(Shader.Find("Sprites/Default"));
        arcLine.sortingLayerName = "Default";
        arcLine.sortingOrder = -1;
        arcLine.startColor = shieldArcColor;
        arcLine.endColor = shieldArcColor;
        arcLine.startWidth = shieldArcLineWidth;
        arcLine.endWidth = shieldArcLineWidth;
        for (int i = 0; i < ShieldArcSegments; i++)
        {
            arcLine.SetPosition(i, points[i]);
        }
    }

    // Generic, initially-hidden ring on every ship (not role-gated) that
    // Support's party-wide speed boost toggles on/off, tinted to match
    // whichever role cast it. Local-space and built once - a fixed-radius
    // circle has nothing to recompute per frame.
    private void CreatePartyBuffRing()
    {
        GameObject ringObj = new GameObject("PartyBuffRing");
        ringObj.transform.SetParent(transform, false);
        partyBuffRing = ringObj.AddComponent<LineRenderer>();
        partyBuffRing.useWorldSpace = false;
        partyBuffRing.loop = true;
        partyBuffRing.positionCount = PartyBuffRingSegments;
        partyBuffRing.material = new Material(Shader.Find("Sprites/Default"));
        partyBuffRing.sortingLayerName = "Default";
        partyBuffRing.sortingOrder = -1;
        partyBuffRing.startWidth = partyBuffRingWidth;
        partyBuffRing.endWidth = partyBuffRingWidth;
        for (int i = 0; i < PartyBuffRingSegments; i++)
        {
            float angle = (i / (float)PartyBuffRingSegments) * Mathf.PI * 2f;
            partyBuffRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * partyBuffRingRadius);
        }
        ringObj.SetActive(false);
    }

    // Called on every ally by Support's speed boost - shows/hides this
    // ship's party-buff ring in the caster's tint color.
    public void SetPartyBuffVisual(bool active, Color color)
    {
        if (partyBuffRing == null) return;
        if (active)
        {
            partyBuffRing.startColor = color;
            partyBuffRing.endColor = color;
        }
        partyBuffRing.gameObject.SetActive(active);
    }

public void OnAbility(InputValue value)
    {
        if (!value.isPressed) return;
        TryUseAbility();
    }

    // Non-input entry point so AIController can trigger abilities directly
    // through the same cooldown gate.
    public void TryUseAbility()
    {
        if (Time.time < nextAbilityTime) return;
        switch (roleComponent.role)
        {
            case PlayerRole.Tank: TriggerTaunt(); break;
            case PlayerRole.Medic: TriggerAuraBoost(); break;
            case PlayerRole.Support: TriggerSpeedBoost(); break;
            case PlayerRole.Attacker: TriggerBigShot(); break;
        }
    }

    void TriggerTaunt()
    {
        nextAbilityTime = Time.time + tauntCooldown;
        OnTaunt?.Invoke();
    }

    void TriggerAuraBoost()
    {
        nextAbilityTime = Time.time + auraBoostCooldown;
        auraBoostEndTime = Time.time + auraBoostDuration;
        if (auraBoostCoroutine != null) StopCoroutine(auraBoostCoroutine);
        auraBoostCoroutine = StartCoroutine(AuraBoostRoutine());
    }

    IEnumerator AuraBoostRoutine()
    {
        auraBoosted = true;
        yield return new WaitForSeconds(auraBoostDuration);
        auraBoosted = false;
        auraBoostCoroutine = null;
    }

    void TriggerSpeedBoost()
    {
        nextAbilityTime = Time.time + speedBoostCooldown;
        speedBoostEndTime = Time.time + speedBoostDuration;
        if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine());
    }

    IEnumerator SpeedBoostRoutine()
    {
        SetPartyBuff(speedBoostMultiplier, true);
        yield return new WaitForSeconds(speedBoostDuration);
        SetPartyBuff(1f, false);
        speedBoostCoroutine = null;
    }

    // Non-destructive party-wide buff: sets (never multiplies-in-place)
    // every ally's PlayerController buff multipliers and toggles their
    // party-buff ring, tinted to this caster's (always Support's) color.
    // Plain assignment on both ends, so there's no revert math and nothing
    // to double-apply.
    private void SetPartyBuff(float multiplier, bool visualActive)
    {
        if (allies == null) return;
        foreach (Transform ally in allies)
        {
            if (ally == null || !ally.gameObject.activeInHierarchy) continue;
            PlayerController pc = ally.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.speedBuffMultiplier = multiplier;
                pc.fireRateBuffMultiplier = multiplier;
            }
            PlayerAbility ability = ally.GetComponent<PlayerAbility>();
            if (ability != null) ability.SetPartyBuffVisual(visualActive, roleComponent.Stats.tintColor);
        }
    }

    void TriggerBigShot()
    {
        nextAbilityTime = Time.time + bigShotCooldown;
        float damage = playerController.fireDamage * bigShotDamageMultiplier;
        playerController.FireBigShot(bigShotWidthMultiplier, damage);
        playerController.AddRecoil(Vector2.down * recoilForce);
    }
}
