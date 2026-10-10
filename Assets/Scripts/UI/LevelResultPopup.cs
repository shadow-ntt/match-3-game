using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup thong bao Thang hoac Thua khi ket thuc man choi
public class LevelResultPopup : MonoBehaviour
{
    [Header("Popup Chien Thang")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winScoreText;
    [SerializeField] private Button nextLevelButton;

    [Header("Popup That Bai")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button retryButton;

    // Singleton Fields khoi tao tai Start
    private ScoreManager scoreManager;
    private GameFlowController gameFlowController;

    private void Start()
    {
        scoreManager = ScoreManager.Instance;
        gameFlowController = GameFlowController.Instance;

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        HideAllPanels();
    }

    private void OnEnable()
    {
        GameEventBus.OnLevelComplete += ShowWin;
        GameEventBus.OnLevelFailed += ShowLose;
    }

    private void OnDisable()
    {
        GameEventBus.OnLevelComplete -= ShowWin;
        GameEventBus.OnLevelFailed -= ShowLose;
    }

    // Hien thi popup chien thang
    private void ShowWin()
    {
        HideAllPanels();
        winPanel.SetActive(true);
        winPanel.transform.localScale = Vector3.zero;
        winPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

        if (winScoreText != null && scoreManager != null)
        {
            winScoreText.text = $"Score: {scoreManager.CurrentScore:N0}";
        }
    }

    // Hien thi popup that bai
    private void ShowLose()
    {
        HideAllPanels();
        losePanel.SetActive(true);
        losePanel.transform.localScale = Vector3.zero;
        losePanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
    }

    // An tat ca cac panel
    public void HideAllPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // Xu ly click nut Next Level
    private void OnNextLevelClicked()
    {
        HideAllPanels();
        if (gameFlowController != null)
        {
            gameFlowController.InitializeLevel(gameFlowController.LevelToLoad + 1);
        }
    }

    // Xu ly click nut Cho lai (Retry)
    private void OnRetryClicked()
    {
        HideAllPanels();
        if (gameFlowController != null)
        {
            gameFlowController.InitializeLevel(gameFlowController.LevelToLoad);
        }
    }
}
