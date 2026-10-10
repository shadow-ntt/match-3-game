using DG.Tweening;
using TMPro;
using UnityEngine;

// Hien thi diem so tren UI kem hoat anh tang diem DOCounter va pulse scale
public class ScoreView : MonoBehaviour
{
    [Header("UI Tham Chieu")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Cau Hinh Hoat Anh")]
    [SerializeField] private float countDuration = 0.3f;
    [SerializeField] private float punchScaleAmount = 0.15f;
    [SerializeField] private float punchDuration = 0.2f;

    private int displayedScore;
    private Tween countTween;
    private Tween punchTween;

    private void OnEnable()
    {
        GameEventBus.OnScoreChanged += OnScoreChangedHandler;
    }

    private void OnDisable()
    {
        GameEventBus.OnScoreChanged -= OnScoreChangedHandler;
        countTween?.Kill();
        punchTween?.Kill();
    }

    // Xu ly khi diem so thay doi tu GameEventBus
    private void OnScoreChangedHandler(int newScore)
    {
        if (newScore == 0)
        {
            displayedScore = 0;
            scoreText.text = "0";
            return;
        }

        countTween?.Kill();
        countTween = DOVirtual.Int(displayedScore, newScore, countDuration, val =>
        {
            displayedScore = val;
            scoreText.text = val.ToString("N0");
        });

        punchTween?.Kill();
        scoreText.transform.localScale = Vector3.one;
        punchTween = scoreText.transform.DOPunchScale(Vector3.one * punchScaleAmount, punchDuration, 1, 0.5f);
    }
}
