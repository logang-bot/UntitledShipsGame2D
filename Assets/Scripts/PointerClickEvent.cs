using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Reacts to a mouse click or tap on a UI element that must not be a
// Selectable - e.g. Lobby's locked ONLINE card, which keyboard and gamepad
// focus deliberately skip but a click still answers with a toast.
public class PointerClickEvent : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onClick;

    public void OnPointerClick(PointerEventData eventData) => onClick.Invoke();
}
