using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Owns the whole pre-fight-to-boss timeline for a level: ships glide in,
// free movement, minions spawn and fight for a while, the boss glides in
// once the screen is clear, then boss combat begins (with minions
// returning at phase 2 via Level1Boss.OnPhase2, wired in the Inspector -
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
    public Level1Boss level1Boss;

    [Header("Timing")]
    public float introDuration = 4f;
    public float freeMovementDuration = 4f;
    public float minionPhase1Duration = 15f; // shortened for testing - was 120s, too slow to iterate on boss phase
    public float bossEntranceDuration = 4f;
    public float offScreenMargin = 1f; // buffer beyond the viewport edge ships/boss start hidden behind

    public SequenceState CurrentState { get; private set; }

    // Enemy scripts (Level1Boss, Minion) check this before firing so
    // nothing can hit a ship while it's frozen and unable to dodge (during
    // Intro/BossEntrance) - see SetShipsFrozen().
    public static bool ShipsFrozen { get; private set; }

    private enum ShipDriver { Human, AI, NetworkReceiver }

    private Vector3[] shipHomes;
    private PlayerInput[] shipInputs;
    private AIController[] shipAI;
    private NetworkInputReceiver[] shipNetworkReceiver;
    // Which driver each ship should return to on unfreeze. Every ship now
    // carries PlayerInput/AIController/NetworkInputReceiver (see
    // Ship.prefab), so "not null" no longer tells them apart - captured once
    // here, right after PartySetupBootstrap ([DefaultExecutionOrder(-1000)])
    // or NetworkPartySpawner has already configured each ship's real driver
    // for this run.
    private ShipDriver[] shipDriverKind;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;

        shipHomes = new Vector3[ships.Length];
        shipInputs = new PlayerInput[ships.Length];
        shipAI = new AIController[ships.Length];
        shipNetworkReceiver = new NetworkInputReceiver[ships.Length];
        shipDriverKind = new ShipDriver[ships.Length];
        for (int i = 0; i < ships.Length; i++) CacheShipDriver(i);
    }

    void CacheShipDriver(int i)
    {
        shipHomes[i] = ships[i].transform.position;
        shipInputs[i] = ships[i].GetComponent<PlayerInput>();
        shipAI[i] = ships[i].GetComponent<AIController>();
        shipNetworkReceiver[i] = ships[i].GetComponent<NetworkInputReceiver>();
        shipDriverKind[i] = DetermineDriverKind(i);
    }

    ShipDriver DetermineDriverKind(int i)
    {
        if (shipNetworkReceiver[i] != null && shipNetworkReceiver[i].enabled) return ShipDriver.NetworkReceiver;
        if (shipInputs[i] != null && shipInputs[i].enabled) return ShipDriver.Human;
        return ShipDriver.AI;
    }

    void Start()
    {
        // An online match's non-host client never runs this timeline at all
        // - EnemySpawner/Level1Boss/MinionSpawner all stay dormant until
        // something calls StartSpawning()/enables them, and this coroutine
        // is the only thing that ever does that, so gating this one entry
        // point is sufficient (see NetworkPartySpawner for the rest of the
        // host/client split). The host runs the real sequence exactly like
        // local/offline play.
        if (IsNonHostClient()) return;

        // Hidden until its own entrance begins - leaving it visible would
        // show it sitting at its home position the whole time. Only the
        // sprite/ring are hidden (SetVisible), not the whole GameObject:
        // MinionSpawner lives on this same GameObject and must keep running
        // throughout (kamikaze minions are unaffected by boss visibility).
        // Physics push from wave enemies is prevented separately - see
        // Enemy.prefab's collider (now a trigger, see combat.md). Called
        // from Start(), not Awake(): Level1Boss.Awake() (which caches the
        // SpriteRenderer/builds the shockwave ring SetVisible touches) isn't
        // guaranteed to run before this script's own Awake() - Unity only
        // guarantees every object's Awake() finishes before any Start().
        level1Boss.SetVisible(false);
        StartCoroutine(RunSequence());
    }

    bool IsNonHostClient() =>
        NetworkSession.Players != null && NetworkSession.Players.Count > 0 && !NetworkSession.IsHost;

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
        level1Boss.enabled = true; // fires OnEnable(), which starts its movement-pattern coroutine
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
        level1Boss.SetVisible(true);

        Vector3 home = level1Boss.transform.position;
        Vector3 viewMax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        Vector3 start = new Vector3(home.x, viewMax.y + offScreenMargin, home.z);
        level1Boss.transform.position = start;

        float t = 0f;
        while (t < bossEntranceDuration)
        {
            t += Time.deltaTime;
            level1Boss.transform.position = Vector3.Lerp(start, home, Mathf.Clamp01(t / bossEntranceDuration));
            yield return null;
        }
        level1Boss.transform.position = home;
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

            if (frozen)
            {
                FreezeDrivers(i);
                pc.SetMoveDirection(Vector2.zero);
                pc.SetFiring(false);
                pc.ResetVelocity(); // so the next unfreeze ramps up from rest, not leftover velocity
                pc.enabled = false;
            }
            else
            {
                pc.enabled = true;
                RestoreDriver(i);
            }
        }
    }

    void FreezeDrivers(int i)
    {
        if (shipInputs[i] != null) shipInputs[i].enabled = false;
        if (shipAI[i] != null) shipAI[i].enabled = false;
        if (shipNetworkReceiver[i] != null) shipNetworkReceiver[i].enabled = false;
    }

    // Restores each ship to its own real driver, not all three - every ship
    // carries all three components now, so unconditionally re-enabling more
    // than one would hand control to the wrong driver the instant a freeze
    // ends.
    void RestoreDriver(int i)
    {
        if (shipInputs[i] != null) shipInputs[i].enabled = shipDriverKind[i] == ShipDriver.Human;
        if (shipAI[i] != null) shipAI[i].enabled = shipDriverKind[i] == ShipDriver.AI;
        if (shipNetworkReceiver[i] != null) shipNetworkReceiver[i].enabled = shipDriverKind[i] == ShipDriver.NetworkReceiver;
    }
}
