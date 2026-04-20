using UnityEngine;
using System.Collections;

public class VictoryModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private float _delayBeforeShowingResults = 0.1f;

    private Coroutine _showResultsCoroutine;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopShowResultsCoroutine();
    }

    protected override void OnShown()
    {
        PlayVictoryAudio();

        if (ResultsUIManager.Instance != null)
            ResultsUIManager.Instance.PrepareResultsIntro();

        StopShowResultsCoroutine();
        _showResultsCoroutine = StartCoroutine(ShowResultsDelayed());
    }

    private IEnumerator ShowResultsDelayed()
    {
        yield return new WaitForSecondsRealtime(_delayBeforeShowingResults);
        _showResultsCoroutine = null;

        if (ResultsUIManager.Instance != null)
            ResultsUIManager.Instance.ShowResultsPanel();
    }

    protected override void OnHidden()
    {
        StopShowResultsCoroutine();
    }

    private void PlayVictoryAudio()
    {
        if (AudioService.Instance == null || _audioContext == null || _audioContext.Audio == null)
            return;

        bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;
        AudioEvent clip = isVictory ? _audioContext.Audio.victory : _audioContext.Audio.defeat;

        if (clip != null)
            AudioService.Instance.PlaySFX(clip);
    }

    private void StopShowResultsCoroutine()
    {
        if (_showResultsCoroutine == null)
            return;

        StopCoroutine(_showResultsCoroutine);
        _showResultsCoroutine = null;
    }
}
