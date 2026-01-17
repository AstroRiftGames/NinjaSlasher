using UnityEngine;

public class LevelSessionStarter : MonoBehaviour
{
    [Header("AUTO START SETTINGS")]
    [SerializeField] private bool autoStartOnReady = true;
    [SerializeField] private float delayBeforeStart = 0.5f;

    private bool hasStarted = false;

    private void Start()
    {
        if (autoStartOnReady)
        {
            Invoke(nameof(StartLevelSession), delayBeforeStart);
        }
    }

    private void StartLevelSession()
    {
        if (hasStarted) return;

        if (LevelSessionManager.Instance == null)
        {
            Debug.LogError("[LevelSessionStarter] LevelSessionManager no encontrado");
            return;
        }

        if (!LevelSessionManager.Instance.HasActiveSession)
        {
            Debug.LogWarning("[LevelSessionStarter] No hay sesión activa para iniciar");
            return;
        }

        LevelSessionManager.Instance.StartLevel();
        hasStarted = true;

        Debug.Log("[LevelSessionStarter] Nivel iniciado automáticamente");
    }

    public void ManualStart()
    {
        if (hasStarted)
        {
            Debug.LogWarning("[LevelSessionStarter] El nivel ya fue iniciado");
            return;
        }

        StartLevelSession();
    }
}