using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialCompletionTrigger
{
    None = 0,
    DashStarted = 1,
    EnemyKilled = 2,
    ComboUpdated = 3,
    ParrySuccessful = 4,
    AutoAdvance = 5
}

public enum TutorialHandIndicator
{
    None = 0,
    Dash = 1,
    Parry = 2
}

[Serializable]
public class TutorialStepDefinition
{
    public string stepId;
    public TutorialCompletionTrigger completionTrigger = TutorialCompletionTrigger.None;
    public TutorialHandIndicator handIndicator = TutorialHandIndicator.None;
    public bool showAllBoundTexts = false;
    public float autoAdvanceDelay = 0f;
    public float triggerCompletionDelay = 0f;
    public float armingDelay = 0f;
}

[CreateAssetMenu(fileName = "TutorialDefinition", menuName = "Tutorial Definition")]
public class TutorialDefinition : ScriptableObject
{
    public string tutorialId;
    public List<TutorialStepDefinition> steps = new List<TutorialStepDefinition>();
}

[Serializable]
public class TutorialSceneBinding
{
    public string stepId;
    public GameObject[] textObjects;
}
