using System;
using UnityEngine.InputSystem;

// The 1 / 2 / 3 keys (top row or numpad) that pick a level-up card directly.
// Standalone actions, live only while the level-up screen is open.
public class LevelUpKeys
{
    private readonly InputAction[] keys = new InputAction[3];

    public LevelUpKeys(Action<int> pick)
    {
        for (int i = 0; i < keys.Length; i++) keys[i] = KeyFor(i, pick);
    }

    public void Enable()
    {
        foreach (InputAction key in keys) key.Enable();
    }

    public void Disable()
    {
        foreach (InputAction key in keys) key.Disable();
    }

    private static InputAction KeyFor(int index, Action<int> pick)
    {
        InputAction action = new InputAction("Pick" + (index + 1), InputActionType.Button);
        action.AddBinding("<Keyboard>/" + (index + 1));
        action.AddBinding("<Keyboard>/numpad" + (index + 1));
        action.performed += _ => pick(index);
        return action;
    }
}
