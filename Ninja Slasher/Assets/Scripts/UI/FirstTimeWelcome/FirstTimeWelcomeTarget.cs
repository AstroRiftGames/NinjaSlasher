using UnityEngine;

public sealed class FirstTimeWelcomeTarget : MonoBehaviour
{
    [SerializeField] private string _id;

    public string Id => _id;
    public RectTransform RectTransform => transform as RectTransform;
}
