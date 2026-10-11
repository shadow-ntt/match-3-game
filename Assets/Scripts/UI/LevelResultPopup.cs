using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private TextMeshProUGUI loseScoreText;

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

    private void OnDestroy()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
        }
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
        if (winPanel != null) winPanel.transform.DOKill();
        if (losePanel != null) losePanel.transform.DOKill();
    }

    // Hien thi popup chien thang
    private void ShowWin()
    {
        HideAllPanels();
        winPanel.SetActive(true);
        winPanel.transform.DOKill();
        winPanel.transform.localScale = Vector3.zero;
        winPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

        if (winScoreText != null && scoreManager != null)
        {
            winScoreText.text = $"Score: {scoreManager.CurrentScore:N0}";
        }

        // Khi nguoi choi vua hoan thanh man choi: tang currentLevel len 1 (neu max roi thi khong cong nua)
        PlayerData playerData = SaveManager.LoadPlayerData();
        int maxLevel = SaveManager.GetMaxLevel();
        int playingLevel = (gameFlowController != null) ? gameFlowController.LevelToLoad : playerData.CurrentLevel;

        int nextLevel = playingLevel;
        if (playingLevel < maxLevel)
        {
            nextLevel = playingLevel + 1;
        }

        playerData.CurrentLevel = nextLevel;
        if (nextLevel > playerData.HighestUnlockedLevel)
        {
            playerData.HighestUnlockedLevel = nextLevel;
        }
        SaveManager.SavePlayerData(playerData);
    }

    // Hien thi popup that bai
    private void ShowLose()
    {
        HideAllPanels();
        losePanel.SetActive(true);
        losePanel.transform.DOKill();
        losePanel.transform.localScale = Vector3.zero;
        losePanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        if (loseScoreText != null && scoreManager != null)
        {
            loseScoreText.text = $"Score: {scoreManager.CurrentScore:N0}";
        }
    }

    // An tat ca cac panel
    public void HideAllPanels()
    {
        // Huy cac tween phong to va reset scale ve mac dinh truoc khi an panel
        if (winPanel != null)
        {
            winPanel.transform.DOKill();
            winPanel.transform.localScale = Vector3.one;
            winPanel.SetActive(false);
        }
        if (losePanel != null)
        {
            losePanel.transform.DOKill();
            losePanel.transform.localScale = Vector3.one;
            losePanel.SetActive(false);
        }
    }

    // Xu ly click nut Next Level -> Reload lai scene
    private void OnNextLevelClicked()
    {
        GameEventBus.RaiseUIButtonClicked();
        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = false;
            nextLevelButton.transform.DOKill();
            nextLevelButton.transform.localScale = Vector3.one;
            nextLevelButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f)
                .OnComplete(ReloadScene);
        }
        else
        {
            ReloadScene();
        }
    }

    // Xu ly click nut Choi lai (Retry) -> Reload lai scene
    private void OnRetryClicked()
    {
        GameEventBus.RaiseUIButtonClicked();
        if (retryButton != null)
        {
            retryButton.interactable = false;
            retryButton.transform.DOKill();
            retryButton.transform.localScale = Vector3.one;
            retryButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f)
                .OnComplete(ReloadScene);
        }
        else
        {
            ReloadScene();
        }
    }

    // Reload lai active scene
    private void ReloadScene()
    {
        DOTween.KillAll();
        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(currentScene))
        {
            SceneManager.LoadScene(currentScene);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
