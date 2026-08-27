using System.Collections;
using UnityEngine;

public class Level1BossMovement
{
    private readonly Level1Boss boss;
    private readonly Camera cam;
    private readonly float homeY;
    private Vector3 home;

    public Level1BossMovement(Level1Boss boss)
    {
        this.boss = boss;
        cam = Camera.main;
        homeY = boss.transform.position.y;
    }

    /// <summary>
    /// Captures "home" fresh each time this fires - LevelSequencer always
    /// lands the boss here first, so home is wherever it just placed it, not
    /// a value baked in the constructor.
    /// </summary>
    public void OnEnable()
    {
        home = boss.transform.position;
        boss.StartCoroutine(MovementPatternRoutine());
    }

    /// <summary>
    /// Loops for the rest of the fight once started: sit still for a random
    /// beat (so players can't time when it'll move), then hop to a random
    /// vertex of the "M" at a random speed. Runs identically through phase 1
    /// and phase 2 - no branching.
    /// </summary>
    private IEnumerator MovementPatternRoutine()
    {
        while (true)
        {
            float stillTime = Random.Range(boss.cycleGapMin, boss.cycleGapMax);
            yield return new WaitForSeconds(stillTime);

            Vector3 target = PickRandomVertex();
            float legDuration = Random.Range(boss.patternMoveDurationMin, boss.patternMoveDurationMax);
            yield return MoveOverTime(boss.transform.position, target, legDuration);
        }
    }

    /// <summary>
    /// The fixed points an "M" is built from: home, its two outer top
    /// corners, their low points below, and the middle notch between them.
    /// Recomputed each hop (not cached) since it depends on the live camera
    /// viewport via BottomY()/ClampToBounds().
    /// </summary>
    private Vector3[] GetPatternVertices()
    {
        float bottomY = BottomY();
        float notchY = Mathf.Lerp(homeY, bottomY, boss.mPatternNotchDepth);
        return new Vector3[]
        {
            home,
            ClampToBounds(new Vector3(home.x - boss.sideOffsetX, home.y, 0f)), // top-left
            ClampToBounds(new Vector3(home.x - boss.sideOffsetX, bottomY, 0f)), // bottom-left
            ClampToBounds(new Vector3(home.x, notchY, 0f)), // notch
            ClampToBounds(new Vector3(home.x + boss.sideOffsetX, bottomY, 0f)), // bottom-right
            ClampToBounds(new Vector3(home.x + boss.sideOffsetX, home.y, 0f)), // top-right
        };
    }

    /// <summary>
    /// Picks a random vertex, re-rolling (bounded) if it happens to land on
    /// wherever the boss already is, so a hop always actually goes somewhere.
    /// </summary>
    private Vector3 PickRandomVertex()
    {
        Vector3[] vertices = GetPatternVertices();
        Vector3 current = boss.transform.position;
        Vector3 target;
        int guard = 0;
        do
        {
            target = vertices[Random.Range(0, vertices.Length)];
            guard++;
        } while (Vector3.Distance(target, current) < 0.05f && guard < 10);
        return target;
    }

    /// <summary>
    /// The lowest Y the M's outer corners descend to - same
    /// maxAdvanceFraction-of-viewport floor ClampToBounds() enforces, just
    /// computed directly instead of relying on clamping an already-far-below candidate.
    /// </summary>
    private float BottomY()
    {
        if (cam == null) return homeY;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float viewportHeight = max.y - min.y;
        return homeY - boss.maxAdvanceFraction * viewportHeight;
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            boss.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        boss.transform.position = to;
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (cam == null) return pos;

        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float viewportHeight = max.y - min.y;

        float clampedX = Mathf.Clamp(pos.x, min.x + boss.screenPadding.x, max.x - boss.screenPadding.x);
        float minY = homeY - boss.maxAdvanceFraction * viewportHeight;
        float clampedY = Mathf.Clamp(pos.y, minY, homeY);
        return new Vector3(clampedX, clampedY, 0f);
    }
}
