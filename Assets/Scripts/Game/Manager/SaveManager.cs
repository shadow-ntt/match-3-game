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
    // Duong dan thu muc luu file Level trong Editor: Assets/Resources/Levels
    public static string EditorLevelFolderPath => Path.Combine(Application.dataPath, "Resources", "Levels");

    // Duong dan file luu tien do nguoi choi: Application.persistentDataPath/player_data.json (chuan da nen tang)
    public static string PlayerDataFilePath => Path.Combine(Application.persistentDataPath, "player_data.json");

    // Dam bao cac thu muc can thiet ton tai
    public static void EnsureDirectoryExists()
    {
#if UNITY_EDITOR
        if (!Directory.Exists(EditorLevelFolderPath))
        {
            Directory.CreateDirectory(EditorLevelFolderPath);
        }
#endif
        if (!Directory.Exists(Application.persistentDataPath))
        {
            Directory.CreateDirectory(Application.persistentDataPath);
        }
    }

    public static string GetLevelFilePath(int levelNumber)
    {
        return GetLevelFilePath($"level_{levelNumber}");
    }

    public static string GetLevelFilePath(string fileName)
    {
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }
        return Path.Combine(EditorLevelFolderPath, $"{fileName}.json");
    }

    // ==========================================
    // PLAYER DATA (LƯU & ĐỌC TIẾN TRÌNH NGƯỜI CHƠI)
    // ==========================================

    // Luu PlayerData thanh file JSON bang Odin Serializer vao persistentDataPath
    public static bool SavePlayerData(PlayerData data)
    {
        if (data == null) return false;

        try
        {
            EnsureDirectoryExists();
            byte[] bytes = OdinSerializer.SerializationUtility.SerializeValue(data, DataFormat.JSON);
            File.WriteAllBytes(PlayerDataFilePath, bytes);
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

    // Luu LevelData thanh file JSON tai Assets/Resources/Levels/level_{levelNumber}.json bang Odin Serializer
    public static bool SaveLevel(int levelNumber, LevelData levelData)
    {
        return SaveLevel($"level_{levelNumber}", levelData);
    }

    // Luu LevelData thanh file JSON tai Assets/Resources/Levels/{fileName}.json bang Odin Serializer
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
            Debug.Log($"[SaveManager] Da luu Level vao: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi luu file '{fileName}': {ex.Message}");
            return false;
        }
    }

    // Load LevelData tu file JSON tai Resources/Levels/level_{levelNumber}
    public static LevelData LoadLevel(int levelNumber)
    {
        return LoadLevel($"level_{levelNumber}");
    }

    // Load LevelData tu file JSON
    public static LevelData LoadLevel(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;

        string cleanName = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(fileName)
            : fileName;

        // 1. Uu tien load tu Resources (Chay tot ca tren moi nen tang Build & Editor)
        TextAsset textAsset = Resources.Load<TextAsset>($"Levels/{cleanName}");
        if (textAsset != null && textAsset.bytes != null && textAsset.bytes.Length > 0)
        {
            try
            {
                return OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(textAsset.bytes, DataFormat.JSON);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Loi khi deserialize LevelData tu Resources 'Levels/{cleanName}': {ex.Message}");
            }
        }

        // 2. Fallback kiem tra file truc tiep trong EditorLevelFolderPath
        string filePath = GetLevelFilePath(cleanName);
        if (File.Exists(filePath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                return OdinSerializer.SerializationUtility.DeserializeValue<LevelData>(bytes, DataFormat.JSON);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Loi khi load file '{filePath}': {ex.Message}");
            }
        }

        Debug.LogError($"[SaveManager] Khong tim thay Level '{cleanName}' trong Resources/Levels!");
        return null;
    }

    // Kiem tra file level co ton tai khong
    public static bool HasLevel(int levelNumber)
    {
        // 1. Kiem tra trong Resources
        TextAsset asset = Resources.Load<TextAsset>($"Levels/level_{levelNumber}");
        if (asset != null) return true;

        // 2. Kiem tra trong EditorLevelFolderPath
        return File.Exists(GetLevelFilePath(levelNumber));
    }

    // Lay so thu tu level lon nhat hien co
    public static int GetMaxLevel()
    {
        int maxLevel = 1;
        try
        {
            // 1. Quet trong Resources/Levels (hoat dong hoan hao sau khi Build)
            TextAsset[] levelAssets = Resources.LoadAll<TextAsset>("Levels");
            if (levelAssets != null && levelAssets.Length > 0)
            {
                foreach (TextAsset asset in levelAssets)
                {
                    if (asset != null && asset.name.StartsWith("level_", StringComparison.OrdinalIgnoreCase))
                    {
                        string numPart = asset.name.Substring(6);
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

            // 2. Quet trong EditorLevelFolderPath neu chay trong Editor
            if (Directory.Exists(EditorLevelFolderPath))
            {
                string[] files = Directory.GetFiles(EditorLevelFolderPath, "level_*.json");
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
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Loi khi lay MaxLevel: {ex.Message}");
        }

        return maxLevel;
    }

    // Xoa file level trong EditorLevelFolderPath
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
}
