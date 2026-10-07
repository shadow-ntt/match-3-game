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
            var tasks = new List<UniTask>();

            Transform parent = board.NormalTilemap != null ? board.NormalTilemap.transform : board.transform;

            for (int x = 0; x < width; x++)
            {
                // Tìm ô Spawner cao nhất trong cột x – đây là điểm item xuất hiện
                int spawnerY = FindSpawnerY(x);
                if (spawnerY < 0) continue;

                // Ô nhận item đầu tiên nằm ngay dưới Spawner
                int startY = spawnerY - 1;
                if (startY < 0) continue;

                int emptyCount = CountEmptyCellsFromTop(x, startY);
                if (emptyCount == 0) continue;

                // Sinh item từ ô thấp nhất lên đến ô cao nhất:
                // Ô thấp nhất có spawnOffset = 0 (rơi trước/dẫn đầu), các ô bên trên có offset tăng dần
                int bottomY = startY - emptyCount + 1;
                for (int y = bottomY; y <= startY; y++)
                {
                    int spawnOffset = y - bottomY;
                    Vector3[] path = CreateSpawnPathFromSpawner(x, y, spawnerY, spawnOffset);
                    tasks.Add(SpawnAndAnimateAsync(x, y, path, parent));
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

    // Tìm tọa độ Y của ô Spawner cao nhất trong cột x (-1 nếu không có)
    private int FindSpawnerY(int x)
    {
        int topY = board.GetTopY(x);
        if (topY < 0) return -1;

        for (int y = topY; y >= 0; y--)
        {
            if (board.IsSpawnerCell(x, y)) return y;
        }
        return -1;
    }

    // Tạo đường spawn từ vị trí ô Spawner xuống ô đích targetY
    private Vector3[] CreateSpawnPathFromSpawner(int targetX, int targetY, int spawnerY, int spawnOffset)
    {
        Vector3 spawnOrigin = GridUtils.GridToWorld(board.Grid, targetX, spawnerY);
        Vector3 spawnPos = spawnOrigin + Vector3.up * (spawnSpacing * spawnOffset);
        Vector3 targetPos = GridUtils.GridToWorld(board.Grid, targetX, targetY);
        return new[] { spawnPos, targetPos };
    }

    // Quét từ startY đi xuống để tìm số lượng ô trống liên tiếp có thể nhận item rơi từ trời
    private int CountEmptyCellsFromTop(int x, int startY)
    {
        int emptyCount = 0;
        for (int y = startY; y >= 0; y--)
        {
            if (board.IsCellAvailableForFill(x, y))
            {
                emptyCount++;
            }
            else
            {
                // Gặp vật cản hoặc ô đã có item:
                // Các ô bên dưới bị chặn và không thể nhận item rơi thẳng từ trên trời
                break;
            }
        }
        return emptyCount;
    }

    // Tính thời lượng rơi dựa trên khoảng cách rơi thẳng
    private float CalculateFallDuration(Vector3 startPos, Vector3 endPos)
    {
        float totalDist = Vector3.Distance(startPos, endPos);
        float cellSize = board.Grid != null ? board.Grid.cellSize.y : 1f;
        return BoardItemUtils.CalculateFallDuration(fallDuration, totalDist / cellSize);
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
        var targetCell = board.GetBoardCell(targetX, targetY);
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

    // Lấy tọa độ Y của ô Spawner cao nhất của cột x (-1 nếu không có)
    public int GetSpawnerY(int x) => FindSpawnerY(x);
}
