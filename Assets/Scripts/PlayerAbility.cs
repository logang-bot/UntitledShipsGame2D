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
    public float auraRadius = 0.5f; // tiny: allies must nearly touch the Medic
    public float auraTickInterval = 1f;
    public int auraHealPerTick = 1;
    public int auraShieldPerTick = 1;

    [Header("Medic - Aura Boost (E ability)")]
    public float auraBoostRadius = 3f; // drastically larger while active
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

    [Header("Support - Buff")]
    public float buffCooldown = 8f;
    public float buffDuration = 4f;
    public float buffMoveSpeedMultiplier = 1.3f;
    public float buffFireRateMultiplier = 0.7f; // lower = faster, matches PlayerController's fireRate semantics

    [Header("Attacker - Big Shot")]
    public float bigShotCooldown = 3f;
    public float bigShotWidthMultiplier = 3f;
    public float bigShotDamage = 1.8f; // was 3 - reduced 40% across all roles' fire damage
    public float recoilForce = 6f;

    private PlayerRoleComponent roleComponent;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private float nextAbilityTime;
    private float nextAuraTickTime;
    private bool auraBoosted;
    private Coroutine buffCoroutine;
    private float buffEndTime;
    private Coroutine auraBoostCoroutine;
    private float auraBoostEndTime;
    private LineRenderer auraRing;

    public float CooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
    public bool IsBuffActive => buffCoroutine != null;
    public float BuffRemaining => Mathf.Max(0f, buffEndTime - Time.time);
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
                case PlayerRole.Support: return "Buff";
                case PlayerRole.Attacker: return "Big Shot";
                default: return "None";
            }
        }
    }

    public string StatusText
    {
        get
        {
            if (roleComponent.role == PlayerRole.Support && IsBuffActive)
                return $"+{(buffMoveSpeedMultiplier - 1f) * 100f:0}% Spd +{(1f - buffFireRateMultiplier) * 100f:0}% Rate ({BuffRemaining:0.0}s)";
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
        if (roleComponent.role == PlayerRole.Medic) CreateAuraRing();
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
            case PlayerRole.Support: TriggerBuff(); break;
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

    void TriggerBuff()
    {
        nextAbilityTime = Time.time + buffCooldown;
        buffEndTime = Time.time + buffDuration;
        if (buffCoroutine != null) StopCoroutine(buffCoroutine);
        buffCoroutine = StartCoroutine(BuffRoutine());
    }

    IEnumerator BuffRoutine()
    {
        playerController.moveSpeed *= buffMoveSpeedMultiplier;
        playerController.fireRate *= buffFireRateMultiplier;
        yield return new WaitForSeconds(buffDuration);
        playerController.moveSpeed /= buffMoveSpeedMultiplier;
        playerController.fireRate /= buffFireRateMultiplier;
        buffCoroutine = null;
    }

    void TriggerBigShot()
    {
        nextAbilityTime = Time.time + bigShotCooldown;
        playerController.FireBigShot(bigShotWidthMultiplier, bigShotDamage);
        playerController.AddRecoil(Vector2.down * recoilForce);
    }
}
