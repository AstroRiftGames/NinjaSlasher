using UnityEngine;

public static class ParryKillTracker
{
    public static bool KillWithParryPerformed
    {
        get
        {
            if (LevelSessionManager.Instance?.HasActiveSession ?? false)
            {
                return LevelSessionManager.Instance.GetCurrentStats().parryKillDone;
            }
            return false;
        }
    }

    public static void RegisterParryKill()
    {
        if (LevelSessionManager.Instance == null)
        {
            Debug.LogWarning("[ParryKillTrackerAdapter] LevelSessionManager no disponible");
            return;
        }

        LevelSessionManager.Instance.RegisterParryKill();
        
        Debug.Log("[ParryKillTrackerAdapter] Parry kill registrado");
    }
}



