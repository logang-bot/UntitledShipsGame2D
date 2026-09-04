using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Owns the whole pre-fight-to-boss timeline for a level: ships glide in,
// free movement, minions spawn and fight for a while, the boss glides in
// once the screen is clear, then boss combat begins (with minions
// returning at phase 2 via MarauderBoss.OnPhase2, wired in the Inspector -
// no sequencer state needed for that part).
//
// This is a deliberate, acknowledged exception to this project's normal
// "no manager/singleton" convention (see docs/architecture.md) - the
// feature is inherently a top-level sequence. Kept intentionally minimal:
// one plain MonoBehaviour, Inspector-wired, no framework. Generic name
// (not "Level1"-prefixed) since the scene and this script are meant to be
// reused by future levels with their own boss/timing values.
public class LevelSequencer : MonoBehaviour
{
    public enum SequenceState { Intro, FreeMovement, MinionPhase1, WaitingForClear, BossEntrance, BossCombat }

    [Header("Scene hookup")]
    public PlayerController[] ships; // drag Player + 3 Teammates
    public EnemySpawner enemySpawner; // drag Spawner
    public MarauderBoss marauderBoss;

    [Header("Timing")]
    public float introDuration = 4f;
    public float freeMovementDuration = 4f;
    public float minionPhase1Duration = 15f; // shortened for testing - was 120s, too slow to iterate on boss phase
    public float bossEntranceDuration = 4f;
    public float offScreenMargin = 1f; // buffer beyond the viewport edge ships/boss start hidden behind

    public SequenceState CurrentState { get; private set; }

    // Enemy scripts (MarauderBoss, Minion) check this before firing so
    // nothing can hit a ship while it's frozen and unable to dodge (during
    // Intro/BossEntrance) - see SetShipsFrozen().
    public static bool ShipsFrozen { get; private set; }

    private Vector3[] shipHomes;
    private PlayerInput[] shipInputs;
    private AIController[] shipAI;
    // Which driver each ship should return to on unfreeze. Every ship now
    // carries both PlayerInput and AIController (see Ship.prefab), so
    // "not null" no longer tells human from AI - captured once here, right
    // after PartySetupBootstrap ([DefaultExecutionOrder(-1000)]) has already
    // configured each ship's real driver for this run, human or AI.
    private bool[] shipIsHuman;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;

        shipHomes = new Vector3[ships.Length];
        shipInputs = new PlayerInput[ships.Length];
        shipAI = new AIController[ships.Length];
        shipIsHuman = new bool[ships.Length];
        for (int i = 0; i < ships.Length; i++)
        {
            shipHomes[i] = ships[i].transform.position;
            shipInputs[i] = ships[i].GetComponent<PlayerInput>();
            shipAI[i] = ships[i].GetComponent<AIController>();
            shipIsHuman[i] = shipInputs[i] != null && shipInputs[i].enabled;
        }
    }

    void Start()
    {
        // Hidden until its own entrance begins - leaving it visible would
        // show it sitting at its home position the whole time. Only the
        // sprite/ring are hidden (SetVisible), not the whole GameObject:
        // MinionSpawner lives on this same GameObject and must keep running
        // throughout (kamikaze minions are unaffected by boss visibility).
        // Physics push from wave enemies is prevented separately - see
        // Enemy.prefab's collider (now a trigger, see combat.md). Called
        // from Start(), not Awake(): MarauderBoss.Awake() (which caches the
        // SpriteRenderer/builds the shockwave ring SetVisible touches) isn't
        // guaranteed to run before this script's own Awake() - Unity only
        // guarantees every object's Awake() finishes before any Start().
        marauderBoss.SetVisible(false);
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        yield return StartCoroutine(IntroRoutine());

        CurrentState = SequenceState.FreeMovement;
        SetShipsFrozen(false);
        yield return new WaitForSeconds(freeMovementDuration);

        CurrentState = SequenceState.MinionPhase1;
        enemySpawner.StartSpawning();
        yield return new WaitForSeconds(minionPhase1Duration);

        CurrentState = SequenceState.WaitingForClear;
        enemySpawner.StopSpawning();
        while (Enemy.Active.Count > 0) yield return null;

        CurrentState = SequenceState.BossEntrance;
        yield return StartCoroutine(BossEntranceRoutine());

        CurrentState = SequenceState.BossCombat;
        SetShipsFrozen(false);
        marauderBoss.enabled = true; // fires OnEnable(), which starts its movement-pattern coroutine
    }

    IEnumerator IntroRoutine()
    {
        CurrentState = SequenceState.Intro;
        SetShipsFrozen(true);

        Vector3 viewMin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3[] startPositions = new Vector3[ships.Length];
        for (int i = 0; i < ships.Length; i++)
        {
            startPositions[i] = new Vector3(shipHomes[i].x, viewMin.y - offScreenMargin, shipHomes[i].z);
            ships[i].transform.position = startPositions[i];
        }

        float t = 0f;
        while (t < introDuration)
        {
            t += Time.deltaTime;
            float frac = Mathf.Clamp01(t / introDuration);
            for (int i = 0; i < ships.Length; i++)
                ships[i].transform.position = Vector3.Lerp(startPositions[i], shipHomes[i], frac);
            yield return null;
        }
        for (int i = 0; i < ships.Length; i++) ships[i].transform.position = shipHomes[i];
    }

    IEnumerator BossEntranceRoutine()
    {
        SetShipsFrozen(true);
        marauderBoss.SetVisible(true);

        Vector3 home = marauderBoss.transform.position;
        Vector3 viewMax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        Vector3 start = new Vector3(home.x, viewMax.y + offScreenMargin, home.z);
        marauderBoss.transform.position = start;

        float t = 0f;
        while (t < bossEntranceDuration)
        {
            t += Time.deltaTime;
            marauderBoss.transform.position = Vector3.Lerp(start, home, Mathf.Clamp01(t / bossEntranceDuration));
            yield return null;
        }
        marauderBoss.transform.position = home;
    }

    // Disabling only PlayerInput/AIController isn't enough to actually stop
    // a ship: PlayerController.HandleMovement() runs every FixedUpdate and
    // its held-fire check runs every Update regardless of those driver
    // components' enabled state. So: disable the drivers first (blocks new
    // input), clear any stale input state, then disable PlayerController
    // itself last so nothing keeps acting on old values. Unfreeze in
    // reverse order. PlayerAbility's ability input flows through PlayerInput
    // (human) / is only invoked by AIController (CPU), so no separate lock
    // is needed to block ability use.
    void SetShipsFrozen(bool frozen)
    {
        ShipsFrozen = frozen;

        for (int i = 0; i < ships.Length; i++)
        {
            PlayerController pc = ships[i];
            PlayerInput pi = shipInputs[i];
            AIController ai = shipAI[i];

            if (frozen)
            {
                if (pi != null) pi.enabled = false;
                if (ai != null) ai.enabled = false;
                pc.SetMoveDirection(Vector2.zero);
                pc.SetFiring(false);
                pc.ResetVelocity(); // so the next unfreeze ramps up from rest, not leftover velocity
                pc.enabled = false;
            }
            else
            {
                pc.enabled = true;
                // Restore each ship to its own real driver (human or AI),
                // not both - every ship carries both components now, so
                // unconditionally re-enabling both would hand AI control to
                // a human ship (and vice versa) the instant a freeze ends.
                if (pi != null) pi.enabled = shipIsHuman[i];
                if (ai != null) ai.enabled = !shipIsHuman[i];
            }
        }
    }
}
