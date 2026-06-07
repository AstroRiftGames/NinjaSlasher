using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FirstTimeWelcomeConfig", menuName = "Game/UI/First Time Welcome Config")]
public class FirstTimeWelcomeConfig : ScriptableObject
{
    [SerializeField] private FirstTimeWelcomeStep[] _steps;

    public int StepCount => _steps != null ? _steps.Length : 0;

    public FirstTimeWelcomeStep GetStep(int index)
    {
        if (_steps == null || index < 0 || index >= _steps.Length)
            return null;

        return _steps[index];
    }
}

[Serializable]
public sealed class FirstTimeWelcomeStep
{
    [SerializeField] private string _targetId;
    [SerializeField] private string _message;

    public string TargetId => _targetId;
    public string Message => _message;
}
