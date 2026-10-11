using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Xu ly su kien click nut Exit (Tro ve StartScene hoac thoat game)
public class ButtonExit : MonoBehaviour
{
    public enum ExitAction
    {
        Auto,             // Tu dong: MainScene -> StartScene, StartScene -> Thoat game
        LoadScene,        // Chuyen sang scene chi dinh
        QuitApplication   // Thoat ung dung
    }

    [Header("Tham chieu UI")]
    [SerializeField] private Button button;

    [Header("Cau hinh Thoat")]
    [SerializeField] private ExitAction exitAction = ExitAction.Auto;
    [SerializeField] private string menuSceneName = "StartScene";
    [SerializeField] private float punchScaleDuration = 0.2f;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    private void Start()
    {
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
    }

    // Xu ly khi click nut Exit
    private void OnButtonClicked()
    {
        GameEventBus.RaiseUIButtonClicked();
        button.interactable = false;

        transform.DOPunchScale(Vector3.one * 0.1f, punchScaleDuration, 10, 1f)
            .OnComplete(ExecuteExit);
    }

    // Thuc hien thoat theo cau hinh
    private void ExecuteExit()
    {
        GameEventBus.ClearAllEvents();
        DOTween.KillAll();

        switch (exitAction)
        {
            case ExitAction.Auto:
                string currentScene = SceneManager.GetActiveScene().name;
                if (currentScene == menuSceneName)
                {
                    QuitGame();
                }
                else
                {
                    SceneManager.LoadScene(menuSceneName);
                }
                break;

            case ExitAction.LoadScene:
                SceneManager.LoadScene(menuSceneName);
                break;

            case ExitAction.QuitApplication:
                QuitGame();
                break;
        }
    }

    // Thoat ung dung hoac dung Play Mode trong Editor
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
