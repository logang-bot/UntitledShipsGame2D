using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// A screen-level shortcut (B / Esc = Back, Start = Continue, ...) on any
// device, built like PauseUI's standalone InputAction so it doesn't depend
// on the selected UI element or on any player's PlayerInput. Lives only
// while its GameObject is active, so a shortcut on a panel that's hidden
// (e.g. RoleSelect's single-player picker in multi mode) stays silent.
public class MenuHotkey : MonoBehaviour
{
    public string[] bindings = { "<Keyboard>/escape", "<Gamepad>/buttonEast" };
    public UnityEvent onPressed;

    private InputAction action;

    void Awake()
    {
        action = new InputAction(name, InputActionType.Button);
        foreach (string path in bindings) action.AddBinding(path);
        action.performed += _ => onPressed.Invoke();
    }

    void OnEnable() => action.Enable();

    void OnDisable() => action.Disable();

    void OnDestroy() => action.Dispose();
}
