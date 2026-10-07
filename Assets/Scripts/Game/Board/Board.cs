using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Utils;

public class Board : MonoBehaviour
{
    [Header("Cấu hình Level")]
    [Tooltip("Level cần tải và vẽ")]
    [SerializeField] private int levelNumber = 1;
    [Tooltip("Tự động tải và vẽ bàn cờ khi Start")]
    [SerializeField] private bool autoLoadOnStart = true;

    [Header("Cấu trúc Grid & 4 Layer Tilemap (giống GetLevelFromTileMap)")]
    [Tooltip("Grid quản lý tọa độ bàn cờ")]
    [SerializeField] private Grid grid;
    [Tooltip("Tầng 1: Nền bàn cờ (Chứa -1: Blank, 0: Board, 1: Spawn)")]
    [SerializeField] private Tilemap boardTilemap;
    [Tooltip("Tầng 2: Tầng giữa chứa Ngọc & Booster (Mid Layer)")]
    [SerializeField] private Tilemap midTilemap;
    [Tooltip("Tầng 3: Dưới ngọc (Sẽ bổ sung sau)")]
    [SerializeField] private Tilemap underTilemap;
    [Tooltip("Tầng 4: Phủ trên ngọc (Sẽ bổ sung sau)")]
    [SerializeField] private Tilemap overlayTilemap;


    [Header("Dữ liệu Level hiện tại")]
    // Biến lưu trữ dữ liệu level lấy từ JSON qua SaveManager
    [SerializeField] private LevelData _levelData;

    // Quản lý các đối tượng đã spawn để tái sử dụng hoặc thu hồi về pool
    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
    private GameObject[,] _midGrid;
    private GameObject[,] _boardCellGrid;
    private GameObject[,] _underGrid;
    private GameObject[,] _overlayGrid;

    public LevelData LevelData => _levelData;
    public int LevelNumber { get => levelNumber; set => levelNumber = value; }
    public Grid Grid { get => grid; set => grid = value; }
    public Tilemap BoardTilemap { get => boardTilemap; set => boardTilemap = value; }
    public Tilemap MidTilemap { get => midTilemap; set => midTilemap = value; }
    public Tilemap UnderTilemap { get => underTilemap; set => underTilemap = value; }
    public Tilemap OverlayTilemap { get => overlayTilemap; set => overlayTilemap = value; }
    public GameObject[,] MidGrid => _midGrid;
    public GameObject[,] BoardCellGrid => _boardCellGrid;
    public GameObject[,] UnderGrid => _underGrid;
    public GameObject[,] OverlayGrid => _overlayGrid;

    // Kich thuoc ban co theo truc X (ngang/cot) va truc Y (doc/hang)
    public int Width => _midGrid != null ? _midGrid.GetLength(0) : 0;
    public int Height => _midGrid != null ? _midGrid.GetLength(1) : 0;
    public int Cols => Width;
    public int Rows => Height;

