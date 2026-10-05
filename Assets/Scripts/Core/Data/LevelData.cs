using System;
using System.Collections.Generic;
using OdinSerializer;
using UnityEngine;

[Serializable]
public class LevelData
{
    [OdinSerialize] private Dictionary<int, int[,]> boardLevel;
    [OdinSerialize] private Dictionary<int, int[,]> normalLayerItem;
    [OdinSerialize] private Dictionary<int, int[,]> underLayerItem;
    [OdinSerialize] private Dictionary<int, int[,]> overLayerItem;

    public LevelData()
    {
        boardLevel = new Dictionary<int, int[,]>();
        normalLayerItem = new Dictionary<int, int[,]>();
        underLayerItem = new Dictionary<int, int[,]>();
        overLayerItem = new Dictionary<int, int[,]>();
    }

    public LevelData(Dictionary<int, int[,]> boardLevel, Dictionary<int, int[,]> normalLayerItem, Dictionary<int, int[,]> underLayerItem, Dictionary<int, int[,]> overLayerItem)
    {
        this.boardLevel = boardLevel ?? new Dictionary<int, int[,]>();
        this.normalLayerItem = normalLayerItem ?? new Dictionary<int, int[,]>();
        this.underLayerItem = underLayerItem ?? new Dictionary<int, int[,]>();
        this.overLayerItem = overLayerItem ?? new Dictionary<int, int[,]>();
    }

    public Dictionary<int, int[,]> BoardLevel => boardLevel;
    public Dictionary<int, int[,]> NormalLayerItem => normalLayerItem;
    public Dictionary<int, int[,]> UnderLayerItem => underLayerItem;
    public Dictionary<int, int[,]> OverLayerItem => overLayerItem;
}