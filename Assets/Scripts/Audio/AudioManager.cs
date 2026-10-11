using UnityEngine;
using UnityEngine.Audio;
using Utils;

// Quan ly am thanh tap trung, ket noi giua GameEventBus va AudioConfigSO (SingletonDontDestroy)
public class AudioManager : Singleton<AudioManager>
{
    [Header("Cau Hinh Am Thanh (ScriptableObject)")]
    [SerializeField] private AudioConfigSO audioConfig;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("BGM Source (Gan tu Inspector)")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private bool autoPlayBgmOnStart = true;

    [Header("SFX Pool (Prefab & Parent gan tu Inspector)")]
    [SerializeField] private AudioSource sfxPrefab;
    [SerializeField] private Transform sfxParent;
    [SerializeField] private int sfxPoolSize = 16;

    private AudioSource[] sfxSources;
    private int currentSfxIndex;

    public AudioConfigSO AudioConfig => audioConfig;

    // Trang thai am thanh hien tai
    public bool IsBgmOn { get; private set; } = true;
    public bool IsSfxOn { get; private set; } = true;

    // Su kien thong bao thay doi trang thai am thanh
    public static event System.Action<bool> OnBgmStateChanged;
    public static event System.Action<bool> OnSfxStateChanged;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        // Doc trang thai am thanh tu PlayerData
        PlayerData playerData = SaveManager.LoadPlayerData();
        IsBgmOn = playerData.IsBgmOn;
        IsSfxOn = playerData.IsSfxOn;

