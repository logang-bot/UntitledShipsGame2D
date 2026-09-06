// Nakama match-state opcodes shared between sender and receiver scripts.
// One constant per message shape sent over SendMatchStateAsync - grows as
// later phases add state-snapshot and bullet-event messages.
public static class NetworkOpCodes
{
    public const long PlayerInput = 1;
    public const long StateSnapshot = 2;
    public const long RolePick = 3;
    public const long GameStart = 4;
}
