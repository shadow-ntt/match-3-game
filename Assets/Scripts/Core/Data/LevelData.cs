using System;
using System.Collections.Generic;
using OdinSerializer;
using UnityEngine;

[Serializable]
public class LevelData
{
    [OdinSerialize] private Dictionary<int, int[,]> boardLevel;
    [OdinSerialize] private Dictionary<int, int[,]> midLayer;
    [OdinSerialize] private Dictionary<int, int[,]> underLayerItem;
    [OdinSerialize] private Dictionary<int, int[,]> overLayerItem;

    // So luot di chuyen toi da cua man choi
    [OdinSerialize] private int movesLimit = 25;
    // Diem so muc tieu can dat (0 neu khong bat buoc)
    [OdinSerialize] private int targetScore = 0;
    // Danh sach cac muc tieu can hoan thanh
    [OdinSerialize] private List<GoalEntry> goals;

    public LevelData()
    {
        boardLevel = new Dictionary<int, int[,]>();
        midLayer = new Dictionary<int, int[,]>();
        underLayerItem = new Dictionary<int, int[,]>();
        overLayerItem = new Dictionary<int, int[,]>();
        movesLimit = 25;
        targetScore = 0;
        goals = new List<GoalEntry>();
    }

    // Constructor tuong thich nguoc voi cac code cu
    public LevelData(Dictionary<int, int[,]> boardLevel, Dictionary<int, int[,]> midLayer, Dictionary<int, int[,]> underLayerItem, Dictionary<int, int[,]> overLayerItem)
        : this(boardLevel, midLayer, underLayerItem, overLayerItem, 25, 0, new List<GoalEntry>())
    {
    }

    // Constructor day du chua ca luot di, muc tieu va diem so
    public LevelData(Dictionary<int, int[,]> boardLevel, Dictionary<int, int[,]> midLayer, Dictionary<int, int[,]> underLayerItem, Dictionary<int, int[,]> overLayerItem, int movesLimit, int targetScore, List<GoalEntry> goals)
    {
        this.boardLevel = boardLevel ?? new Dictionary<int, int[,]>();
        this.midLayer = midLayer ?? new Dictionary<int, int[,]>();
        this.underLayerItem = underLayerItem ?? new Dictionary<int, int[,]>();
        this.overLayerItem = overLayerItem ?? new Dictionary<int, int[,]>();
        this.movesLimit = movesLimit > 0 ? movesLimit : 25;
        this.targetScore = Mathf.Max(0, targetScore);
        this.goals = goals ?? new List<GoalEntry>();
    }

    public Dictionary<int, int[,]> BoardLevel => boardLevel;
    public Dictionary<int, int[,]> MidLayer => midLayer;
    public Dictionary<int, int[,]> MidLayerItem => midLayer;
    public Dictionary<int, int[,]> UnderLayerItem => underLayerItem;
    public Dictionary<int, int[,]> OverLayerItem => overLayerItem;

    public int MovesLimit => movesLimit > 0 ? movesLimit : 25;
    public int TargetScore => targetScore;
    public List<GoalEntry> Goals => goals ?? (goals = new List<GoalEntry>());
}