        InitSfxPool();
    }

    // Khoi tao pool AudioSource tu prefab va transform cha
    private void InitSfxPool()
    {
        sfxSources = new AudioSource[sfxPoolSize];
        Transform parent = sfxParent != null ? sfxParent : transform;
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = Instantiate(sfxPrefab, parent);
            source.playOnAwake = false;
            source.loop = false;
            sfxSources[i] = source;
        }
    }

    private const string BgmVolumeParam = "BgmVolume";
    private const string SfxVolumeParam = "SfxVolume";
    private const float VolumeOnDb = 0f;
    private const float VolumeOffDb = -80f;

    private void Start()
    {
        ApplyMixerVolume(BgmVolumeParam, IsBgmOn);
        ApplyMixerVolume(SfxVolumeParam, IsSfxOn);

        if (autoPlayBgmOnStart && audioConfig != null && audioConfig.BgmMusic != null)
        {
            PlayBGM();
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnItemSwapped += OnItemSwappedHandler;
        GameEventBus.OnMatchExploded += OnMatchExplodedHandler;
        GameEventBus.OnBoosterCreated += OnBoosterCreatedHandler;
        GameEventBus.OnBoosterActivated += OnBoosterActivatedHandler;
        GameEventBus.OnBoosterComboActivated += OnBoosterComboActivatedHandler;
        GameEventBus.OnObstacleDestroyed += OnObstacleDestroyedHandler;
        GameEventBus.OnScoreChanged += OnScoreChangedHandler;
        GameEventBus.OnLevelComplete += OnLevelCompleteHandler;
        GameEventBus.OnLevelFailed += OnLevelFailedHandler;
        GameEventBus.OnUIButtonClicked += OnUIButtonClickedHandler;
    }

    private void OnDisable()
    {
        GameEventBus.OnItemSwapped -= OnItemSwappedHandler;
        GameEventBus.OnMatchExploded -= OnMatchExplodedHandler;
        GameEventBus.OnBoosterCreated -= OnBoosterCreatedHandler;
        GameEventBus.OnBoosterActivated -= OnBoosterActivatedHandler;
        GameEventBus.OnBoosterComboActivated -= OnBoosterComboActivatedHandler;
        GameEventBus.OnObstacleDestroyed -= OnObstacleDestroyedHandler;
        GameEventBus.OnScoreChanged -= OnScoreChangedHandler;
        GameEventBus.OnLevelComplete -= OnLevelCompleteHandler;
        GameEventBus.OnLevelFailed -= OnLevelFailedHandler;
        GameEventBus.OnUIButtonClicked -= OnUIButtonClickedHandler;
    }

    // Lay AudioSource tiep theo trong pool de phat am thanh
    private AudioSource GetNextSfxSource()
    {
        AudioSource source = sfxSources[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxSources.Length;
        return source;
    }

    // Phat 1 AudioEventSO thong qua SFX pool
    public void PlayEvent(AudioEventSO audioEvent, float pitchMultiplier = 1f)
    {
        if (!IsSfxOn || audioEvent == null) return;
        AudioSource source = GetNextSfxSource();
        audioEvent.Play(source, pitchMultiplier);
    }

    // Phat nhac nen BGM
    public void PlayBGM()
    {
        if (audioConfig != null && audioConfig.BgmMusic != null && bgmSource != null)
        {
            audioConfig.BgmMusic.PlayLoop(bgmSource);
        }
    }

    // Dung nhac nen BGM
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    // ==========================================
    // DIEU KHIEN BAT / TAT AM THANH QUA AUDIOMIXER
    // ==========================================

    private void ApplyMixerVolume(string paramName, bool isOn)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(paramName, isOn ? VolumeOnDb : VolumeOffDb);
        }
    }

    public void TurnOnBgm() => SetBgm(true);
    public void TurnOffBgm() => SetBgm(false);

    public void SetBgm(bool isOn)
    {
        IsBgmOn = isOn;
        ApplyMixerVolume(BgmVolumeParam, isOn);

        if (bgmSource != null && isOn && !bgmSource.isPlaying && audioConfig != null && audioConfig.BgmMusic != null)
        {
            PlayBGM();
        }

        PlayerData playerData = SaveManager.LoadPlayerData();
        playerData.IsBgmOn = isOn;
        SaveManager.SavePlayerData(playerData);

        OnBgmStateChanged?.Invoke(isOn);
    }

    public void TurnOnSfx() => SetSfx(true);
    public void TurnOffSfx() => SetSfx(false);

    public void SetSfx(bool isOn)
    {
        IsSfxOn = isOn;
        ApplyMixerVolume(SfxVolumeParam, isOn);

        PlayerData playerData = SaveManager.LoadPlayerData();
        playerData.IsSfxOn = isOn;
        SaveManager.SavePlayerData(playerData);

        OnSfxStateChanged?.Invoke(isOn);
    }

    // ==========================================
    // CAC HAM XU LY SU KIEN TU GAMEEVENTBUS
    // ==========================================

    private void OnItemSwappedHandler(bool isRevert)
    {
        if (audioConfig == null) return;
        PlayEvent(isRevert ? audioConfig.SwapBackward : audioConfig.SwapForward);
    }

    private void OnMatchExplodedHandler(int count, int cascadeStep)
    {
        if (audioConfig == null) return;

        // Tang cao do (pitch) theo nhip combo de tao cam giac thoa man
        float pitchMultiplier = Mathf.Min(1f + cascadeStep * 0.05f, 1.35f);
        PlayEvent(audioConfig.MatchNormal, pitchMultiplier);
    }

    private void OnBoosterCreatedHandler(EnumItemBoard boosterType)
    {
        if (audioConfig == null) return;
        PlayEvent(audioConfig.BoosterCreated);
    }

    private void OnBoosterActivatedHandler(EnumItemBoard boosterType)
    {
        if (audioConfig == null) return;

        switch (boosterType)
        {
            case EnumItemBoard.HorizontalRocket:
            case EnumItemBoard.VerticalRocket:
                PlayEvent(audioConfig.RocketLaunch);
                break;
            case EnumItemBoard.TNT:
                PlayEvent(audioConfig.TntExplode);
                break;
            case EnumItemBoard.Missile:
                PlayEvent(audioConfig.MissileExplode);
                break;
            case EnumItemBoard.LightBall:
                PlayEvent(audioConfig.LightBallPower);
                break;
        }
    }

    private void OnBoosterComboActivatedHandler(EnumItemBoard typeA, EnumItemBoard typeB)
    {
        if (audioConfig == null) return;

        if (typeA == EnumItemBoard.TNT && typeB == EnumItemBoard.TNT)
        {
            PlayEvent(audioConfig.MegaExplode);
        }
        else
        {
            PlayEvent(audioConfig.BoosterMerge);
        }
    }

    private void OnObstacleDestroyedHandler(EnumItemBoard obstacleType)
    {
        if (audioConfig == null) return;

        switch (obstacleType)
        {
            case EnumItemBoard.Stone:
                PlayEvent(audioConfig.StoneBreak);
                break;
            default:
                PlayEvent(audioConfig.StoneBreak);
                break;
        }
    }

    private void OnScoreChangedHandler(int newScore)
    {
        if (audioConfig == null || newScore <= 0) return;
        PlayEvent(audioConfig.ScoreIncrease);
    }

    private void OnLevelCompleteHandler()
    {
        if (audioConfig == null) return;
        PlayEvent(audioConfig.LevelWin);
    }

    private void OnLevelFailedHandler()
    {
        if (audioConfig == null) return;
        PlayEvent(audioConfig.LevelLose);
    }

    private void OnUIButtonClickedHandler()
    {
        if (audioConfig == null) return;
        PlayEvent(audioConfig.ButtonClick);
    }
}
