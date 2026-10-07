using System.Collections.Generic;
using System.IO;
using OdinSerializer;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

// Component gan truc tiep len BoardGrid trong scene de trich xuat LevelData tu Inspector
public class GetLevelFromTileMap : MonoBehaviour
{
    // 4 Tilemap con tuong ung 4 layer truyen truc tiep tu Inspector
    [Header("4 Tilemap con (Layers)")]
    [SerializeField] private Tilemap boardTilemap;
    [FormerlySerializedAs("normalTilemap")]
    [SerializeField] private Tilemap midTilemap;
    [SerializeField] private Tilemap overlayTilemap;
    [SerializeField] private Tilemap underTilemap;

    [Header("Cau hinh Level")]
    [SerializeField] private int levelNumber = 1;

    public Tilemap BoardTilemap { get => boardTilemap; set => boardTilemap = value; }
    public Tilemap MidTilemap { get => midTilemap; set => midTilemap = value; }
    public Tilemap OverlayTilemap { get => overlayTilemap; set => overlayTilemap = value; }
    public Tilemap UnderTilemap { get => underTilemap; set => underTilemap = value; }
    public int LevelNumber { get => levelNumber; set => levelNumber = value; }

    // Lay ItemId truc tiep tu TileItem (dung EnumItemBoard)
    private int GetItemId(TileBase tile)
    {
        if (tile == null) return (int)EnumItemBoard.Blank;

        // Neu la TileItem thi lay truc tiep ItemId tu enum
        if (tile is TileItem tileItem)
        {
            return tileItem.ItemId;
        }

        // Fallback neu van dung tile mac dinh
        return (int)EnumItemBoard.Board;
    }

    // Lay bounds tu boardTilemap, cac layer con lai se theo bounds nay
    public BoundsInt GetCombinedBounds()
    {
        if (boardTilemap == null) return new BoundsInt(0, 0, 0, 8, 8, 1);
        boardTilemap.CompressBounds();
        return boardTilemap.cellBounds;
    }

    // Chuyen du lieu 1 Tilemap thanh mang 2 chieu int[,] theo Bounds chung
    private int[,] ConvertTilemapToGrid(Tilemap tm, BoundsInt bounds)
    {
        int rows = bounds.size.y;
        int cols = bounds.size.x;
        int[,] grid = new int[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            // Hang 0 cua ma tran la hang tren cung cua ban co (yMax - 1)
            int y = bounds.yMax - 1 - r;

            for (int c = 0; c < cols; c++)
            {
                // Cot 0 cua ma tran la cot ben trai ngoai cung (xMin)
                int x = bounds.xMin + c;

                TileBase tile = tm != null ? tm.GetTile(new Vector3Int(x, y, 0)) : null;

                grid[r, c] = GetItemId(tile);
            }
        }

        return grid;
    }

    // Trich xuat thanh LevelData
    public LevelData BuildLevelData(out BoundsInt bounds)
    {
        bounds = GetCombinedBounds();

        return new LevelData(
            new Dictionary<int, int[,]> { { 0, ConvertTilemapToGrid(boardTilemap, bounds) } },
            new Dictionary<int, int[,]> { { 0, ConvertTilemapToGrid(midTilemap, bounds) } },
            new Dictionary<int, int[,]> { { 0, ConvertTilemapToGrid(underTilemap, bounds) } },
            new Dictionary<int, int[,]> { { 0, ConvertTilemapToGrid(overlayTilemap, bounds) } }
        );
    }

    // Trich xuat va luu LevelData truc tiep vao Assets/Data bang Odin
    [ContextMenu("Trích xuất & Lưu Level")]
    public void ExtractAndSaveLevel()
    {
        if (boardTilemap == null)
        {
            Debug.LogError("[GetLevelFromTileMap] Vui lòng gán Board Tilemap trước khi trích xuất!");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán Board Tilemap!", "OK");
#endif
            return;
        }

        LevelData levelData = BuildLevelData(out BoundsInt bounds);

        string dataDir = Path.Combine(Application.dataPath, "Data");
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);

        string filePath = Path.Combine(dataDir, $"level_{levelNumber}.json");
        byte[] bytes = SerializationUtility.SerializeValue(levelData, DataFormat.JSON);
        File.WriteAllBytes(filePath, bytes);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"[GetLevelFromTileMap] Đã lưu Level {levelNumber} thành công vào {filePath} ({bounds.size.y} hàng x {bounds.size.x} cột)");
        UnityEditor.EditorUtility.DisplayDialog("Thành công", $"Đã trích xuất & lưu Level {levelNumber} thành công!\nKích thước: {bounds.size.y} hàng x {bounds.size.x} cột\nFile: Assets/Data/level_{levelNumber}.json", "OK");
#endif
    }

    // Xem truoc thong tin Level trong Console
    [ContextMenu("Xem trước ma trận Level")]
    public void PreviewLevelData()
    {
        LevelData data = BuildLevelData(out BoundsInt bounds);

        int rows = bounds.size.y;
        int cols = bounds.size.x;
        Debug.Log($"=== THÔNG TIN PREVIEW LEVEL {levelNumber} ===");
        Debug.Log($"Kích thước bàn cờ: {rows} hàng x {cols} cột (X: {bounds.xMin}..{bounds.xMax - 1}, Y: {bounds.yMin}..{bounds.yMax - 1})");

        int[,] mid = data.MidLayer[0];
        string rowPreview = "";
        for (int r = 0; r < rows; r++)
        {
            rowPreview += $"Hàng {r:D2}: [";
            for (int c = 0; c < cols; c++)
            {
                rowPreview += $" {mid[r, c],2} ";
            }
            rowPreview += "]\n";
        }
        Debug.Log($"Ma trận MidLayer:\n{rowPreview}");
    }
}
