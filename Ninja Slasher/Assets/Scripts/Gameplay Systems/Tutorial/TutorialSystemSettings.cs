using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSystemSettings", menuName = "Tutorial/Tutorial System Settings")]
public class TutorialSystemSettings : ScriptableObject
{
    [Header("REFERENCES")]
    public TutorialUIOverlay tutorialPanelPrefab;
}
