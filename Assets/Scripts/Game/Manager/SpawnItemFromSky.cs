using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Quản lý sinh item mới từ trên cao (Spawn From Sky) với cơ chế rơi thẳng
public class SpawnItemFromSky : Singleton<SpawnItemFromSky>
{
    [SerializeField] private Board board;
    [SerializeField] private float fallDuration = 0.2f;
    [SerializeField] private Ease fallEase = Ease.InQuad;
    [SerializeField] private float spawnSpacing = 1.0f;
    [SerializeField]
    private List<EnumItemBoard> allowedColors = new List<EnumItemBoard>
    {
        EnumItemBoard.Red,
        EnumItemBoard.Blue,
        EnumItemBoard.Green,
        EnumItemBoard.Yellow
    };

    private bool _isSpawning;

    public Board Board => board;
    public bool IsSpawning => _isSpawning;
    public List<EnumItemBoard> AllowedColors => allowedColors;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
    }

    // Quét các ô trống trên bàn cờ và sinh ngọc mới (chỉ rơi thẳng) bất đồng bộ bằng UniTask
    public async UniTask<bool> SpawnFromSkyAsync()
    {
        if (_isSpawning || board == null || board.NormalGrid == null || board.BoardCellGrid == null)
        {
            return false;
        }

        _isSpawning = true;
        try
        {
            int width = board.Width;
            var columnSpawnCount = new Dictionary<int, int>();
            var tasks = new List<UniTask>();

            Transform parent = board.NormalTilemap != null ? board.NormalTilemap.transform : board.transform;

            for (int x = 0; x < width; x++)
            {
                List<int> emptyYList = GetEmptyYList(x);
                for (int i = 0; i < emptyYList.Count; i++)
                {
                    int targetY = emptyYList[i];
                    int spawnOffset = columnSpawnCount.GetValueOrDefault(x, 0);

                    Vector3[] path = FindStraightSpawnPath(x, targetY, spawnOffset);
                    if (path == null)
                    {
                        continue;
                    }

                    columnSpawnCount[x] = spawnOffset + 1;
                    tasks.Add(SpawnAndAnimateAsync(x, targetY, path, parent));
                }
            }

            if (tasks.Count == 0)
            {
                return false;
            }

            await UniTask.WhenAll(tasks);
            return true;
        }
        finally
        {
            _isSpawning = false;
        }
    }

    // Tìm điểm spawn thẳng từ đỉnh cột targetX và đích đến
    private Vector3[] FindStraightSpawnPath(int targetX, int targetY, int spawnOffset)
    {
        if (!CanSpawnStraight(targetX, targetY)) return null;

        int topY = GetTopY(targetX);
        if (topY < 0) return null;

        Vector3 spawnOrigin = GridUtils.GridToWorld(board.Grid, targetX, topY + 1);
        Vector3 spawnPos = spawnOrigin + Vector3.up * (spawnSpacing * spawnOffset);
        Vector3 targetPos = GridUtils.GridToWorld(board.Grid, targetX, targetY);
        return new[] { spawnPos, targetPos };
    }

    // Kiểm tra cột targetX có đường thông thẳng từ trên trời xuống targetY không
    public bool CanSpawnStraight(int x, int targetY)
    {
        int topY = GetTopY(x);
        if (topY < 0 || topY < targetY) return false;

        for (int y = targetY + 1; y <= topY; y++)
        {
            if (IsCellBlockedForSpawn(x, y)) return false;
        }

        return true;
    }

    // Kiểm tra một ô có chặn đường spawn từ trời không
    private bool IsCellBlockedForSpawn(int x, int y)
    {
        if (!board.IsInBounds(x, y)) return true;
        if (board.BoardCellGrid[x, y] == null) return true;
        if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null) return true;
        if (board.UnderGrid != null && board.UnderGrid[x, y] != null) return true;

        // Nếu đã có gem trong NormalGrid và không phải đang rơi thì bị chặn
        if (board.NormalGrid[x, y] != null)
        {
            var cell = GetBoardCell(x, y);
            if (cell == null || !cell.IsGettingFilled)
            {
                return true;
            }
        }

        return false;
    }

    // Tính thời lượng rơi dựa trên khoảng cách rơi thẳng
    private float CalculateFallDuration(Vector3 startPos, Vector3 endPos)
    {
        float totalDist = Vector3.Distance(startPos, endPos);
        float cellSize = board.Grid != null ? board.Grid.cellSize.y : 1f;
        return fallDuration * Mathf.Sqrt(Mathf.Max(1f, totalDist / cellSize));
    }

    // Spawn đối tượng mới và chạy tween rơi thẳng từ path[0] xuống path[1]
    private async UniTask SpawnAndAnimateAsync(
        int targetX,
        int targetY,
        Vector3[] path,
        Transform parent)
    {
        int colorIndex = UnityEngine.Random.Range(0, allowedColors.Count);
        int colorId = (int)allowedColors[colorIndex];

        GameObject newObj = Pooltem.Instance.SpawnBoardItem(colorId, path[0], Quaternion.identity, parent);
        if (newObj == null) return;

        newObj.name = $"NormalItem_{colorId}_{targetX}_{targetY}";

        // Cập nhật grid logic ngay lập tức
        board.NormalGrid[targetX, targetY] = newObj;
        var targetCell = GetBoardCell(targetX, targetY);
        if (targetCell != null)
        {
            targetCell.SetState(EnumStateBoardCell.Falling);
            targetCell.IsGettingFilled = true;
        }

        float duration = CalculateFallDuration(path[0], path[1]);

        try
        {
            await newObj.transform.DOMove(path[1], duration).SetEase(fallEase).ToUniTask();
        }
        finally
        {
            if (targetCell != null)
            {
                targetCell.SetState(EnumStateBoardCell.Occupied);
                targetCell.IsGettingFilled = false;
            }
        }
    }

    // Kiểm tra ô (x, y) có bị chặn bởi vật cản phía trên không
    public bool IsBlockedFromSky(int x, int y)
    {
        return !CanSpawnStraight(x, y);
    }

    // Lấy danh sách các tọa độ Y đang trống của cột x
    public List<int> GetEmptyYList(int x)
    {
        var emptyYList = new List<int>();
        int height = board.Height;

        for (int y = 0; y < height; y++)
        {
            var cellObj = board.BoardCellGrid[x, y];
            if (cellObj == null) continue;
            if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null) continue;
            if (board.UnderGrid != null && board.UnderGrid[x, y] != null) continue;
            if (board.BoardCellGrid[x, y].TryGetComponent<IBoardItem>(out var boardItem) && boardItem.ItemId == EnumItemBoard.Spawn) continue;
            // Ô trên bàn cờ chưa có normal item và chưa bị đặt chỗ bởi item khác
            if (board.NormalGrid[x, y] == null)
            {
                var cell = cellObj.GetComponent<BoardCell>();
                if (cell == null || !cell.IsGettingFilled)
                {
                    emptyYList.Add(y);
                }
            }
        }

        return emptyYList;
    }

    // Lấy tọa độ Y cao nhất có BoardCell hợp lệ của cột x
    public int GetTopY(int x)
    {
        for (int y = board.Height - 1; y >= 0; y--)
        {
            if (board.BoardCellGrid[x, y] != null) return y;
        }
        return -1;
    }

    // Lấy BoardCell tại tọa độ (x, y)
    private BoardCell GetBoardCell(int x, int y)
    {
        if (!board.IsInBounds(x, y)) return null;
        var cellObj = board.BoardCellGrid[x, y];
        if (cellObj == null) return null;
        return cellObj.GetComponent<BoardCell>();
    }
}
