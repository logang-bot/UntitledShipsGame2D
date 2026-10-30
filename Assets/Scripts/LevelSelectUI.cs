using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Card-per-level picker between RoleSelect and the shared Level scene. One
// click per card, no separate confirm step (unlike RoleSelectUI's
// pick-then-Start flow, which exists to gate on a variable-length
// multi-picker) - each card is already a complete, unambiguous choice, same
// shape as LobbyUI.SelectLocal(). LevelSetup reads the pick from
// LevelSelection to decide which boss to spawn. Launching grows the picked
// card to fill the screen and cuts through black into the fight; Level
// loads on its own (not additively), which closes MenuBackground too.
public class LevelSelectUI : MonoBehaviour
{
    private const float GrowSeconds = 0.3f;
    private const float BlackoutSeconds = 0.12f;

    public MenuScreen screen;
    public SquadStripView squad;
    public LevelCardView[] cards; // index = (int)Level
    public CanvasGroup blackout;

    void Start() => squad.Show(SquadLineup.Current());

    public void SelectLevel1() { LoadLevel(Level.Level1); }
    public void SelectLevel2() { LoadLevel(Level.Level2); }
    public void SelectLevel3() { LoadLevel(Level.Level3); }

    public void Back()
    {
        screen.GoBack("RoleSelect");
    }

    private void LoadLevel(Level level)
    {
        if (!screen.TryBeginLeaving()) return;
        LevelSelection.SelectedLevel = level;
        StartCoroutine(LaunchRoutine((RectTransform)cards[(int)level].transform));
    }

    private IEnumerator LaunchRoutine(RectTransform card)
    {
        card.SetParent(card.root, true);
        card.SetAsLastSibling();
        yield return MenuTween.GrowToCenter(card, FillScale(card), GrowSeconds);
        blackout.transform.SetAsLastSibling();
        yield return MenuTween.Slide(new SlideTarget(blackout), new TweenSpec { duration = BlackoutSeconds, fromAlpha = 0f, toAlpha = 1f });
        SceneManager.LoadScene("Level");
    }

    private static float FillScale(RectTransform card)
    {
        Rect screenRect = ((RectTransform)card.root).rect;
        return Mathf.Max(screenRect.width / card.rect.width, screenRect.height / card.rect.height);
    }
}
