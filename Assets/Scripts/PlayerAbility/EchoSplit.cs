using UnityEngine;

// Which shots the Echo Gate splits, and the fan they split into. Pure, so
// it's unit-tested (EchoSplitTests).
public static class EchoSplit
{
    /// <summary>Only a player's regular shot that hasn't been split already: no combo shots, no enemy bullets.</summary>
    public static bool ShouldSplit(Bullet shot) =>
        shot.Owner == "Player" && shot.Kind == PlayerShotKind.Regular && !shot.IsEcho;

    /// <summary>
    /// <paramref name="count"/> directions spread evenly from +spreadDeg
    /// (left) to -spreadDeg (right) around <paramref name="forward"/>; for an
    /// odd count the middle one is <paramref name="forward"/> itself.
    /// </summary>
    public static Vector2[] Directions(Vector2 forward, int count, float spreadDeg)
    {
        Vector2[] directions = new Vector2[count];
        for (int i = 0; i < count; i++) directions[i] = Rotate(forward, AngleFor(i, count, spreadDeg));
        return directions;
    }

    /// <summary>The index of the direction the original shot keeps.</summary>
    public static int MiddleIndex(int count) => count / 2;

    private static float AngleFor(int index, int count, float spreadDeg) =>
        count <= 1 ? 0f : Mathf.Lerp(spreadDeg, -spreadDeg, index / (float)(count - 1));

    private static Vector2 Rotate(Vector2 direction, float degrees) =>
        ((Vector2)(Quaternion.Euler(0f, 0f, degrees) * direction)).normalized;
}
