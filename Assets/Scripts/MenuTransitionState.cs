// Carries the direction of the menu transition in progress from the screen
// being left to the one being entered, so the new screen slides in from the
// matching side. Same static-carrier pattern as GameModeSelection: survives
// additive scene loads, resets on domain reload.
public static class MenuTransitionState
{
    public static bool IsBack;
}
