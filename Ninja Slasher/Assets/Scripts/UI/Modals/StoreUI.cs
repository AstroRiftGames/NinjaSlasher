using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de UI para el panel de la tienda.
/// Muestra los precios de cada bundle y conecta los botones con StorePurchaseHandler.
///
/// Setup en Inspector:
///   _handler          → StorePurchaseHandler del mismo GameObject (o arrastrar referencia)
///   _smallBtn         → botón "Comprar Small"
///   _mediumBtn        → botón "Comprar Medium"
///   _largeBtn         → botón "Comprar Large"
///   _smallPriceText   → TMP_Text del precio small
///   _mediumPriceText  → TMP_Text del precio medium
///   _largePriceText   → TMP_Text del precio large
/// </summary>
public class StoreUI : MonoBehaviour
{
    [Header("Handler")]
    [SerializeField] private StorePurchaseHandler _handler;

    [Header("Buttons")]
    [SerializeField] private Button _smallBtn;
    [SerializeField] private Button _mediumBtn;
    [SerializeField] private Button _largeBtn;

    [Header("Price Labels")]
    [SerializeField] private TMP_Text _smallPriceText;
    [SerializeField] private TMP_Text _mediumPriceText;
    [SerializeField] private TMP_Text _largePriceText;

    private void Awake()
    {
        if (_handler == null)
            _handler = GetComponentInParent<StorePurchaseHandler>()
                    ?? GetComponent<StorePurchaseHandler>();
    }

    private void OnEnable()
    {
        _smallBtn?.onClick.AddListener(OnSmallClicked);
        _mediumBtn?.onClick.AddListener(OnMediumClicked);
        _largeBtn?.onClick.AddListener(OnLargeClicked);

        if (IAPManager.Instance == null) return;

        if (IAPManager.Instance.IsInitialized)
            RefreshPrices();
        else
            IAPManager.Instance.OnIAPInitialized += RefreshPrices;
    }

    private void OnDisable()
    {
        _smallBtn?.onClick.RemoveListener(OnSmallClicked);
        _mediumBtn?.onClick.RemoveListener(OnMediumClicked);
        _largeBtn?.onClick.RemoveListener(OnLargeClicked);

        if (IAPManager.Instance != null)
            IAPManager.Instance.OnIAPInitialized -= RefreshPrices;
    }

    private void RefreshPrices()
    {
        if (_handler == null) return;

        SetPrice(_smallPriceText,  _handler.GetSmallBentoPrice());
        SetPrice(_mediumPriceText, _handler.GetMediumBentoPrice());
        SetPrice(_largePriceText,  _handler.GetLargeBentoPrice());
    }

    private static void SetPrice(TMP_Text label, string price)
    {
        if (label != null)
            label.text = price;
    }

    private void OnSmallClicked()  => _handler?.BuySmallBento();
    private void OnMediumClicked() => _handler?.BuyMediumBento();
    private void OnLargeClicked()  => _handler?.BuyLargeBento();
}
