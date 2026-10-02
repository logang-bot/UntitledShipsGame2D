using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyFrameUI : MonoBehaviour
{
    public Image healthBarFill;
    public Image shieldBarFill;
    public Image avatarImage;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI shieldText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI fireRateText;
    public TextMeshProUGUI dpsText;
    public TextMeshProUGUI abilityText;
    
    public Button abilityButton;
public TextMeshProUGUI nameText;

    private PlayerHealth playerHealth;
    private PlayerRoleComponent playerRole;
    private PlayerController playerController;

    private bool isHuman;
private PlayerAbility playerAbility;
    private bool isDead;
    // The ship's own root GameObject - kept so DPS can be looked up as
    // MarauderBoss.GetDamageDealt(shipObject), the same key TakeDamage()
    // records it under (see PlayerController.SpawnBullet's `gameObject`
    // passed as the bullet's source).
    private GameObject shipObject;

public void Initialize(GameObject player, string displayName, bool isHumanPlayer)
    {
        shipObject = player;
        playerHealth = player.GetComponent<PlayerHealth>();
        playerRole = player.GetComponent<PlayerRoleComponent>();
        playerController = player.GetComponent<PlayerController>();
        playerAbility = player.GetComponent<PlayerAbility>();
        isHuman = isHumanPlayer;

        if (nameText != null) nameText.text = displayName;
        roleText.text = "<b>Role:</b> " + playerRole.role;

        Color tint = playerRole.Stats.tintColor;
        healthBarFill.color = tint;
        roleText.color = tint;
        avatarImage.color = tint;

        // Manual ability triggering is AI-teammate-only - the human already
        // has E-bound OnAbility(InputValue) for their own ability. The
        // playerAbility reference is only resolved here at runtime (each
        // PartyFrame prefab instance is generic until bound to a ship), so
        // this can't be an Inspector persistent listener - the one
        // deliberate exception to this codebase's "Inspector listeners
        // only" convention (see GameOverUI.cs/RoleSelectUI.cs).
        if (abilityButton != null)
        {
            abilityButton.gameObject.SetActive(!isHuman);
            if (!isHuman)
            {
                abilityButton.onClick.RemoveAllListeners();
                abilityButton.onClick.AddListener(() => playerAbility.TryUseAbility());
            }
        }
    }

    public void OnPlayerDied()
    {
        // One last paint before Update() stops running (below): TakeDamage()
        // already finished mutating playerHealth by the time Die() invokes
        // OnDeath (which calls this), and that happens in the same frame as
        // the killing blow - before this frame's own Update() - so without
        // this, the bars/text would freeze on the *previous* frame's values
        // (e.g. "1/5") instead of the true final ones. Clamped to 0 since
        // overkill damage can leave CurrentHealth/CurrentShield negative.
        int health = Mathf.Max(0, playerHealth.CurrentHealth);
        int shield = Mathf.Max(0, playerHealth.CurrentShield);
        healthBarFill.fillAmount = (float)health / playerHealth.maxHealth;
        healthText.text = $"<b><color=#A8A8B8>HP:</color></b> {health}/{playerHealth.maxHealth}";
        shieldBarFill.fillAmount = (float)shield / playerHealth.maxShield;
        if (shieldText != null) shieldText.text = $"<b><color=#A8A8B8>SH:</color></b> {shield}/{playerHealth.maxShield}";

        isDead = true;
        healthBarFill.color = Color.gray;
        shieldBarFill.color = Color.gray;
    }

void Update()
    {
        if (playerHealth == null) return;
        if (isDead) return;

        healthBarFill.fillAmount = (float)playerHealth.CurrentHealth / playerHealth.maxHealth;
        healthText.text = $"<b><color=#A8A8B8>HP:</color></b> {playerHealth.CurrentHealth}/{playerHealth.maxHealth}";
        shieldBarFill.fillAmount = (float)playerHealth.CurrentShield / playerHealth.maxShield;
        if (shieldText != null) shieldText.text = $"<b><color=#A8A8B8>SH:</color></b> {playerHealth.CurrentShield}/{playerHealth.maxShield}";
        moveSpeedText.text = $"<b><color=#A8A8B8>Move Speed:</color></b> {playerController.moveSpeed * playerController.speedBuffMultiplier:0.0}";
        fireRateText.text = $"<b><color=#A8A8B8>Fire Rate:</color></b> {playerController.shotsPerSecond * playerController.fireRateBuffMultiplier:0.0}/s";
        // Null-guarded like shieldText above, so an older PartyFrame instance
        // that hasn't had the line added yet keeps working instead of NREing.
        //
        // Real damage-dealt-to-boss DPS, not PlayerController.CurrentDps's
        // theoretical "every normal shot lands" ceiling - that number never
        // moves for combo/Big Shot hits, since those bypass fireDamage
        // entirely (see PlayerAbilityAttacker.TryComboAttack/Trigger).
        // MarauderBoss.GetDamageDealt/CombatElapsed is the same source of
        // truth DpsMeterUI's Recount-style panel reads.
        if (dpsText != null)
        {
            MarauderBoss boss = playerController.bossObject as MarauderBoss;
            float dps = (boss != null && boss.CombatElapsed > 0f)
                ? boss.GetDamageDealt(shipObject) / boss.CombatElapsed
                : 0f;
            dpsText.text = $"<b><color=#A8A8B8>DPS:</color></b> {dps:0.0}";
        }
        abilityText.text = $"<b><color=#A8A8B8>{playerAbility.AbilityName}:</color></b> {playerAbility.StatusText}";
        if (abilityButton != null && !isHuman) abilityButton.interactable = playerAbility.CooldownRemaining <= 0f;
    }
}
