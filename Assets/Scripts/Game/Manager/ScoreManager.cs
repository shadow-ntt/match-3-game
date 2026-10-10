using UnityEngine;
using Utils;

// Quan ly tinh diem va he so combo lien hoan (Cascade Multiplier)
public class ScoreManager : Singleton<ScoreManager>
{
    [Header("Cau hinh Diem co ban")]
    [SerializeField] private int baseGemScore = 20;
    [SerializeField] private int obstacleScore = 50;
    [SerializeField] private int boosterScore = 40;

    [Header("Cau hinh He so Combo")]
    [SerializeField] private float cascadeMultiplierStep = 0.5f;
    [SerializeField] private float maxMultiplier = 3.0f;

    private int _currentScore;
    private int _comboLevel;

    public int CurrentScore => _currentScore;
    public int ComboLevel => _comboLevel;
    public float CurrentMultiplier => Mathf.Min(1f + _comboLevel * cascadeMultiplierStep, maxMultiplier);

    // Khoi tao lai diem so ve 0
    public void ResetScore()
    {
        _currentScore = 0;
        _comboLevel = 0;
        GameEventBus.RaiseScoreChanged(_currentScore);
    }

    // Dat lai cap combo ve 0 khi bat dau mot luot vuot moi
    public void ResetCombo()
    {
        _comboLevel = 0;
    }

    // Tang cap combo khi co no lien hoan cascade
    public void IncrementCombo()
    {
        _comboLevel++;
    }

    // Cong them diem khi pha huy ngoc hoac vat can
    public void AddScore(int count, bool isObstacle = false, bool isByBooster = false)
    {
        if (count <= 0) return;

        float multiplier = CurrentMultiplier;
        int perItem = isObstacle ? obstacleScore : (isByBooster ? boosterScore : baseGemScore);
        int addedScore = Mathf.RoundToInt(perItem * count * multiplier);

        _currentScore += addedScore;
        GameEventBus.RaiseScoreChanged(_currentScore);
    }
}
