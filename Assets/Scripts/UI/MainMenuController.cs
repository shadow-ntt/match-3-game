using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Dieu khien giao dien Start Scene, hien thi level hien tai va chuyen sang MainScene
public class MainMenuController : MonoBehaviour
{
    [Header("Tham chieu UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI playButtonText;

    [Header("Cau hinh Scene")]
    [SerializeField] private string gameplaySceneName = "MainScene";

    private void Awake()
    {
        // Tuan thu quy tac: Awake lay du lieu (Load JSON)
        PlayerData playerData = SaveManager.LoadPlayerData();
        playButtonText.text = $"Play level {playerData.CurrentLevel}";
    }

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
    }

    // Xu ly click nut Play level
    private void OnPlayButtonClicked()
    {
        GameEventBus.RaiseUIButtonClicked();
        playButton.interactable = false;

        playButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1f)
            .OnComplete(LoadGameplayScene);
    }

    // Don dep event bus va load scene gameplay
    private void LoadGameplayScene()
    {
        GameEventBus.ClearAllEvents();
        DOTween.KillAll();
        SceneManager.LoadScene(gameplaySceneName);
    }
}
