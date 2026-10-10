using System;
using System.Collections.Generic;
using UnityEngine;

// Cau hinh anh xa ItemId sang Icon Sprite hien thi tren UI
[Serializable]
public struct GoalIconConfig
{
    public int itemId;
    public Sprite icon;
}

// Quan ly danh sach cac muc tieu can hoan thanh tren UI (Goal Panel)
public class GoalView : MonoBehaviour
{
    [Header("UI Tham Chieu")]
    [SerializeField] private Transform container;
    [SerializeField] private GoalItemView itemPrefab;

    [Header("Cau Hinh Icon")]
    [SerializeField] private List<GoalIconConfig> iconConfigs = new List<GoalIconConfig>();

    private Dictionary<int, GoalItemView> goalItems = new Dictionary<int, GoalItemView>();

    private void OnEnable()
    {
        GameEventBus.OnGoalUpdated += OnGoalUpdatedHandler;
        GameEventBus.OnGoalsReset += ClearGoals;
    }

    private void OnDisable()
    {
        GameEventBus.OnGoalUpdated -= OnGoalUpdatedHandler;
        GameEventBus.OnGoalsReset -= ClearGoals;
    }

    // Xu ly cap nhat tien do muc tieu tu GameEventBus
    private void OnGoalUpdatedHandler(int itemId, int current, int required)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("GoalView: itemPrefab chua duoc gan trong Inspector!");
            return;
        }

        if (!goalItems.TryGetValue(itemId, out var itemView))
        {
            // Neu chua duoc tao
            Transform parent = container != null ? container : transform;
            itemView = Instantiate(itemPrefab, parent);
            Sprite icon = GetIcon(itemId);
            itemView.Initialize(itemId, icon, required);
            goalItems[itemId] = itemView;
        }

        if (itemView != null)
        {
            itemView.UpdateAmount(current);
        }
    }

    // Lay Sprite tuong ung theo ItemId
    private Sprite GetIcon(int itemId)
    {
        for (int i = 0; i < iconConfigs.Count; i++)
        {
            if (iconConfigs[i].itemId == itemId)
            {
                return iconConfigs[i].icon;
            }
        }
        return null;
    }

    // Don dep toan bo danh sach muc tieu
    public void ClearGoals()
    {
        foreach (var item in goalItems.Values)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        goalItems.Clear();
    }
}
