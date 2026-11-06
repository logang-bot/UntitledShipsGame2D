using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The level-up screen's header (design: Neon Fleet HUD v2, section 10):
// LEVEL UP, the pilot, PICK i / n when picks are banked, LV a → b, the sub
// line, and the INPUT LOCKED drain bar above the cards. The lock row keeps its
// space when hidden so the cards don't jump when the lock ends.
public class LevelUpHeaderView : MonoBehaviour
{
    public TMP_Text pilot;
    public GameObject pickTag;
    public TMP_Text pickText;
    public TMP_Text fromLevel;
    public TMP_Text toLevel;
    public TMP_Text sub;
    public CanvasGroup lockGroup;
    public Image lockFill;
    public TMP_Text lockTime;

    public void Show(int levelReached, PickStreak streak, string pilotLabel)
    {
        pilot.text = pilotLabel;
        fromLevel.text = "LV " + (levelReached - 1);
        toLevel.text = levelReached.ToString();
        pickTag.SetActive(streak.IsMulti);
        pickText.text = streak.PickText;
        sub.text = streak.SubLine;
        sub.color = NeonPalette.Get(streak.HasMore ? PaletteColor.Notice : PaletteColor.Text3);
    }

    public void ShowLock(LevelUpFlow flow, float now)
    {
        lockGroup.alpha = flow.IsLocked(now) ? 1f : 0f;
        lockFill.fillAmount = flow.LockElapsedFraction(now);
        lockTime.text = flow.LockRemaining(now).ToString("0.00", CultureInfo.InvariantCulture);
    }
}
