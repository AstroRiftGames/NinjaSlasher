using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;

public class TutorialUIOverlay : UIOverlayBase, IPointerClickHandler
{
    [Header("REFERENCES")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Button closeButton;

    [Header("INPUT")]
    [SerializeField] private bool closeOnBackgroundTap = false;

    public event Action Dismissed;
    private bool _wasPausedBeforeShow;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(NotifyDismissed);
            closeButton.onClick.AddListener(NotifyDismissed);
        }
    }

    protected override void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(NotifyDismissed);

        base.OnDisable();
    }

    public void Bind(TutorialPanelData data)
    {
        if (titleText != null)
            titleText.text = data != null ? data.title : string.Empty;

        if (descriptionText != null)
            descriptionText.text = data != null ? data.description : string.Empty;

        ConfigureVideo(data != null ? data.video : null);
    }

    protected override void OnShown()
    {
        base.OnShown();

        _wasPausedBeforeShow = Time.timeScale == 0f;

        if (!_wasPausedBeforeShow)
            Time.timeScale = 0f;

        UIEvents.RaisePause(true);

        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }
    }

    protected override void OnHidden()
    {
        base.OnHidden();

        if (!_wasPausedBeforeShow)
            Time.timeScale = 1f;

        UIEvents.RaisePause(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            ClearTargetTexture();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!closeOnBackgroundTap)
            return;

        NotifyDismissed();
    }

    public void Dismiss()
    {
        NotifyDismissed();
    }

    private void ConfigureVideo(VideoClip clip)
    {
        if (videoPlayer == null)
            return;

        videoPlayer.clip = clip;

        if (clip == null)
        {
            videoPlayer.Stop();
            ClearTargetTexture();
        }
    }

    private void ClearTargetTexture()
    {
        if (videoPlayer == null || videoPlayer.targetTexture == null)
            return;

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = videoPlayer.targetTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = active;
    }

    private void NotifyDismissed()
    {
        Dismissed?.Invoke();
    }

    private void ResolveReferences()
    {
        if (closeButton == null)
        {
            foreach (Button candidate in GetComponentsInChildren<Button>(true))
            {
                string buttonName = candidate.name.ToLowerInvariant();
                if (buttonName.Contains("close") || buttonName.Contains("cerrar"))
                {
                    closeButton = candidate;
                    break;
                }
            }
        }

        if (closeButton == null)
            Debug.LogWarning("[TutorialPanelView] No se encontró un botón de cierre asignado ni autodetectable.");
    }
}