    // Kiem tra toa do (x, y) co nam trong pham vi ban co
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return IsInBounds(pos.x, pos.y);
    }

    private void Awake()
    {
        LoadLevelData(levelNumber);
    }
    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadAndDrawBoard();
        }
    }

    // 1. LẤY DỮ LIỆU JSON TỪ SAVEMANAGER

    private SaveManager GetSaveManager()
    {
        if (SaveManager.Instance != null) return SaveManager.Instance;
        return FindAnyObjectByType<SaveManager>();
    }

    private Pooltem GetPool()
    {
        if (Pooltem.Instance != null) return Pooltem.Instance;
        return FindAnyObjectByType<Pooltem>();
    }

    // Tải LevelData từ file JSON thông qua SaveManager
    public bool LoadLevelData(int level)
    {
        levelNumber = level;
        SaveManager saveManager = GetSaveManager();

        if (saveManager == null)
        {
            Debug.LogError("[Board] Không tìm thấy SaveManager trong scene để đọc JSON!");
            return false;
        }

        _levelData = saveManager.LoadLevel(levelNumber);

        if (_levelData == null)
        {
            Debug.LogWarning($"[Board] Không thể tải dữ liệu JSON cho Level {levelNumber} từ: {saveManager.GetLevelFilePath(levelNumber)}");
            return false;
        }

        Debug.Log($"[Board] Đã tải thành công dữ liệu JSON cho Level {levelNumber}.");
        return true;
    }

    // 2. VẼ BÀN CỜ DỰA VÀO POOL TRÊN CÁC TẦNG TILEMAP

    [ContextMenu("Tải & Vẽ Bàn Cờ (Load & Draw Board)")]
    public void LoadAndDrawBoard()
    {
        DrawBoard();
    }

    // Vẽ bàn cờ dựa vào _levelData hiện tại và Pooltem
    [ContextMenu("Vẽ Bàn Cờ (Draw Board)")]
    public void DrawBoard()
    {
        if (_levelData == null)
        {
            Debug.LogWarning("[Board] _levelData đang rỗng! Vui lòng gọi LoadLevelData trước.");
            return;
        }

        Pooltem pool = GetPool();
        if (pool == null)
        {
            Debug.LogError("[Board] Không tìm thấy Pooltem trong scene để spawn item!");
            return;
        }

        // Xóa các item đã vẽ trước đó để tránh trùng lặp
        ClearBoard();

        // Xac dinh kich thuoc ban co (chieu rong theo truc X, chieu cao theo truc Y)
        int width = 0;
        int height = 0;

        if (_levelData.BoardLevel != null && _levelData.BoardLevel.TryGetValue(0, out var boardMatrix) && boardMatrix != null)
        {
            height = boardMatrix.GetLength(0);
            width = boardMatrix.GetLength(1);
        }
        else if (_levelData.MidLayer != null && _levelData.MidLayer.TryGetValue(0, out var midMatrix) && midMatrix != null)
        {
            height = midMatrix.GetLength(0);
            width = midMatrix.GetLength(1);
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        _midGrid = new GameObject[width, height];
        _boardCellGrid = new GameObject[width, height];
        _underGrid = new GameObject[width, height];
        _overlayGrid = new GameObject[width, height];

        // Tu dong can giua Grid quanh goc toa do (0, 0) de ban co nam ngay giua man hinh
        if (grid != null)
        {
            float originX = -width * grid.cellSize.x * 0.5f;
            float originY = -height * grid.cellSize.y * 0.5f;
            grid.transform.position = new Vector3(originX, originY, 0);
        }

        // 1. TANG BOARD: Chua -1 (Blank), 0 (Board), 1 (Spawn)
        SpawnLayer(_levelData.BoardLevel, boardTilemap, "BoardCell", _boardCellGrid, width, height, pool, BoardItemUtils.IsValidBoardCell);

        // 2. TANG 2: Layer Under (Lop duoi: Diem sinh ngoc Spawner id = 1 / Spawn, nen dac biet...)
        SpawnLayer(_levelData.UnderLayerItem, underTilemap, "UnderItem", _underGrid, width, height, pool, BoardItemUtils.IsValidUnderItem);

        // 3. TANG 3: Layer Mid (Layer giua: Cac vien ngoc Match-3: 102..108 va Booster: 301..305)
        SpawnLayer(_levelData.MidLayer, midTilemap, "MidItem", _midGrid, width, height, pool, BoardItemUtils.IsMidLayer);

        // 4. TANG 4: Layer Overlay (Lop phu tren ngoc: bang tuyet, day xich, long sat, mang nhen...)
        SpawnLayer(_levelData.OverLayerItem, overlayTilemap, "OverlayItem", _overlayGrid, width, height, pool, BoardItemUtils.IsValidOverlayItem);

        // Dong bo State cua BoardCell theo MidGrid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_boardCellGrid[x, y] != null && _boardCellGrid[x, y].TryGetComponent<BoardCell>(out var cellComp))
                {
                    cellComp.State = (_midGrid[x, y] != null || _underGrid[x, y] != null) ? EnumStateBoardCell.Occupied : EnumStateBoardCell.Empty;
                }
            }
        }
    }

    // ==========================================
    // HELPER SPAWN TANG GRID
    // ==========================================

    private void SpawnLayer(
        Dictionary<int, int[,]> layerData,
        Tilemap tilemap,
        string prefix,
        GameObject[,] targetGrid,
        int width,
        int height,
        Pooltem pool,
        Func<int, bool> isValid)
    {
        if (layerData == null || pool == null) return;

        Transform parent = tilemap != null ? tilemap.transform : transform;

        foreach (var kvp in layerData)
        {
            int[,] gridData = kvp.Value;
            if (gridData == null) continue;

            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    int id = gridData[r, c];
                    if (isValid != null && !isValid(id)) continue;

                    // Chuyen doi sang he toa do Grid (x, y): x tu trai sang phai (c), y tu duoi len tren (height - 1 - r)
                    int x = c;
                    int y = height - 1 - r;

                    Vector3 worldPos = GridUtils.GridToWorld(grid, x, y);
                    GameObject obj = pool.SpawnBoardItem(id, worldPos, Quaternion.identity, parent);
                    if (obj != null)
                    {
                        obj.name = $"{prefix}_{id}_{x}_{y}";
                        _spawnedObjects.Add(obj);
                        if (targetGrid != null)
                        {
                            targetGrid[x, y] = obj;
                        }

                        if (obj.TryGetComponent<BoardCell>(out var cellComp))
                        {
                            cellComp.X = x;
                            cellComp.Y = y;
                            cellComp.State = EnumStateBoardCell.Occupied;
                        }
                    }
                }
            }
        }
    }


    // 3. THU HỒI VỀ POOL
    // Xóa toàn bộ các đối tượng đã vẽ trên bàn cờ và trả về Pooltem
    [ContextMenu("Xóa Bàn Cờ (Clear Board)")]
    public void ClearBoard()
    {
        Pooltem pool = GetPool();

        for (int i = 0; i < _spawnedObjects.Count; i++)
        {
            GameObject obj = _spawnedObjects[i];
            if (obj == null) continue;

            if (pool != null)
            {
                pool.ReturnBoardItem(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        _spawnedObjects.Clear();
        _midGrid = null;
        _boardCellGrid = null;
        _underGrid = null;
        _overlayGrid = null;
    }


    private void OnDestroy()
    {
        ClearBoard();
    }
}



