using UnityEngine;

// The Combat FX sprites a ship carries: its role's superpower sequence, the
// heal flash it shows when a Medic patches it up, and the sprites of the
// role visuals the PlayerAbility helpers draw (Medic aura, Tank shield arc,
// Support's party speed ring). One component on Ship.prefab serves all four
// roles, since a ship only learns its role at spawn.
public class ShipCombatFx : MonoBehaviour
{
    [Header("Superpowers (SpriteSequence prefabs)")]
    public GameObject tauntFx;
    public GameObject auraBoostFx;
    public GameObject speedBoostFx;
    public GameObject bigShotFx;
    public GameObject healFlashFx;

    [Header("Medic aura")]
    public Sprite auraIdle;
    public Sprite auraBoost;
    public Sprite auraBoostPulse;

    [Header("Tank shield arc")]
    public Sprite shieldIdle;
    public Sprite shieldBlock;

    [Header("Support party speed ring")]
    public Sprite speedRing;
    public Sprite speedRingEnd;

    public void PlayAbility(PlayerRole role) => SpawnChild(AbilityFxFor(role));

    public void PlayHealFlash() => SpawnChild(healFlashFx);

    private GameObject AbilityFxFor(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Tank: return tauntFx;
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
