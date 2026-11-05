using UnityEngine;

// Plays the playfield callouts at the right moments: WAVE when the escort
// wave spawns, the boss-entry WARNING when the boss glides in (then reveals
// the boss panel on the right rail as the banner leaves), and PHASE 2 on the
// boss's phase change. Polls LevelSequencer's state, which has no events.
// Bound by LevelSetup's onSpawned event.
public class LevelCallouts : MonoBehaviour
{
    public LevelSequencer sequencer;
    public CalloutBannerView banner;
    public BossPanelView bossPanel;

    private Level level;
    private LevelSequencer.SequenceState lastState;

    public void Bind(GameObject bossObject)
    {
        MonoBehaviour boss = bossObject.GetComponent<IBoss>() as MonoBehaviour;
        level = BossEvents.LevelOf(boss);
        BossEvents.Phase2(boss)?.AddListener(ShowPhase2);
    }

    void Update()
    {
        LevelSequencer.SequenceState state = sequencer.CurrentState;
        if (state == lastState) return;
        lastState = state;
        if (state == LevelSequencer.SequenceState.MinionPhase1) banner.Play(CalloutSpecs.Wave);
        if (state == LevelSequencer.SequenceState.BossEntrance) banner.Play(CalloutSpecs.BossEntry(level), bossPanel.Reveal);
        if (state == LevelSequencer.SequenceState.BossCombat) bossPanel.Reveal();
    }

    private void ShowPhase2() => banner.Play(CalloutSpecs.Phase2(level));
}
