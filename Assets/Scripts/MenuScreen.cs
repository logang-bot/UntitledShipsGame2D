using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One pre-fight menu screen: the root Canvas of a screen scene that loads
// additively on top of the persistent MenuBackground scene (grid, drifting
// ships, camera, EventSystem). Slides its groups in on Start and out on
// GoTo/GoBack, then swaps itself for the next screen scene. Opening a screen
// scene on its own (editor Play, or PauseUI loading MainMenu/RoleSelect from
// a level) loads MenuBackground underneath it. See docs/systems/scene-flow.md.
[RequireComponent(typeof(CanvasGroup))]
public class MenuScreen : MonoBehaviour
{
    public const string BackgroundScene = "MenuBackground";

    private const float EnterDuration = 0.22f;
    private const float ExitDuration = 0.16f;
    private const float Stagger = 0.04f;
    private const float SlideDistance = 24f;

    /// <summary>Header, content and footer, staggered in this order. Empty = the whole screen slides as one.</summary>
    public CanvasGroup[] slideGroups;
    /// <summary>Focused on enter (if focusOnEnter), and whenever a navigation press arrives with nothing selected.</summary>
    public Selectable firstSelected;
    /// <summary>
    /// Off for choice screens (Role Select, Level Select), which open with every
    /// option unhighlighted; the first arrow / D-pad / stick press then focuses
    /// firstSelected.
    /// </summary>
    public bool focusOnEnter = true;

    private CanvasGroup screenGroup;
    private SlideTarget[] targets;
    private bool leaving;

    public bool IsLeaving => leaving;

    void Awake()
    {
        screenGroup = GetComponent<CanvasGroup>();
        targets = BuildTargets();
        EnsureBackgroundLoaded();
        SetInteractable(false);
    }

    IEnumerator Start()
    {
        yield return PlayAll(EnterSpec(MenuTransitionState.IsBack));
        SetInteractable(true);
        if (focusOnEnter) Focus(firstSelected);
    }

    void LateUpdate()
    {
        if (leaving || !screenGroup.interactable || !NothingSelected()) return;
        if (NavigationPressed()) Focus(firstSelected);
    }

    private static bool NothingSelected() =>
        EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null;

    private static bool NavigationPressed() =>
        EventSystem.current.currentInputModule is InputSystemUIInputModule module
        && module.move != null && module.move.action != null && module.move.action.triggered;

    public void GoTo(string scene) => Leave(scene, false);

    public void GoBack(string scene) => Leave(scene, true);

    /// <summary>
    /// For a screen that leaves by its own animation rather than GoTo/GoBack
    /// (Level Select's launch): blocks input and any further transition.
    /// False if the screen is already leaving.
    /// </summary>
    public bool TryBeginLeaving()
    {
        if (leaving) return false;
        leaving = true;
        SetInteractable(false);
        return true;
    }

    public void Focus(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    private void Leave(string scene, bool back)
    {
        if (leaving) return;
        leaving = true;
        MenuTransitionState.IsBack = back;
        StartCoroutine(LeaveRoutine(scene));
    }

    private IEnumerator LeaveRoutine(string scene)
    {
        SetInteractable(false);
        yield return PlayAll(ExitSpec(MenuTransitionState.IsBack));
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    private IEnumerator PlayAll(TweenSpec spec)
    {
        for (int i = 0; i < targets.Length; i++)
            StartCoroutine(MenuTween.Slide(targets[i], WithDelay(spec, i * Stagger)));
        yield return new WaitForSecondsRealtime(spec.duration + Stagger * (targets.Length - 1));
    }

    private SlideTarget[] BuildTargets()
    {
        CanvasGroup[] groups = slideGroups != null && slideGroups.Length > 0 ? slideGroups : new[] { screenGroup };
        SlideTarget[] built = new SlideTarget[groups.Length];
        for (int i = 0; i < groups.Length; i++) built[i] = new SlideTarget(groups[i]);
        return built;
    }

    private void SetInteractable(bool interactable)
    {
        screenGroup.interactable = interactable;
        screenGroup.blocksRaycasts = interactable;
    }

    private static void EnsureBackgroundLoaded()
    {
        if (SceneManager.GetSceneByName(BackgroundScene).IsValid()) return;
        SceneManager.LoadScene(BackgroundScene, LoadSceneMode.Additive);
    }

    private static TweenSpec WithDelay(TweenSpec spec, float delay)
    {
        spec.delay = delay;
        return spec;
    }

    private static TweenSpec EnterSpec(bool back) => new TweenSpec
    {
        duration = EnterDuration,
        fromOffset = new Vector2(back ? -SlideDistance : SlideDistance, 0f),
        fromAlpha = 0f,
        toAlpha = 1f,
        easeOut = true,
    };

    private static TweenSpec ExitSpec(bool back) => new TweenSpec
    {
        duration = ExitDuration,
        toOffset = new Vector2(back ? SlideDistance : -SlideDistance, 0f),
        fromAlpha = 1f,
        toAlpha = 0f,
        easeOut = false,
    };
}
