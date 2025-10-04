using CandyCoded.HapticFeedback;
using Managers;
using UnityEngine;

public enum HapticFeedbackType
{
    Light,
    Medium,
    Heavy,
}
public class HapticFeedbackController : MonoBehaviour
{
    private static HapticFeedbackController _instance;

    public static HapticFeedbackController Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindFirstObjectByType<HapticFeedbackController>();

            return _instance;
        }
    }

    bool _isActive = true;
    public bool IsActive => _isActive;
    public void SetActive(bool value) => _isActive = value;

    public void PlayVibration(HapticFeedbackType type)
    {
        switch (type)
        {

            case HapticFeedbackType.Light:
                HapticFeedback.LightFeedback();
                break;
            case HapticFeedbackType.Medium:
                HapticFeedback.MediumFeedback();
                break;
            case HapticFeedbackType.Heavy:
                HapticFeedback.HeavyFeedback();
                break;
            default:
                HapticFeedback.LightFeedback();
                break;
        }
    }
}
