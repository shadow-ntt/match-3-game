using System;
using System.IO;
using OdinSerializer;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

// Quan ly luu va doc file JSON (LevelData & PlayerData) bang Odin Serializer
public static class SaveManager
{
    // Duong dan thu muc luu file: Assets/Data
    public static string DataFolderPath => Path.Combine(Application.dataPath, "Data");

    // Duong dan file luu tien do nguoi choi: Assets/Data/player_data.json
    public static string PlayerDataFilePath => Path.Combine(DataFolderPath, "player_data.json");

    // Dam bao thu muc Assets/Data ton tai
    public static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(DataFolderPath))
        {
            Directory.CreateDirectory(DataFolderPath);
        }
    }

    public static string GetLevelFilePath(int levelNumber)
    {
        return Path.Combine(DataFolderPath, $"level_{levelNumber}.json");
    }

    public static string GetLevelFilePath(string fileName)
    {
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".json";
        }
        return Path.Combine(DataFolderPath, fileName);
    }

    // ==========================================
    // PLAYER DATA (LƯU & ĐỌC TIẾN TRÌNH NGƯỜI CHƠI)
    // ==========================================

    // Luu PlayerData thanh file JSON bang Odin Serializer
    public static bool SavePlayerData(PlayerData data)
    {
        if (data == null) return false;

        try
        {
            EnsureDirectoryExists();
            byte[] bytes = OdinSerializer.SerializationUtility.SerializeValue(data, DataFormat.JSON);
            File.WriteAllBytes(PlayerDataFilePath, bytes);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi luu PlayerData: {ex.Message}");
            return false;
        }
    }

    // Load PlayerData tu JSON, neu chua ton tai thi tao moi va luu lai
    public static PlayerData LoadPlayerData()
    {
        if (!File.Exists(PlayerDataFilePath))
        {
            PlayerData newPlayerData = new PlayerData();
            SavePlayerData(newPlayerData);
            return newPlayerData;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(PlayerDataFilePath);
            PlayerData data = OdinSerializer.SerializationUtility.DeserializeValue<PlayerData>(bytes, DataFormat.JSON);
            return data ?? new PlayerData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi load PlayerData: {ex.Message}");
            return new PlayerData();
        }
    }

    // ==========================================
    // SAVE & LOAD LEVEL DATA
    // ==========================================

    // Luu LevelData thanh file JSON tai Assets/Data/level_{levelNumber}.json bang Odin Serializer
    public static bool SaveLevel(int levelNumber, LevelData levelData)
    {
        return SaveLevel($"level_{levelNumber}", levelData);
    }

    // Luu LevelData thanh file JSON tai Assets/Data/{fileName}.json bang Odin Serializer
    public static bool SaveLevel(string fileName, LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError($"[SaveManager] Khong the luu LevelData null ({fileName})!");
            return false;
        }

        try
        {
            EnsureDirectoryExists();
            string filePath = GetLevelFilePath(fileName);
            byte[] bytes = OdinSerializer.SerializationUtility.SerializeValue(levelData, DataFormat.JSON);
            File.WriteAllBytes(filePath, bytes);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi luu file '{fileName}': {ex.Message}");
            return false;
        }
    }

    // Load LevelData tu file JSON tai Assets/Data/level_{levelNumber}.json bang Odin Serializer
    public static LevelData LoadLevel(int levelNumber)
    {
        return LoadLevel($"level_{levelNumber}");
    }

    // Load LevelData tu file JSON tai Assets/Data/{fileName}.json bang Odin Serializer
    public static LevelData LoadLevel(string fileName)
    {
        string filePath = GetLevelFilePath(fileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            return OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(bytes, DataFormat.JSON);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi load file '{fileName}': {ex.Message}");
            return null;
        }
    }

    // Kiem tra file level co ton tai trong Assets/Data khong
    public static bool HasLevel(int levelNumber)
    {
        return File.Exists(GetLevelFilePath(levelNumber));
    }

    // Lay so thu tu level lon nhat hien co trong Assets/Data
    public static int GetMaxLevel()
    {
        EnsureDirectoryExists();
        int maxLevel = 1;
        try
        {
            string[] files = Directory.GetFiles(DataFolderPath, "level_*.json");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("level_", StringComparison.OrdinalIgnoreCase))
                {
                    string numPart = fileName.Substring(6);
                    if (int.TryParse(numPart, out int lvl))
                    {
                        if (lvl > maxLevel)
                        {
                            maxLevel = lvl;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi lay MaxLevel: {ex.Message}");
        }

        return maxLevel;
    }

    // Xoa file level trong Assets/Data
    public static bool DeleteLevel(int levelNumber)
    {
        string filePath = GetLevelFilePath(levelNumber);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    // ==========================================
    // MENU TOOLS TEST TREN UNITY EDITOR
    // ==========================================

    [MenuItem("Tools/Save/Reset Player Data (Level 1)")]
    public static void ResetPlayerDataToLevel1()
    {
        SavePlayerData(new PlayerData(1));
        Debug.Log("[SaveManager] Da reset PlayerData ve Level 1!");
    }

    [MenuItem("Tools/Save/Test Save Demo Level 1")]
    public static void TestSaveDemoLevel1()
    {
        int rows = 8;
        int cols = 8;
        var board = new System.Collections.Generic.Dictionary<int, int[,]>();
        var mid = new System.Collections.Generic.Dictionary<int, int[,]>();
        var under = new System.Collections.Generic.Dictionary<int, int[,]>();
        var over = new System.Collections.Generic.Dictionary<int, int[,]>();

        int[,] boardGrid = new int[rows, cols];
        int[,] midGrid = new int[rows, cols];
        int[,] underGrid = new int[rows, cols];
        int[,] overGrid = new int[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                boardGrid[r, c] = (int)EnumItemBoard.Board;
                midGrid[r, c] = UnityEngine.Random.Range((int)EnumItemBoard.Red, (int)EnumItemBoard.Pink + 1);
                underGrid[r, c] = (r == 0) ? (int)EnumItemBoard.Spawn : (int)EnumItemBoard.Blank;
                overGrid[r, c] = (int)EnumItemBoard.Blank;
            }
        }

        board[0] = boardGrid;
        mid[0] = midGrid;
        under[0] = underGrid;
        over[0] = overGrid;

        var goals = new System.Collections.Generic.List<GoalEntry>
        {
            new GoalEntry((int)EnumItemBoard.Red, 15),
            new GoalEntry((int)EnumItemBoard.Yellow, 10)
        };

        LevelData demoData = new LevelData(board, mid, under, over, 30, 1000, goals);
        SaveLevel(1, demoData);
    }

    [MenuItem("Tools/Save/Test Load Demo Level 1")]
    public static void TestLoadDemoLevel1()
    {
        LevelData data = LoadLevel(1);
        if (data != null)
        {
            Debug.Log($"[SaveManager] Test load thanh cong Level 1! Goals: {data.Goals.Count}, Moves: {data.MovesLimit}");
        }
    }
#endif
}
