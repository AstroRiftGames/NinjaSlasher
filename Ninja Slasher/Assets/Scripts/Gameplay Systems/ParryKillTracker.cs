public static class ParryKillTracker
{
    public static bool KillWithParryPerformed { get; private set; } = false;

    public static void RegisterParryKill()
    {
        KillWithParryPerformed = true;
    }

    public static void Reset()
    {
        KillWithParryPerformed = false;
    }
}
