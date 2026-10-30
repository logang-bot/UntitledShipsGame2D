using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One Warden arm card (design: HUD Boss Panel): NOT ACTIVE (dashed), IDLE,
// LOCK + the role it's aimed at (boss-color stroke and glow), or DOWN with
// the seconds left and a recovery bar (hatched).
public class ArmCardView : MonoBehaviour
{
    public NeonFrame frame;
    public Image armIcon;
    public TMP_Text armName;
    public RoleChipView target;
    public TMP_Text state;
    public GameObject bar;
    public Image barFill;

    private BossTokens boss;

    public void Bind(BossTokens tokens) => boss = tokens;

    public void Show(ArmCardState card)
    {
        frame.SetLook(ArmCardLooks.For(card.status, boss));
        armName.text = $"ARM {card.letter}";
        state.text = StateText(card);
        PaintText(card.status);
        if (card.status == ArmStatus.Lock && card.target.HasValue) target.Show(card.target.Value);
        else target.Hide();
        bar.SetActive(card.status == ArmStatus.Down);
        barFill.fillAmount = card.downLeft;
    }

    private static string StateText(ArmCardState card) => card.status switch
    {
        ArmStatus.Off => "NOT ACTIVE",
        ArmStatus.Idle => "IDLE",
        ArmStatus.Lock => "LOCK",
        _ => $"DOWN {Mathf.CeilToInt(card.downSeconds)}s",
    };

    private void PaintText(ArmStatus status)
    {
        Color main = NeonPalette.Get(boss.main);
        armIcon.color = status == ArmStatus.Off ? NeonPalette.Get(PaletteColor.Muted) : status == ArmStatus.Down ? NeonPalette.Get(PaletteColor.Text3) : main;
        armName.color = NeonPalette.Get(status == ArmStatus.Off ? PaletteColor.Text3 : status == ArmStatus.Down ? PaletteColor.Text2 : PaletteColor.Text);
        state.color = status == ArmStatus.Lock ? main : NeonPalette.Get(status == ArmStatus.Off ? PaletteColor.Text3 : status == ArmStatus.Idle ? PaletteColor.Text2 : PaletteColor.Text);
    }
}
