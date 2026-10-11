using UnityEngine;

// Bang cau hinh trung tam chua toan bo AudioEventSO cua du an
[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Config")]
public class AudioConfigSO : ScriptableObject
{
    [Header("BGM (Nhac Nen)")]
    [SerializeField] private AudioEventSO bgmMusic;

    [Header("Thao Tac Vuot (Swap)")]
    [SerializeField] private AudioEventSO swapForward;
    [SerializeField] private AudioEventSO swapBackward;

    [Header("No Match & Diem So")]
    [SerializeField] private AudioEventSO matchNormal;
    [SerializeField] private AudioEventSO matchExplode;
    [SerializeField] private AudioEventSO scoreIncrease;

    [Header("Sinh Booster")]
    [SerializeField] private AudioEventSO boosterCreated;

    [Header("Kich Hoat Booster Don")]
    [SerializeField] private AudioEventSO rocketLaunch;
    [SerializeField] private AudioEventSO missileFly;
    [SerializeField] private AudioEventSO missileExplode;
    [SerializeField] private AudioEventSO tntFuse;
    [SerializeField] private AudioEventSO tntExplode;
    [SerializeField] private AudioEventSO lightBallPower;
    [SerializeField] private AudioEventSO lightBallBeam;

    [Header("Combo Booster")]
    [SerializeField] private AudioEventSO boosterMerge;
    [SerializeField] private AudioEventSO megaExplode;

    [Header("Chuong Ngai Vat (Obstacles)")]
    [SerializeField] private AudioEventSO stoneBreak;
    [SerializeField] private AudioEventSO bushBreak;
    [SerializeField] private AudioEventSO grassBreak;
    [SerializeField] private AudioEventSO mailBoxBreak;

    [Header("Giao Dien (UI) & Ket Thuc Man")]
    [SerializeField] private AudioEventSO levelWin;
    [SerializeField] private AudioEventSO levelLose;
    [SerializeField] private AudioEventSO buttonClick;

    // Public Getters
    public AudioEventSO BgmMusic => bgmMusic;
    public AudioEventSO SwapForward => swapForward;
    public AudioEventSO SwapBackward => swapBackward;
    public AudioEventSO MatchNormal => matchNormal;
    public AudioEventSO MatchExplode => matchExplode;
    public AudioEventSO ScoreIncrease => scoreIncrease;
    public AudioEventSO BoosterCreated => boosterCreated;
    public AudioEventSO RocketLaunch => rocketLaunch;
    public AudioEventSO MissileFly => missileFly;
    public AudioEventSO MissileExplode => missileExplode;
    public AudioEventSO TntFuse => tntFuse;
    public AudioEventSO TntExplode => tntExplode;
    public AudioEventSO LightBallPower => lightBallPower;
    public AudioEventSO LightBallBeam => lightBallBeam;
    public AudioEventSO BoosterMerge => boosterMerge;
    public AudioEventSO MegaExplode => megaExplode;
    public AudioEventSO StoneBreak => stoneBreak;
    public AudioEventSO BushBreak => bushBreak;
    public AudioEventSO GrassBreak => grassBreak;
    public AudioEventSO MailBoxBreak => mailBoxBreak;
    public AudioEventSO LevelWin => levelWin;
    public AudioEventSO LevelLose => levelLose;
    public AudioEventSO ButtonClick => buttonClick;
}
