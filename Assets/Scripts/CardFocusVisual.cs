using UnityEngine;
using UnityEngine.EventSystems;

// Focus styling for a selectable card (Lobby's mode cards, later the role
// and level cards): while the card is selected by keyboard/gamepad - or
// hovered by the mouse - the white outline and "▸" tag show, and anything
// listed as hidden-on-focus (e.g. an idle footer line) hides.
public class CardFocusVisual : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject[] showWhenFocused = new GameObject[0];
    public GameObject[] hideWhenFocused = new GameObject[0];

    private bool selected;
    private bool hovered;

    void OnEnable() => Refresh();

    public void OnSelect(BaseEventData eventData) => Set(ref selected, true);
    public void OnDeselect(BaseEventData eventData) => Set(ref selected, false);
    public void OnPointerEnter(PointerEventData eventData) => Set(ref hovered, true);
    public void OnPointerExit(PointerEventData eventData) => Set(ref hovered, false);

    private void Set(ref bool flag, bool value)
    {
        flag = value;
        Refresh();
    }

    private void Refresh()
    {
        bool focused = selected || hovered;
        foreach (GameObject go in showWhenFocused) if (go != null) go.SetActive(focused);
        foreach (GameObject go in hideWhenFocused) if (go != null) go.SetActive(!focused);
    }
}
