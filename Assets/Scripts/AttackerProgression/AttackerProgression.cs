using UnityEngine;

// The human Attacker's run: earns XP from every mob the party kills, banks a
// pick per level-up for LevelUpUI, applies picked cards to the ship, and casts
// Crit Strike once unlocked. Does nothing if no human is playing Attacker.
// See docs/systems/roguelike-progression.md.
public class AttackerProgression : MonoBehaviour
{
    public PartyFrameManager partyFrameManager; // the live roster, same source GameOverUI reads

    [Header("Crit Strike")]
    public float critInterval = 3f;
    public float critWidthMultiplier = 1.5f;
    public Color critColor = Color.red;

    public PlayerController Ship { get; private set; }
    public AttackerXpTrack Xp { get; } = new AttackerXpTrack();
    public AttackerUpgrades Upgrades { get; } = new AttackerUpgrades();
    public int PendingPicks { get; private set; }
    public float LastCritTime { get; private set; } = float.NegativeInfinity;

    private float nextCritTime;

    public float CritCooldownRemaining => Mathf.Max(0f, nextCritTime - Time.time);

    // Awake, not Start: PartyFrameManager.IsHuman is only valid before
    // LevelSequencer's first freeze disables PlayerInput.
    void Awake()
    {
        if (partyFrameManager == null || partyFrameManager.players == null) return;
        foreach (GameObject candidate in partyFrameManager.players)
        {
            if (!PartyFrameManager.IsHuman(candidate)) continue;
            if (candidate.GetComponent<PlayerRoleComponent>().role == PlayerRole.Attacker)
                Ship = candidate.GetComponent<PlayerController>();
        }
    }

    void OnEnable() { MobKills.Killed += OnMobKilled; }
    void OnDisable() { MobKills.Killed -= OnMobKilled; }

    void OnMobKilled()
    {
        if (Ship != null && Xp.AddXp()) PendingPicks++;
    }

    public void ApplyCard(UpgradeCard card)
    {
        bool unlockingCrit = card == UpgradeCard.CritStrike && !Upgrades.CritUnlocked;
        Upgrades.Apply(card);
        PendingPicks--;

        Ship.fireDamageBonus = Upgrades.DamageBonus;
        Ship.shotsPerAttack = Upgrades.ShotCount;
        if (unlockingCrit) nextCritTime = Time.time + critInterval;
    }

    void Update()
    {
        // isActiveAndEnabled covers both a dead ship and LevelSequencer's freeze.
        if (Ship == null || !Ship.isActiveAndEnabled || !Upgrades.CritUnlocked) return;
        if (Time.time < nextCritTime) return;

        nextCritTime = Time.time + critInterval;
        LastCritTime = Time.time;
        Ship.FireCrit(Upgrades.CritDamage(Ship.CurrentDamage), critWidthMultiplier, critColor);
    }
}
