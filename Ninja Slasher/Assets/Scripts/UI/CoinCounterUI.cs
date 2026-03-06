using DG.Tweening;
using TMPro;
using UnityEngine;

public class CoinCounterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private RectTransform   _coinIcon;

    [Header("Counter Animation")]
    [SerializeField] private float _animationDuration = 0.7f;
    [SerializeField] private Ease  _ease = Ease.OutCubic;

    [SerializeField] private bool    _punchIcon      = true;
    [SerializeField] private Vector3 _iconPunch      = new Vector3(0.35f, 0.35f, 0f);
    [SerializeField] private float   _iconPunchDuration = 0.4f;

    [SerializeField] private bool    _punchLabel     = true;
    [SerializeField] private Vector3 _labelPunch     = new Vector3(0.2f, 0.2f, 0f);
    [SerializeField] private float   _labelPunchDuration = 0.3f;

    [Header("Audio")]
    [SerializeField] private UIAudioContext _audioContext;

    private int   _displayedCoins;
    private Tween _countTween;

    private void OnEnable()
    {
        GameEvents.OnCoinsChanged += AnimateTo;

        int saved = SaveManager.Instance != null ? SaveManager.Instance.GetCoins() : 0;
        SetImmediate(saved);
    }

    private void OnDisable()
    {
        GameEvents.OnCoinsChanged -= AnimateTo;
        _countTween?.Kill();
    }

    public void SetImmediate(int value)
    {
        _countTween?.Kill();
        _displayedCoins = value;
        UpdateLabel(value);
    }

    private void AnimateTo(int targetCoins)
    {
        _countTween?.Kill();

        int animatedValue = _displayedCoins;

        _countTween = DOTween
            .To(
                getter: ()  => animatedValue,
                setter: val =>
                {
                    animatedValue   = val;
                    _displayedCoins = val;
                    UpdateLabel(val);
                },
                endValue: targetCoins,
                duration: _animationDuration)
            .SetEase(_ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _displayedCoins = targetCoins;
                UpdateLabel(targetCoins);
            });

        PlayPolish();
    }

    private void UpdateLabel(int value)
    {
        if (_label != null)
            _label.text = value.ToString();
    }

    private void PlayPolish()
    {
        if (_punchIcon && _coinIcon != null)
        {
            DOTween.Kill(_coinIcon);
            _coinIcon
                .DOPunchScale(_iconPunch, _iconPunchDuration, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(true);
        }

        if (_punchLabel && _label != null)
        {
            DOTween.Kill(_label.rectTransform);
            _label.rectTransform
                .DOPunchScale(_labelPunch, _labelPunchDuration, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(true);
        }

        if (_audioContext?.Audio?.rewardCoins != null)
            AudioService.Instance?.PlaySFX(_audioContext.Audio.rewardCoins);
    }
}
