using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The design's "Menu Button": drives a uGUI Button's visuals through the five
// states (normal, hover, focus, pressed, disabled) from MenuButtonStyles.
// The Button keeps its click, navigation and interactable logic; its own
// Transition is None, so this is the only thing coloring the button. Focus =
// keyboard/gamepad selection: hover colors plus a white outline outside the
// frame and a marker before the label. Disabled shows a lock and, if set,
// the reason line under the button.
[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    private const float SubmitFlashSeconds = 0.08f;
    private static readonly Vector2 PressedNudge = new Vector2(0f, -1f);

    public MenuButtonVariant variant = MenuButtonVariant.Primary;
    public string disabledReason;

    [Header("Parts")]
    public Image fill;
    public Image stroke;
    public Image glow;
    public Image focusOutline;
    public RectTransform content;
    public TMP_Text label;
    public TMP_Text focusMarker;
    public Image lockIcon;
    public TMP_Text keyHint;
    public Image keyHintFrame;
    public TMP_Text reasonText;

    private Button button;
    private bool selected;
    private bool hovered;
    private bool pointerDown;
    private float submitFlashUntil;
    private MenuButtonState? shown;
    private Vector2 contentHome;

    void Awake()
    {
        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        if (content != null) contentHome = content.anchoredPosition;
    }

    void OnEnable() => shown = null;

    public void SetDisabledReason(string reason)
    {
        disabledReason = reason;
        shown = null;
    }

    void LateUpdate()
    {
        MenuButtonState state = CurrentState();
        if (shown == state) return;
        shown = state;
        Show(state);
    }

    public void OnSelect(BaseEventData eventData) => selected = true;
    public void OnDeselect(BaseEventData eventData) => selected = false;
    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
    public void OnPointerDown(PointerEventData eventData) => pointerDown = true;
    public void OnPointerUp(PointerEventData eventData) => pointerDown = false;
    public void OnSubmit(BaseEventData eventData) => submitFlashUntil = Time.unscaledTime + SubmitFlashSeconds;

    private MenuButtonState CurrentState()
    {
        if (!button.IsInteractable()) return MenuButtonState.Disabled;
        if (pointerDown || Time.unscaledTime < submitFlashUntil) return MenuButtonState.Pressed;
        if (selected) return MenuButtonState.Focus;
        return hovered ? MenuButtonState.Hover : MenuButtonState.Normal;
    }

    private void Show(MenuButtonState state)
    {
        MenuButtonLook look = MenuButtonStyles.For(variant, state);
        ShowColors(look);
        ShowMarkers(state);
        if (content != null) content.anchoredPosition = contentHome + (state == MenuButtonState.Pressed ? PressedNudge : Vector2.zero);
    }

    private void ShowColors(MenuButtonLook look)
    {
        Color text = look.text;
        if (fill != null) fill.color = look.fill;
        if (stroke != null) stroke.color = look.stroke;
        if (glow != null) glow.color = look.glow;
        if (label != null) label.color = text;
        if (lockIcon != null) lockIcon.color = text;
        ShowKeyHintColor(text);
    }

    private void ShowKeyHintColor(Color text)
    {
        text.a *= 0.75f;
        if (keyHint != null) keyHint.color = text;
        if (keyHintFrame != null) keyHintFrame.color = text;
    }

    private void ShowMarkers(MenuButtonState state)
    {
        bool disabled = state == MenuButtonState.Disabled;
        if (focusOutline != null) focusOutline.enabled = state == MenuButtonState.Focus;
        if (focusMarker != null) focusMarker.gameObject.SetActive(state == MenuButtonState.Focus);
        if (lockIcon != null) lockIcon.gameObject.SetActive(disabled);
        if (reasonText != null) reasonText.gameObject.SetActive(disabled && !string.IsNullOrEmpty(disabledReason));
        if (reasonText != null) reasonText.text = disabledReason;
    }
}
