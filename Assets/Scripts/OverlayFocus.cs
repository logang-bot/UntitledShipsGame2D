using UnityEngine;
using UnityEngine.EventSystems;

// Puts keyboard / gamepad focus on an overlay's first button when it opens
// (RESUME on Pause, RESTART on Game Over and Victory), per the design.
public class OverlayFocus : MonoBehaviour
{
    public GameObject firstSelected;

    void OnEnable()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}
