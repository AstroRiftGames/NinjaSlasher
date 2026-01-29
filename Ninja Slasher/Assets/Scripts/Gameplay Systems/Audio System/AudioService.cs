using UnityEngine;

public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private AudioSettings audioSettings = new AudioSettings();

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Pool")]
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

        InitializeSettings();
        InitializePlayers();
    }

    private void Start()
    {
        MusicEvents.OnEnterSplash?.Invoke();
    }

    private void InitializeSettings()
    {
        audioSettings.Load();
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

        if (audioSourcePrefab == null)
        {
            CreateAudioSourcePrefab();
        }
        _sfxPlayer = new SFXPlayer(audioSourcePrefab, sfxPoolSize, audioSettings, transform);
    }

    private void CreateAudioSourcePrefab()
    {
        GameObject prefabGO = new GameObject("PooledAudioSource_Prefab");
        prefabGO.transform.SetParent(transform);
        prefabGO.AddComponent<AudioSource>();
        audioSourcePrefab = prefabGO.AddComponent<PooledAudioSource>();
        prefabGO.SetActive(false);
    }

    #region PUBLIC API

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

    public AudioSettings Settings => audioSettings;

    public void SaveSettings()
        => audioSettings.Save();

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}