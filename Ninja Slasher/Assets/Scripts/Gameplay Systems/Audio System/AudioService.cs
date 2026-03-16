using UnityEngine;

public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [Header("AUDIO SETTINGS")]
    [SerializeField] private AudioSettingsSO audioSettings;
    public AudioSettingsSO AudioSettings => audioSettings;
    [SerializeField] private UserConfig audioConfig;

    [Header("SOURCE")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX POOL")]
    [SerializeField] private PooledAudioSource audioSourcePrefab;
    [SerializeField] private int sfxPoolSize = 10;

    private MusicPlayer _musicPlayer;
    private SFXPlayer _sfxPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePlayers();
        InitializeSettings();
    }

    private void OnEnable()
    {
        if (_musicPlayer == null || _sfxPlayer == null) return;

        ApplyMusicState(audioConfig.MusicEnabled);
        ApplySFXState(audioConfig.SFXEnabled);
    }

    private void Start()
    {
        MusicEvents.OnEnterSplash?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (audioConfig != null)
        {
            audioConfig.OnMusicEnabledChanged -= OnMusicEnabledChanged;
            audioConfig.OnSFXEnabledChanged   -= OnSFXEnabledChanged;
            audioConfig.OnSaveStateRequested  -= PersistAudioSettings;
        }

        SaveManager.OnDataLoaded -= OnSaveDataLoaded;
    }

    private void InitializePlayers()
    {
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
        }

        _musicPlayer = new MusicPlayer(musicSource, audioSettings, this);

        if (audioSourcePrefab == null) CreateAudioSourcePrefab();

        _sfxPlayer = new SFXPlayer(audioSourcePrefab, sfxPoolSize, audioSettings, transform);
    }

    private void InitializeSettings()
    {
        audioSettings.Load();

        audioConfig.OnMusicEnabledChanged += OnMusicEnabledChanged;
        audioConfig.OnSFXEnabledChanged   += OnSFXEnabledChanged;
        audioConfig.OnSaveStateRequested  += PersistAudioSettings;

        SaveManager.OnDataLoaded += OnSaveDataLoaded;

        if (SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded)
        {
            ApplySettingsFromSave(SaveManager.Instance.GetGameData());
        }
        else
        {
            ApplyMusicState(audioConfig.MusicEnabled);
            ApplySFXState(audioConfig.SFXEnabled);
        }
    }

    private void OnSaveDataLoaded(GameData data)
    {
        ApplySettingsFromSave(data);
    }

    private void ApplySettingsFromSave(GameData data)
    {
        audioConfig.InitializeState(data.musicEnabled, data.sfxEnabled);
    }

    private void OnMusicEnabledChanged(bool enabled) => ApplyMusicState(enabled);
    private void OnSFXEnabledChanged(bool enabled)   => ApplySFXState(enabled);

    private void ApplyMusicState(bool enabled)
    {
        if (enabled) _musicPlayer.UnmuteMusic();
        else         _musicPlayer.MuteMusic();
    }

    private void ApplySFXState(bool enabled)
    {
        if (enabled) _sfxPlayer.UnmuteSFX();
        else         _sfxPlayer.MuteSFX();
    }

    private void PersistAudioSettings()
    {
        var sm = SaveManager.Instance;
        if (sm == null) return;

        sm.Modify(data =>
        {
            data.musicEnabled = audioConfig.MusicEnabled;
            data.sfxEnabled   = audioConfig.SFXEnabled;
        });
    }

    public void PlayMusic(AudioEvent audioEvent, float fadeTime = 0f)
        => _musicPlayer.Play(audioEvent, fadeTime);

    public void StopMusic(float fadeTime = 0f)
        => _musicPlayer.Stop(fadeTime);

    public void PauseMusic()
        => _musicPlayer.Pause();

    public void ResumeMusic()
        => _musicPlayer.Resume();

    public void PlaySFX(AudioEvent audioEvent)
        => _sfxPlayer.Play(audioEvent);

    public void PlaySFXAtPosition(AudioEvent audioEvent, Vector3 position)
        => _sfxPlayer.PlayAtPosition(audioEvent, position);

    public void StopSFX(AudioEvent audioEvent)
        => _sfxPlayer.Stop(audioEvent);

    public void StopAllSFX()
        => _sfxPlayer.StopAll();

    public void RefreshVolumes()
    {
        _musicPlayer?.UpdateVolume();
        _sfxPlayer?.RefreshVolumes();
    }

    private void CreateAudioSourcePrefab()
    {
        GameObject prefabGO = new GameObject("PooledAudioSource_Prefab");
        prefabGO.transform.SetParent(transform);
        prefabGO.AddComponent<AudioSource>();
        audioSourcePrefab = prefabGO.AddComponent<PooledAudioSource>();
        prefabGO.SetActive(false);
    }
}
