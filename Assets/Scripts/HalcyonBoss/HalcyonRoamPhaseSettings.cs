// One phase's roam tuning, grouped so HalcyonRoam can hold a phase1 and a
// phase2 copy as two Inspector-visible fields. Speed is re-rolled within
// [speedMin, speedMax] on every waypoint and at random intervals mid-glide,
// so the boss's arrival time can't be read off its current speed.
[System.Serializable]
public class HalcyonRoamPhaseSettings
{
    public float speedMin = 3.5f;
    public float speedMax = 5.5f;
    public float pauseMin = 0.2f;
    public float pauseMax = 0.5f;
    public float speedChangeIntervalMin = 0.5f;
    public float speedChangeIntervalMax = 1.2f;
}
