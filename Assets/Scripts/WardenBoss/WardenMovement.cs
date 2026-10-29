using System.Collections;
using UnityEngine;

// Sibling component on the Boss GameObject: Marauder's dash-or-hold "M"
// pattern, reimplemented as a MonoBehaviour with its own fields (can't reuse
// MarauderBossMovement directly - it's a helper class owned by MarauderBoss,
// not a shared utility). Unlike Marauder, legs are speed-based and eased
// (slower, since Warden already has the densest fire of the three bosses).
// Coroutine-driven, so OnDisable must stop it - disabling a MonoBehaviour
// does not stop coroutines it started (see docs/unity-notes.md). See
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenMovement : MonoBehaviour
{
    [Header("Pattern")]
    public float sideOffsetX = 2.2f;
    public float moveSpeedMin = 1.5f;
    public float moveSpeedMax = 2.5f;
    public float cycleGapMin = 1f;
    public float cycleGapMax = 2.5f;
    public float maxAdvanceFraction = 0.4f;
    [Range(0f, 1f)] public float mPatternNotchDepth = 0.5f;
    public Vector2 screenPadding = new Vector2(0.8f, 0.5f);

    private Camera cam;
    private float homeY;
    private Vector3 home;

    void Awake()
    {
        cam = Camera.main;
        homeY = transform.position.y;
    }

    void OnEnable()
    {
        StopAllCoroutines();
        home = transform.position;
        StartCoroutine(MovementPatternRoutine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator MovementPatternRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(cycleGapMin, cycleGapMax));
            Vector3 target = PickRandomVertex();
            yield return MoveOverTime(transform.position, target, LegDuration(target));
        }
    }

    private float LegDuration(Vector3 to)
    {
        return Vector3.Distance(transform.position, to) / Random.Range(moveSpeedMin, moveSpeedMax);
    }

    private Vector3[] GetPatternVertices()
    {
        float bottomY = BottomY();
        float notchY = Mathf.Lerp(homeY, bottomY, mPatternNotchDepth);
        return new Vector3[]
        {
            home,
            ClampToBounds(new Vector3(home.x - sideOffsetX, home.y, 0f)),
            ClampToBounds(new Vector3(home.x - sideOffsetX, bottomY, 0f)),
            ClampToBounds(new Vector3(home.x, notchY, 0f)),
            ClampToBounds(new Vector3(home.x + sideOffsetX, bottomY, 0f)),
            ClampToBounds(new Vector3(home.x + sideOffsetX, home.y, 0f)),
        };
    }

    private Vector3 PickRandomVertex()
    {
        Vector3[] vertices = GetPatternVertices();
        Vector3 target;
        int guard = 0;
        do
        {
            target = vertices[Random.Range(0, vertices.Length)];
            guard++;
        } while (Vector3.Distance(target, transform.position) < 0.05f && guard < 10);
        return target;
    }

    private float ViewportHeight()
    {
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        return max.y - min.y;
    }

    private float BottomY()
    {
        if (cam == null) return homeY;
        return homeY - maxAdvanceFraction * ViewportHeight();
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        transform.position = to;
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (cam == null) return pos;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float clampedX = Mathf.Clamp(pos.x, min.x + screenPadding.x, max.x - screenPadding.x);
        float clampedY = Mathf.Clamp(pos.y, homeY - maxAdvanceFraction * ViewportHeight(), homeY);
        return new Vector3(clampedX, clampedY, 0f);
    }
}
