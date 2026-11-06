using UnityEngine;

// The timing of one level-up screen, on unscaled time since the fight is
// frozen: input is locked for the first 0.3 s so an in-flight press can't
// pick an unread card, the cards slide in over 150 ms, and a pick holds a
// 250 ms confirm before the screen moves on. Pure, so it's unit-tested
// (LevelUpFlowTests).
public class LevelUpFlow
{
    public const float LockSeconds = 0.3f;
    public const float SlideSeconds = 0.15f;
    public const float ConfirmSeconds = 0.25f;

    private float openedAt;
    private float pickedAt;

    public int PickedIndex { get; private set; } = -1;
    public bool HasPicked => PickedIndex >= 0;

    public void Open(float now)
    {
        openedAt = now;
        PickedIndex = -1;
    }

    public bool IsLocked(float now) => now < openedAt + LockSeconds;

    public float LockRemaining(float now) => Mathf.Max(0f, openedAt + LockSeconds - now);

    public float LockElapsedFraction(float now) => Mathf.Clamp01((now - openedAt) / LockSeconds);

    public float SlideIn(float now) => Mathf.Clamp01((now - openedAt) / SlideSeconds);

    public bool TryPick(int index, float now)
    {
        if (IsLocked(now) || HasPicked) return false;
        PickedIndex = index;
        pickedAt = now;
        return true;
    }

    public bool IsConfirmDone(float now) => HasPicked && now >= pickedAt + ConfirmSeconds;

    public UpgradeCardState StateOf(int index, int focusedIndex, float now)
    {
        if (IsLocked(now)) return UpgradeCardState.Locked;
        if (HasPicked) return index == PickedIndex ? UpgradeCardState.Picked : UpgradeCardState.Dim;
        return index == focusedIndex ? UpgradeCardState.Focus : UpgradeCardState.Rest;
    }
}
