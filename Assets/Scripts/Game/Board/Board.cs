using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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
    [Tooltip("Tầng 2: Ngọc thông thường (Chứa 102: Red, 103: Blue... 108: Pink)")]
    [SerializeField] private Tilemap normalTilemap;
    [Tooltip("Tầng 3: Dưới ngọc (Sẽ bổ sung sau)")]
    [SerializeField] private Tilemap underTilemap;
    [Tooltip("Tầng 4: Phủ trên ngọc (Sẽ bổ sung sau)")]
    [SerializeField] private Tilemap overlayTilemap;


    [Header("Dữ liệu Level hiện tại")]
    // Biến lưu trữ dữ liệu level lấy từ JSON qua SaveManager
    [SerializeField] private LevelData _levelData;

    // Quản lý các đối tượng đã spawn để tái sử dụng hoặc thu hồi về pool
    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
    private GameObject[,] _itemGrid;
    private GameObject[,] _boardCellGrid;
    private GameObject[,] _underGrid;
    private GameObject[,] _overlayGrid;

    public LevelData LevelData => _levelData;
    public int LevelNumber { get => levelNumber; set => levelNumber = value; }
    public Grid Grid { get => grid; set => grid = value; }
    public Tilemap BoardTilemap { get => boardTilemap; set => boardTilemap = value; }
    public Tilemap NormalTilemap { get => normalTilemap; set => normalTilemap = value; }
    public Tilemap UnderTilemap { get => underTilemap; set => underTilemap = value; }
    public Tilemap OverlayTilemap { get => overlayTilemap; set => overlayTilemap = value; }
    public GameObject[,] ItemGrid => _itemGrid;
    public GameObject[,] BoardCellGrid => _boardCellGrid;
    public GameObject[,] UnderGrid => _underGrid;
    public GameObject[,] OverlayGrid => _overlayGrid;

    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadAndDrawBoard(levelNumber);
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
        LoadAndDrawBoard(levelNumber);
    }

    public void LoadAndDrawBoard(int level)
    {
        if (LoadLevelData(level))
        {
            DrawBoard();
        }
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

        // Xác định kích thước ma trận bàn cờ
        int rows = 0;
        int cols = 0;

        if (_levelData.BoardLevel != null && _levelData.BoardLevel.TryGetValue(0, out var boardMatrix) && boardMatrix != null)
        {
            rows = boardMatrix.GetLength(0);
            cols = boardMatrix.GetLength(1);
        }
        else if (_levelData.NormalLayerItem != null && _levelData.NormalLayerItem.TryGetValue(0, out var normalMatrix) && normalMatrix != null)
        {
            rows = normalMatrix.GetLength(0);
            cols = normalMatrix.GetLength(1);
        }

        if (rows <= 0 || cols <= 0)
        {
            Debug.LogWarning("[Board] Dữ liệu ma trận level có kích thước không hợp lệ!");
            return;
        }

        _itemGrid = new GameObject[rows, cols];
        _boardCellGrid = new GameObject[rows, cols];
        _underGrid = new GameObject[rows, cols];
        _overlayGrid = new GameObject[rows, cols];

        // Tu dong can giua Grid quanh goc toa do (0, 0) de ban co nam ngay giua man hinh
        if (grid != null)
        {
            float originX = -cols * grid.cellSize.x * 0.5f;
            float originY = -rows * grid.cellSize.y * 0.5f;
            grid.transform.position = new Vector3(originX, originY, 0);
        }

        // 1. TẦNG BOARD: Chứa -1 (Blank), 0 (Board), 1 (Spawn)
        Transform boardParent = boardTilemap != null ? boardTilemap.transform : transform;
        if (_levelData.BoardLevel != null)
        {
            foreach (var kvp in _levelData.BoardLevel)
            {
                int[,] gridData = kvp.Value;
                if (gridData == null) continue;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int id = gridData[r, c];

                        // Tầng Board chứa -1: Blank, 0: Board, 1: Spawn
                        if (id == (int)EnumItemBoard.Board || id == (int)EnumItemBoard.Spawn)
                        {
                            Vector3 worldPos = GridUtils.RowColToWorld(grid, r, c, rows);
                            GameObject cellObj = pool.SpawnBoardItem(id, worldPos, Quaternion.identity, boardParent);
                            if (cellObj != null)
                            {
                                cellObj.name = $"BoardCell_{id}_{r}_{c}";
                                _spawnedObjects.Add(cellObj);
                                _boardCellGrid[r, c] = cellObj;
                            }
                        }
                    }
                }
            }
        }

        // 2. TẦNG 2: Layer Under (Lớp dưới: Điểm sinh ngọc Spawner id = 1 / Spawn, nền đặc biệt...)
        Transform underParent = underTilemap != null ? underTilemap.transform : transform;
        if (_levelData.UnderLayerItem != null)
        {
            foreach (var kvp in _levelData.UnderLayerItem)
            {
                int[,] gridData = kvp.Value;
                if (gridData == null) continue;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int id = gridData[r, c];
                        if (id != (int)EnumItemBoard.Blank && id > 0)
                        {
                            Vector3 worldPos = GridUtils.RowColToWorld(grid, r, c, rows);
                            GameObject underObj = pool.SpawnBoardItem(id, worldPos, Quaternion.identity, underParent);
                            if (underObj != null)
                            {
                                underObj.name = $"UnderItem_{id}_{r}_{c}";
                                _spawnedObjects.Add(underObj);
                                _underGrid[r, c] = underObj;
                            }
                        }
                    }
                }
            }
        }

        // 3. TẦNG 3: Layer Normal (Các viên ngọc Match-3: 2: Red, 3: Blue, 4: Green, 5: Yellow, 6: Purple, 7: Orange, 8: Pink)
        Transform normalParent = normalTilemap != null ? normalTilemap.transform : transform;
        if (_levelData.NormalLayerItem != null)
        {
            foreach (var kvp in _levelData.NormalLayerItem)
            {
                int[,] gridData = kvp.Value;
                if (gridData == null) continue;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int id = gridData[r, c];

                        // Chỉ spawn các item ngọc hợp lệ (id >= 102)
                        if (id != (int)EnumItemBoard.Blank && id >= (int)EnumItemBoard.Red)
                        {
                            Vector3 worldPos = GridUtils.RowColToWorld(grid, r, c, rows);
                            GameObject itemObj = pool.SpawnBoardItem(id, worldPos, Quaternion.identity, normalParent);
                            if (itemObj != null)
                            {
                                itemObj.name = $"NormalItem_{id}_{r}_{c}";
                                _spawnedObjects.Add(itemObj);
                                _itemGrid[r, c] = itemObj;
                            }
                        }
                    }
                }
            }
        }

        // 4. TẦNG 4: Layer Overlay (Lớp phủ trên ngọc: băng tuyết, dây xích, lồng sắt, màng nhện...)
        Transform overlayParent = overlayTilemap != null ? overlayTilemap.transform : transform;
        if (_levelData.OverLayerItem != null)
        {
            foreach (var kvp in _levelData.OverLayerItem)
            {
                int[,] gridData = kvp.Value;
                if (gridData == null) continue;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int id = gridData[r, c];
                        if (id != (int)EnumItemBoard.Blank && id > 0)
                        {
                            Vector3 worldPos = GridUtils.RowColToWorld(grid, r, c, rows);
                            GameObject overlayObj = pool.SpawnBoardItem(id, worldPos, Quaternion.identity, overlayParent);
                            if (overlayObj != null)
                            {
                                overlayObj.name = $"OverlayItem_{id}_{r}_{c}";
                                _spawnedObjects.Add(overlayObj);
                                _overlayGrid[r, c] = overlayObj;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"[Board] Đã vẽ xong bàn cờ Level {levelNumber} trên 4 tầng Grid: {rows} hàng x {cols} cột ({_spawnedObjects.Count} items).");
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
        _itemGrid = null;
        _boardCellGrid = null;
        _underGrid = null;
        _overlayGrid = null;
    }


    private void OnDestroy()
    {
        ClearBoard();
    }
}



