// One Warden arm card: its letter, status, the role it's locked onto, and
// while down the seconds left and the recovery bar (0..1 remaining).
public struct ArmCardState
{
    public string letter;
    public ArmStatus status;
    public PlayerRole? target;
    public float downSeconds;
    public float downLeft;
}
