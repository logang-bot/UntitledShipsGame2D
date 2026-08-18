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

    private PlayerHealth playerHealth;
    private PlayerRoleComponent playerRole;
    private PlayerController playerController;

    public void Initialize(GameObject player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
        playerRole = player.GetComponent<PlayerRoleComponent>();
        playerController = player.GetComponent<PlayerController>();

        roleText.text = "Role: " + playerRole.role;

        Color tint = playerRole.Stats.tintColor;
        healthBarFill.color = tint;
        roleText.color = tint;
        avatarImage.color = tint;
    }

    void Update()
    {
        if (playerHealth == null) return;

        healthBarFill.fillAmount = (float)playerHealth.CurrentHealth / playerHealth.maxHealth;
        healthText.text = $"HP: {playerHealth.CurrentHealth}/{playerHealth.maxHealth}";
        moveSpeedText.text = $"Move Speed: {playerController.moveSpeed:0.0}";
        fireRateText.text = $"Fire Rate: {playerController.fireRate:0.00}s";
    }
}
