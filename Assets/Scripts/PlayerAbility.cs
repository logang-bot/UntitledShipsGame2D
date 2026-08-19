using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerAbility : MonoBehaviour
{
    [Header("Tank - Taunt")]
    public float tauntCooldown = 5f;
    public UnityEvent OnTaunt;

    [Header("Medic - Heal")]
    public float healCooldown = 6f;
    public int healAmount = 2;

    [Header("Support - Buff")]
    public float buffCooldown = 8f;
    public float buffDuration = 4f;
    public float buffMoveSpeedMultiplier = 1.3f;
    public float buffFireRateMultiplier = 0.7f; // lower = faster, matches PlayerController's fireRate semantics

    [Header("Attacker - Big Shot")]
    public float bigShotCooldown = 3f;
    public float bigShotWidthMultiplier = 3f;
    public int bigShotDamage = 3;
    public float recoilForce = 6f;

    private PlayerRoleComponent roleComponent;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private float nextAbilityTime;
    private Coroutine buffCoroutine;
    private float buffEndTime;

    public float CooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
    public bool IsBuffActive => buffCoroutine != null;
    public float BuffRemaining => Mathf.Max(0f, buffEndTime - Time.time);

    public string AbilityName
    {
        get
        {
            switch (roleComponent.role)
            {
                case PlayerRole.Tank: return "Taunt";
                case PlayerRole.Medic: return "Heal";
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
            return CooldownRemaining > 0f ? $"{CooldownRemaining:0.0}s" : "Ready";
        }
    }

    void Awake()
    {
        roleComponent = GetComponent<PlayerRoleComponent>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    public void OnAbility(InputValue value)
    {
        if (!value.isPressed || Time.time < nextAbilityTime) return;
        switch (roleComponent.role)
        {
            case PlayerRole.Tank: TriggerTaunt(); break;
            case PlayerRole.Medic: TriggerHeal(); break;
            case PlayerRole.Support: TriggerBuff(); break;
            case PlayerRole.Attacker: TriggerBigShot(); break;
        }
    }

    void TriggerTaunt()
    {
        nextAbilityTime = Time.time + tauntCooldown;
        OnTaunt?.Invoke();
    }

    void TriggerHeal()
    {
        nextAbilityTime = Time.time + healCooldown;
        playerHealth.Heal(healAmount);
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
