using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class FloatingComboTextManager : MonoBehaviourSingleton<FloatingComboTextManager>
{
    [Header("REFERENCES")]
    [SerializeField] private FloatingComboText _floatingTextPrefab;
    [SerializeField] private Canvas _targetCanvas;
    [SerializeField] private int _poolSize = 10;

    [Header("SETTINGS")]
    [SerializeField]
    private ComboTextData[] _comboLevels = new ComboTextData[]
    {
        new ComboTextData { message = "COMBO x2!", color = new Color(255, 255, 0) },
        new ComboTextData { message = "COMBO x3!!", color = new Color(255, 128, 0) },
        new ComboTextData { message = "COMBO x4!!!", color = new Color(255, 77, 0) },
        new ComboTextData { message = "COMBO x5!!!!", color = new Color(255, 0, 0) }
    };

    private Queue<FloatingComboText> _textPool = new Queue<FloatingComboText>();
    private List<FloatingComboText> _activeTexts = new List<FloatingComboText>();

    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeToComboManager();
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.OnComboUpdatedWithPosition -= HandleComboUpdated;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ReinitializePoolForNewScene();
        SubscribeToComboManager();
    }

    private Canvas FindGameplayCanvas()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Contains("Gameplay") || canvas.name.Contains("gameplay"))
            {
                return canvas;
            }
        }
        return FindObjectOfType<Canvas>();
    }

    private void Start()
    {
        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();
        }

        SubscribeToComboManager();
    }

    private void SubscribeToComboManager()
    {
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.OnComboUpdatedWithPosition -= HandleComboUpdated;
            ComboManager.Instance.OnComboUpdatedWithPosition += HandleComboUpdated;
        }
        else
        {
            StartCoroutine(RetrySubscription());
        }
    }

    private System.Collections.IEnumerator RetrySubscription()
    {
        yield return new WaitForSeconds(0.5f);

        if (ComboManager.Instance != null)
        {
            SubscribeToComboManager();
        }
    }

    private void InitializePool()
    {
        if (_floatingTextPrefab == null)
        {
            return;
        }

        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();

            if (_targetCanvas == null)
            {
                return;
            }
        }

        for (int i = 0; i < _poolSize; i++)
        {
            CreateNewText();
        }
    }

    private void ReinitializePoolForNewScene()
    {
        foreach (var text in _activeTexts.ToArray())
        {
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }
        _activeTexts.Clear();

        while (_textPool.Count > 0)
        {
            var text = _textPool.Dequeue();
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        if (_targetCanvas == null)
        {
            _targetCanvas = FindGameplayCanvas();
        }

        if (_targetCanvas != null)
        {
            InitializePool();
        }
    }

    private FloatingComboText CreateNewText()
    {
        FloatingComboText newText = Instantiate(_floatingTextPrefab, _targetCanvas.transform);
        newText.gameObject.SetActive(false);
        _textPool.Enqueue(newText);
        return newText;
    }

    private FloatingComboText GetFromPool()
    {
        if (_textPool.Count == 0)
        {
            return CreateNewText();
        }

        FloatingComboText text = _textPool.Dequeue();
        text.gameObject.SetActive(true);
        _activeTexts.Add(text);
        return text;
    }

    public void ReturnToPool(FloatingComboText text)
    {
        if (text == null) return;

        text.gameObject.SetActive(false);
        _activeTexts.Remove(text);
        _textPool.Enqueue(text);
    }

    private void HandleComboUpdated(int level, Vector3 enemyPosition)
    {
        if (level < 2)
        {
            return;
        }

        ComboTextData data = GetComboData(level);
        if (data == null)
        {
            return;
        }
        ShowFloatingText(data.message, enemyPosition, data.color);
    }

    private ComboTextData GetComboData(int level)
    {
        int index = Mathf.Clamp(level - 2, 0, _comboLevels.Length - 1);
        return _comboLevels[index];
    }

    public void ShowFloatingText(string message, Vector3 worldPosition, Color color)
    {
        FloatingComboText text = GetFromPool();

        if (text == null)
        {            
            return;
        }
        text.Show(message, worldPosition, color);
    }
}

[Serializable]
public class ComboTextData
{
    public string message = "COMBO!";
    public Color color = Color.white;
}