using System.Collections;
using UnityEngine;

// Lines the level track up with the fight: the song's bossCombatAtSeconds
// lands on the frame LevelSequencer unfreezes the ships for boss combat.
// Every pre-fight step is timed except the wait for the last wave enemy to
// die, so the track starts where an instant clear would land it, and when
// the boss entrance begins (a fixed time before combat) a short volume dip
// re-seeks it if that wait threw it off. Once combat starts, the track
// loops back to the fight's moment instead of the pre-fight part.
public class BossMusicCue : MonoBehaviour
{
    public MusicTrack music;
    public LevelSequencer sequencer;
    public float bossCombatAtSeconds = 87f;
    public float driftToleranceSeconds = 0.25f;
    public float dipFadeSeconds = 0.3f;

    private LevelSequencer.SequenceState lastState;

    void Awake() => music.startAtSeconds = Mathf.Max(0f, bossCombatAtSeconds - TimedPreFightSeconds());

    void Update()
    {
        LevelSequencer.SequenceState state = sequencer.CurrentState;
        if (state != lastState) OnStateChanged(state);
        lastState = state;
    }

    private void OnStateChanged(LevelSequencer.SequenceState state)
    {
        if (state == LevelSequencer.SequenceState.BossEntrance) AlignForEntrance();
        if (state == LevelSequencer.SequenceState.BossCombat) music.startAtSeconds = bossCombatAtSeconds;
    }

    private void AlignForEntrance()
    {
        float onTrack = bossCombatAtSeconds - sequencer.bossEntranceDuration;
        if (Mathf.Abs(music.source.time - onTrack) <= driftToleranceSeconds) return;
        StartCoroutine(music.JumpTo(onTrack + dipFadeSeconds, dipFadeSeconds));
    }

    private float TimedPreFightSeconds()
    {
        return sequencer.introDuration + sequencer.freeMovementDuration
            + sequencer.minionPhase1Duration + sequencer.bossEntranceDuration;
    }
}
