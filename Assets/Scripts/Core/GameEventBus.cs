using System;

// Kenh su kien tinh tap trung cho toan bo gameplay va UI (Event Bus)
public static class GameEventBus
{
    // Su kien thay doi diem so: (currentScore)
    public static event Action<int> OnScoreChanged;

    // Su kien thay doi luot di: (movesLeft, totalMoves)
    public static event Action<int, int> OnMovesChanged;

    // Su kien cap nhat tien do muc tieu: (itemId, currentAmount, requiredAmount)
    public static event Action<int, int, int> OnGoalUpdated;

    // Su kien mot muc tieu cu the da hoan thanh: (itemId)
    public static event Action<int> OnGoalCompleted;

    // Su kien hoan thanh toan bo muc tieu man choi (Chien thang)
    public static event Action OnLevelComplete;

    // Su kien het luot di ma chua dat muc tieu (That bai)
    public static event Action OnLevelFailed;

    // Su kien reset toan bo muc tieu (khi bat dau hoac choi lai man)
    public static event Action OnGoalsReset;

    public static void RaiseScoreChanged(int score) => OnScoreChanged?.Invoke(score);

    public static void RaiseMovesChanged(int movesLeft, int totalMoves) => OnMovesChanged?.Invoke(movesLeft, totalMoves);

    public static void RaiseGoalUpdated(int itemId, int current, int required) => OnGoalUpdated?.Invoke(itemId, current, required);

    public static void RaiseGoalCompleted(int itemId) => OnGoalCompleted?.Invoke(itemId);

    public static void RaiseGoalsReset() => OnGoalsReset?.Invoke();

    public static void RaiseLevelComplete() => OnLevelComplete?.Invoke();

    public static void RaiseLevelFailed() => OnLevelFailed?.Invoke();

    // Xoa toan bo subscriber de tranh ro ri bo nho khi load/restart scene
    public static void ClearAllEvents()
    {
        OnScoreChanged = null;
        OnMovesChanged = null;
        OnGoalUpdated = null;
        OnGoalCompleted = null;
        OnGoalsReset = null;
        OnLevelComplete = null;
        OnLevelFailed = null;
    }
}
