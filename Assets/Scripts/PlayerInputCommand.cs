using UnityEngine;

// The discrete, serializable per-tick input a remote client sends over
// Nakama and the host replays through NetworkInputReceiver - the primitive
// today's continuous Update()/FixedUpdate() input polling has no equivalent
// of. JsonUtility (de)serializes it directly since every field is public
// and JsonUtility-compatible (including Vector2), matching this project's
// plain-data, no-extra-library style.
[System.Serializable]
public struct PlayerInputCommand
{
    public Vector2 moveDirection;
    public bool firing;
    public bool abilityPressed;
    public bool combo1Pressed;
    public bool combo2Pressed;
    public bool combo3Pressed;

    public string ToJson() => JsonUtility.ToJson(this);

    public static PlayerInputCommand FromJson(string json) =>
        JsonUtility.FromJson<PlayerInputCommand>(json);
}
