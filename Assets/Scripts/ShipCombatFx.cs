using UnityEngine;

// The Combat FX sprites a ship carries: its role's superpower sequence, the
// heal flash it shows when a Medic patches it up, and the sprites of the
// role visuals the PlayerAbility helpers draw (Medic aura and Sanctuary,
// Tank shield arc, Super Shield and Taunt magnet, Support Echo Gate). The
// Tank's Q has no one-shot sequence: its Super Shield is drawn on the arc
// itself. One component on Ship.prefab serves all four roles, since a ship
// only learns its role at spawn.
public class ShipCombatFx : MonoBehaviour
{
    private const float SanctuaryArtRadiusPx = 384f;
    private const float AuraBoostRingRadiusPx = 240f;

    [Header("Superpowers (SpriteSequence prefabs, played on the press)")]
    public GameObject auraBoostFx;
    public GameObject speedBoostFx;
    // The Attacker's Nova Beam wind-up flash at the muzzle (the design's
    // Big Shot frames, which now open the beam cast).
    public GameObject bigShotFx;
    public GameObject healFlashFx;

    [Header("Attacker Nova Beam")]
    public GameObject beamPrefab;

    [Header("Medic aura (the boost sprites stand in for the Sanctuary until its art lands)")]
    public Sprite auraIdle;
    public Sprite auraBoost;
    public Sprite auraBoostPulse;

    [Header("Medic Sanctuary, in ZoneFrame order")]
    public Sprite[] sanctuaryFrames;

    [Header("Tank shield arc")]
    public Sprite shieldIdle;
    public Sprite shieldBlock;

    [Header("Tank Super Shield, in SuperShieldFrame order")]
    public Sprite[] superShieldFrames;
    public Sprite superShieldBlock;

    [Header("Tank Taunt magnet, in TauntMagnetFrame order")]
    public Sprite[] tauntMagnetFrames;

    [Header("Support Echo Gate, in ZoneFrame order (empty = a plain bar)")]
    public Sprite[] echoGateFrames;
    public Sprite echoGateShatter;
    public Sprite echoSplitSpark;
    public Sprite echoTether;
    public Sprite[] echoChannelFrames;
    // The old party speed ring, standing in for the channel ring until its art lands.
    public Sprite speedRing;

    public bool HasSuperShieldArt =>
        superShieldFrames != null && superShieldFrames.Length == (int)SuperShieldFrame.Collapse + 1 && superShieldBlock != null;

    public bool HasTauntMagnetArt => tauntMagnetFrames != null && tauntMagnetFrames.Length == (int)TauntMagnetFrame.None;

    public bool HasSanctuaryArt => IsFullZoneSet(sanctuaryFrames);

    public bool HasEchoGateArt => IsFullZoneSet(echoGateFrames);

    /// <summary>How far from its centre the current Sanctuary sprite draws the dome's edge.</summary>
    public float SanctuaryRadiusPx => HasSanctuaryArt ? SanctuaryArtRadiusPx : AuraBoostRingRadiusPx;

    public Sprite SuperShieldSprite(SuperShieldFrame frame) => superShieldFrames[(int)frame];

    public Sprite SanctuarySprite(ZoneFrame frame)
    {
        if (HasSanctuaryArt) return sanctuaryFrames[(int)frame];
        return frame == ZoneFrame.Pulse ? auraBoostPulse : auraBoost;
    }

    public void PlayAbility(PlayerRole role) => SpawnChild(AbilityFxFor(role));

    public void PlayHealFlash() => SpawnChild(healFlashFx);

    /// <summary>Spawned in world space, not under the ship, so the ship's 0.6 scale doesn't size the beam.</summary>
    public void SpawnBeam(AttackerBeamShot shot)
    {
        if (beamPrefab == null) return;
        Instantiate(beamPrefab).GetComponent<AttackerBeam>().Fire(shot);
    }

    private static bool IsFullZoneSet(Sprite[] frames) => frames != null && frames.Length == (int)ZoneFrame.Collapse + 1;

    private GameObject AbilityFxFor(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Tank: return null;
            case PlayerRole.Medic: return auraBoostFx;
            case PlayerRole.Support: return speedBoostFx;
            default: return bigShotFx;
        }
    }

    private void SpawnChild(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, transform);
        FxSprites.UndoParentScale(fx.transform);
    }
}
