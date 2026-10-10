using UnityEngine;
using Utils;

// Dieu phoi luong man choi, khoi tao cac manager tu LevelData va xu ly Thang/Thua
public class GameFlowController : Singleton<GameFlowController>
{
    [Header("Tham chieu Ban co & Input")]
    [SerializeField] private Board board;
    [SerializeField] private HandleInput handleInput;

    [Header("Cau hinh Man choi")]
    [SerializeField] private int levelToLoad = 1;
    [SerializeField] private bool autoInitOnStart = true;

    private MovesManager movesManager;
    private GoalTracker goalTracker;
    private ScoreManager scoreManager;

    public int LevelToLoad { get => levelToLoad; set => levelToLoad = value; }

    private void Start()
    {
        movesManager = MovesManager.Instance;
        goalTracker = GoalTracker.Instance;
        scoreManager = ScoreManager.Instance;

        GameEventBus.OnLevelComplete += OnLevelCompleteHandler;
        GameEventBus.OnLevelFailed += OnLevelFailedHandler;

        if (autoInitOnStart)
        {
            InitializeLevel();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameEventBus.OnLevelComplete -= OnLevelCompleteHandler;
        GameEventBus.OnLevelFailed -= OnLevelFailedHandler;
    }

    // Khoi tao cac Manager tu LevelData cua ban co
    public void InitializeLevel(int level = -1)
    {
        if (level > 0)
        {
            levelToLoad = level;
            board.LoadLevelData(levelToLoad);
        }

        LevelData levelData = board.LevelData;
        movesManager.Initialize(levelData.MovesLimit);
        goalTracker.Initialize(levelData.Goals);
        scoreManager.ResetScore();

        handleInput.EnableInput = true;
    }

    // Xu ly khi thang man choi
    private void OnLevelCompleteHandler()
    {
        movesManager.SetLevelEnded(true);
        handleInput.EnableInput = false;
    }

    // Xu ly khi thua man choi
    private void OnLevelFailedHandler()
    {
        movesManager.SetLevelEnded(true);
        handleInput.EnableInput = false;
    }
}
