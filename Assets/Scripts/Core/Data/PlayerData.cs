using System;
using OdinSerializer;
using UnityEngine;

// Model luu tru tien do va thong tin nguoi choi bang JSON qua Odin Serializer
[Serializable]
public class PlayerData
{
    [OdinSerialize, SerializeField] private int currentLevel = 1;
    [OdinSerialize, SerializeField] private int highestUnlockedLevel = 1;
    [OdinSerialize, SerializeField] private bool isBgmOn = true;
    [OdinSerialize, SerializeField] private bool isSfxOn = true;

    public PlayerData()
    {
        currentLevel = 1;
        highestUnlockedLevel = 1;
        isBgmOn = true;
        isSfxOn = true;
    }

    public PlayerData(int level)
    {
        currentLevel = Mathf.Max(1, level);
        highestUnlockedLevel = Mathf.Max(1, level);
        isBgmOn = true;
        isSfxOn = true;
    }

    public int CurrentLevel
    {
        get => currentLevel;
        set => currentLevel = Mathf.Max(1, value);
    }

    public int HighestUnlockedLevel
    {
        get => highestUnlockedLevel;
        set => highestUnlockedLevel = Mathf.Max(1, value);
    }

    public bool IsBgmOn
    {
        get => isBgmOn;
        set => isBgmOn = value;
    }

    public bool IsSfxOn
    {
        get => isSfxOn;
        set => isSfxOn = value;
    }
}
