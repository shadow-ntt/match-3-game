using System;
using System.IO;
using OdinSerializer;
using UnityEngine;
using Utils;

public class SaveManager : SingletonDontDestroy<SaveManager>
{
    // Đường dẫn lưu file: Assets/Data
    public string DataFolderPath => Path.Combine(Application.dataPath, "Data");

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(DataFolderPath))
        {
            Directory.CreateDirectory(DataFolderPath);
        }
    }

    public string GetLevelFilePath(int levelNumber)
    {
        return Path.Combine(DataFolderPath, $"level_{levelNumber}.json");
    }

    public string GetLevelFilePath(string fileName)
    {
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".json";
        }
        return Path.Combine(DataFolderPath, fileName);
    }

    // ==========================================
    // SAVE (LƯU LEVEL DATA THÀNH JSON BẰNG ODIN)
    // ==========================================

    // Lưu LevelData thành file JSON tại Assets/Data/level_{levelNumber}.json bằng Odin Serializer
    public bool SaveLevel(int levelNumber, LevelData levelData)
    {
        return SaveLevel($"level_{levelNumber}", levelData);
    }

    // Lưu LevelData thành file JSON tại Assets/Data/{fileName}.json bằng Odin Serializer
    public bool SaveLevel(string fileName, LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError($"[SaveManager] Không thể lưu LevelData null ({fileName})!");
            return false;
        }

        try
        {
            EnsureDirectoryExists();
            string filePath = GetLevelFilePath(fileName);

            // Serialize ra byte array định dạng JSON bằng Odin
            byte[] bytes = SerializationUtility.SerializeValue(levelData, DataFormat.JSON);
            File.WriteAllBytes(filePath, bytes);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            Debug.Log($"[SaveManager] Đã lưu thành công tại: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Lỗi khi lưu file '{fileName}': {ex.Message}");
            return false;
        }
    }

    // ==========================================
    // LOAD (TẢI LEVEL DATA TỪ FILE JSON BẰNG ODIN)
    // ==========================================

    // Load LevelData từ file JSON tại Assets/Data/level_{levelNumber}.json bằng Odin Serializer
    public LevelData LoadLevel(int levelNumber)
    {
        return LoadLevel($"level_{levelNumber}");
    }

    // Load LevelData từ file JSON tại Assets/Data/{fileName}.json bằng Odin Serializer
    public LevelData LoadLevel(string fileName)
    {
        string filePath = GetLevelFilePath(fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveManager] Không tìm thấy file tại: {filePath}");
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            LevelData data = SerializationUtility.DeserializeValue<LevelData>(bytes, DataFormat.JSON);
            Debug.Log($"[SaveManager] Đã load thành công: {filePath}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Lỗi khi load file '{fileName}': {ex.Message}");
            return null;
        }
    }

    // Kiểm tra file level có tồn tại trong Assets/Data không
    public bool HasLevel(int levelNumber)
    {
        return File.Exists(GetLevelFilePath(levelNumber));
    }

    // Xóa file level trong Assets/Data
    public bool DeleteLevel(int levelNumber)
    {
        string filePath = GetLevelFilePath(levelNumber);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"[SaveManager] Đã xóa file: {filePath}");
            return true;
        }
        return false;
    }

    // ==========================================
    // CONTEXT MENU TEST TRÊN INSPECTOR
    // ==========================================

    [ContextMenu("Test Save Demo Level 1")]
    private void TestSaveDemoLevel1()
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

    [ContextMenu("Test Load Demo Level 1")]
    private void TestLoadDemoLevel1()
    {
        LevelData data = LoadLevel(1);
        if (data != null)
        {
            Debug.Log($"[SaveManager] Test load thành công Level 1!");
            Debug.Log($"Board Layers: {data.BoardLevel?.Count}, Mid Layers: {data.MidLayer?.Count}, Moves: {data.MovesLimit}, TargetScore: {data.TargetScore}, Goals: {data.Goals.Count}");
        }
    }
}
