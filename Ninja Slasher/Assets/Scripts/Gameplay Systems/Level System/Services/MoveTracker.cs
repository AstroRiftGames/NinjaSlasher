using UnityEngine;

public static class MoveTracker
{
    public static int TotalMoves
    {
        get
        {
            if (LevelSessionManager.Instance?.IsSessionRunning ?? false)
            {
                return LevelSessionManager.Instance.GetCurrentStats().movesUsed;
            }
            return 0;
        }
    }

    public static event System.Action OnMoveRegistered;

    public static void RegisterMove()
    {
        if (LevelSessionManager.Instance == null)
        {
            Debug.LogWarning("[MoveTrackerAdapter] LevelSessionManager no disponible");
            return;
        }

        LevelSessionManager.Instance.RegisterMove();
        OnMoveRegistered?.Invoke();
    }
}
