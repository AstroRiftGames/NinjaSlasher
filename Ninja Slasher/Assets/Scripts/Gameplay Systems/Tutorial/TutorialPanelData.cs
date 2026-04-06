using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "TutorialPanelData", menuName = "Tutorial/Tutorial Panel Data")]
public class TutorialPanelData : ScriptableObject
{
    [Header("CONTENT")]
    public VideoClip video;
    public string title;

    [TextArea(3, 6)]
    public string description;
}
