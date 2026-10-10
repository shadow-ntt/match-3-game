using System.Collections.Generic;
using UnityEngine;
using Utils;

// Tien do cua tung muc tieu
public class GoalProgress
{
    public int itemId;
    public int current;
    public int required;

    public bool IsCompleted => current >= required;
}

// Theo doi va cap nhat tien do cac muc tieu cua man choi
public class GoalTracker : Singleton<GoalTracker>
{
    private Dictionary<int, GoalProgress> _goalsProgress = new Dictionary<int, GoalProgress>();
    private bool _isAllGoalsCompleted;

    public bool IsAllCompleted => _isAllGoalsCompleted;
    public Dictionary<int, GoalProgress> GoalsProgress => _goalsProgress;

    // Khoi tao danh sach muc tieu tu LevelData
    public void Initialize(List<GoalEntry> goals)
    {
        _goalsProgress.Clear();
        _isAllGoalsCompleted = false;

        GameEventBus.RaiseGoalsReset();

        if (goals == null || goals.Count == 0)
        {
            _isAllGoalsCompleted = true;
            return;
        }

        for (int i = 0; i < goals.Count; i++)
        {
            var goal = goals[i];
            var progress = new GoalProgress
            {
                itemId = goal.ItemId,
                current = 0,
                required = goal.Amount
            };

            _goalsProgress[goal.ItemId] = progress;
            GameEventBus.RaiseGoalUpdated(goal.ItemId, 0, goal.Amount);
        }
    }

    // Ghi nhan item bi pha huy va cap nhat tien do
    public void RegisterDestroyed(int itemId, int count = 1)
    {
        if (_isAllGoalsCompleted || count <= 0) return;

        if (_goalsProgress.TryGetValue(itemId, out var progress))
        {
            if (progress.IsCompleted) return;

            progress.current = Mathf.Min(progress.current + count, progress.required);
            GameEventBus.RaiseGoalUpdated(itemId, progress.current, progress.required);

            if (progress.IsCompleted)
            {
                GameEventBus.RaiseGoalCompleted(itemId);
                CheckAllGoalsCompleted();
            }
        }
    }

    // Kiem tra da hoan thanh toan bo muc tieu hay chua
    public bool CheckAllGoalsCompleted()
    {
        if (_isAllGoalsCompleted) return true;

        foreach (var progress in _goalsProgress.Values)
        {
            if (!progress.IsCompleted) return false;
        }

        _isAllGoalsCompleted = true;
        GameEventBus.RaiseLevelComplete();
        return true;
    }

    // Kiem tra mot item co phai la muc tieu hay khong
    public bool HasGoal(int itemId)
    {
        return _goalsProgress.ContainsKey(itemId);
    }
}
