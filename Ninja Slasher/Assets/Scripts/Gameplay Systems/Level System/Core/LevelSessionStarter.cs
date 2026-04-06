using UnityEngine;

public class LevelSessionStarter : MonoBehaviour
{
    [Header("AUTO START SETTINGS")]
    [SerializeField] private bool autoStartOnReady = true;

    private bool hasStarted = false;
    private Coroutine _startRoutine;

    private void Start()
    {
        if (autoStartOnReady)
        {
            _startRoutine = StartCoroutine(WaitForReadyAndStart());
        }
    }

    private System.Collections.IEnumerator WaitForReadyAndStart()
    {
        while (!hasStarted)
        {
            if (LevelSessionManager.Instance != null &&
                LevelSessionManager.Instance.CurrentSession != null &&
                LevelSessionManager.Instance.CurrentSession.State == LevelSessionState.Ready &&
                (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive()))
            {
                StartLevelSession();
                yield break;
            }

            yield return null;
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

    private void OnDisable()
    {
        if (_startRoutine != null)
        {
            StopCoroutine(_startRoutine);
            _startRoutine = null;
        }
    }
}
