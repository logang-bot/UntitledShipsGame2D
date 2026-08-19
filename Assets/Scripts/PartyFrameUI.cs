using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyFrameUI : MonoBehaviour
{
    public Image healthBarFill;
    public Image avatarImage;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI fireRateText;
    public TextMeshProUGUI abilityText;

    private PlayerHealth playerHealth;
    private PlayerRoleComponent playerRole;
    private PlayerController playerController;
    private PlayerAbility playerAbility;
    private bool isDead;

    public void Initialize(GameObject player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
        playerRole = player.GetComponent<PlayerRoleComponent>();
        playerController = player.GetComponent<PlayerController>();
        playerAbility = player.GetComponent<PlayerAbility>();

        roleText.text = "Role: " + playerRole.role;

        Color tint = playerRole.Stats.tintColor;
        healthBarFill.color = tint;
        roleText.color = tint;
        avatarImage.color = tint;
    }

    public void OnPlayerDied()
    {
        isDead = true;
        healthBarFill.color = Color.gray;
    }

    void Update()
    {
        if (playerHealth == null) return;
        if (isDead) return;

        healthBarFill.fillAmount = (float)playerHealth.CurrentHealth / playerHealth.maxHealth;
        healthText.text = $"HP: {playerHealth.CurrentHealth}/{playerHealth.maxHealth}";
        moveSpeedText.text = $"Move Speed: {playerController.moveSpeed:0.0}";
        fireRateText.text = $"Fire Rate: {playerController.fireRate:0.00}s";
        abilityText.text = $"{playerAbility.AbilityName}: {playerAbility.StatusText}";
    }
}
