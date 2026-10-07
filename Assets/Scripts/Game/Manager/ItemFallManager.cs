using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Quản lý logic rơi item xuống các ô trống theo hệ tọa độ (X, Y) bao gồm rơi thẳng và rơi chéo
public class ItemFallManager : Singleton<ItemFallManager>
{
    [SerializeField] private Board board;
    [SerializeField] private float fallDuration = 0.2f;
    [SerializeField] private float diagonalDuration = 0.25f;
    [SerializeField] private Ease fallEase = Ease.InQuad;

    private bool isFalling;

    public Board Board => board;
    public bool IsFalling => isFalling;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
    }

    // Thực hiện toàn bộ chu trình rơi (thẳng và chéo) bất đồng bộ bằng UniTask
    public async UniTask<bool> OnItemFallAsync()
    {
        if (isFalling || board == null || board.NormalGrid == null || board.BoardCellGrid == null)
        {
            return false;
        }

        isFalling = true;
        try
        {
            bool anyFell = false;

            while (true)
            {
                bool fellInPass = await StepFallAsync();
                if (!fellInPass)
                {
                    break;
                }

                anyFell = true;
            }

            return anyFell;
        }
        finally
        {
            isFalling = false;
        }
    }

    // Kiểm tra tọa độ (x, y) có phải là vật cản hoặc ngoài bàn cờ không
    public bool IsObstacle(int x, int y)
    {
        if (board == null) return true;
        if (!board.IsInBounds(x, y)) return true;
        if (board.BoardCellGrid[x, y] == null) return true;
        if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null) return true;
        if (board.UnderGrid != null && board.UnderGrid[x, y] != null) return true;

        GameObject normalObj = board.NormalGrid[x, y];
        if (normalObj != null && normalObj.TryGetComponent<IBoardItem>(out var item))
        {
            if (!BoardItemUtils.IsValidNormalItem(item))
            {
                return true;
            }
        }

        return false;
    }

    // Kiểm tra đường thẳng phía trên ô (targetX, targetY) có bị chặn bởi vật cản không
    public bool IsBlockedFromFallingStraight(int targetX, int targetY)
    {
        int height = board.Height;
        for (int y = targetY + 1; y < height; y++)
        {
            if (IsObstacle(targetX, y))
            {
                return true;
            }
        }
        return false;
    }

    // Lấy BoardCell tại tọa độ (x, y)
    private BoardCell GetBoardCell(int x, int y)
    {
        if (!board.IsInBounds(x, y)) return null;
        GameObject cellObj = board.BoardCellGrid[x, y];
        if (cellObj == null) return null;
        return cellObj.GetComponent<BoardCell>();
    }

    // Kiểm tra ô có đang hợp lệ, trống và chưa bị đặt chỗ bởi item khác
    private bool IsCellEmptyAndAvailable(int x, int y)
    {
        // 1. Kiểm tra tọa độ có nằm trong phạm vi bàn cờ không (tránh IndexOutOfRangeException)
        if (!board.IsInBounds(x, y)) return false;

        // 2. Tầng 1 (Nền): Ô phải có BoardCell hợp lệ (nếu null là ô trống ngoài bàn cờ hoặc lỗ khuyết của map)
        if (board.BoardCellGrid[x, y] == null) return false;

        // 3. Tầng 4 (Overlay): Nếu có vật cản che phủ bên trên (băng, xích, khóa...) thì item không thể rơi vào
        if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null) return false;

        // 4. Tầng 3 (Under): Nếu có vật cản tầng dưới (hộp gỗ, đá, chướng ngại vật...) thì item không thể rơi vào
        if (board.UnderGrid != null && board.UnderGrid[x, y] != null) return false;

        // 5. Tầng 2 (Normal): Nếu ô đã có ngọc/item thông thường đang chiếm giữ thì không thể rơi đè lên
        if (board.NormalGrid[x, y] != null) return false;

        // 6. Trạng thái ô: Nếu ô đang được ngọc khác rơi tới hoặc đang spawn lấp vào (đã bị đặt chỗ trước) thì bỏ qua
        BoardCell cell = GetBoardCell(x, y);
        if (cell != null && cell.IsGettingFilled) return false;

        return true;
    }

    // Trả về Y đích thấp nhất trong cột x, đi từ fromY - 1 xuống đáy (-1 nếu không thể rơi thẳng)
    private int FindStraightTarget(int x, int fromY)
    {
        if (fromY - 1 < 0) return -1;
        if (!IsCellEmptyAndAvailable(x, fromY - 1)) return -1;

        int targetY = fromY - 1;
        while (targetY - 1 >= 0)
        {
            if (IsCellEmptyAndAvailable(x, targetY - 1))
            {
                targetY--;
            }
            else
            {
                break;
            }
        }

        return targetY;
    }

    // Kiểm tra ngọc tại (x, y) có thể trượt chéo sang hướng dx không
    private bool CanFallDiagonal(int x, int y, int dx)
    {
        int nx = x + dx;
        int ny = y - 1;
        if (!board.IsInBounds(nx, ny)) return false;

        // Nếu thuộc tầng 3 (UnderGrid) thì mới cho phép rơi chéo
        if (board.OverlayGrid == null) return false;

        bool isUnderDx = board.IsInBounds(nx, y) && board.OverlayGrid[nx, y] != null;
        bool isUnderBelow = board.IsInBounds(x, y - 1) && board.OverlayGrid[x, y - 1] != null;

        if (!isUnderDx && !isUnderBelow) return false;

        return IsCellEmptyAndAvailable(nx, ny);
    }

    // Trả về (destX, destY) ô đích chéo hợp lệ thấp nhất ((-1, -1) nếu không thể rơi chéo)
    private (int destX, int destY) FindDiagonalTarget(int x, int y)
    {
        bool canLeft = CanFallDiagonal(x, y, -1);
        bool canRight = CanFallDiagonal(x, y, 1);
        if (!canLeft && !canRight) return (-1, -1);

        int dx;
        if (canLeft && canRight)
        {
            dx = UnityEngine.Random.value < 0.5f ? -1 : 1;
        }
        else
        {
            dx = canLeft ? -1 : 1;
        }

        int destX = x + dx;
        int destY = y - 1;

        while (destY - 1 >= 0)
        {
            if (IsCellEmptyAndAvailable(destX, destY - 1))
            {
                destY--;
            }
            else
            {
                break;
            }
        }

        return (destX, destY);
    }

    // Thực hiện 1 lượt rơi (Phase 1: rơi thẳng, Phase 2: rơi chéo)
    private async UniTask<bool> StepFallAsync()
    {
        var tasks = new List<UniTask>();


        // Phase 1: Ưu tiên tất cả các ô có thể rơi thẳng trước
        CollectStraightFalls(tasks);
        // Phase 2: Kiểm tra rơi chéo cho các viên chưa thể rơi thẳng
        CollectDiagonalFalls(tasks);

        if (tasks.Count == 0)
        {
            return false;
        }

        await UniTask.WhenAll(tasks);
        return true;
    }

    // Thu thập các task rơi thẳng trong lượt
    private void CollectStraightFalls(List<UniTask> tasks)
    {
        int width = board.Width;
        int height = board.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                GameObject itemObj = board.NormalGrid[x, y];
                if (itemObj == null) continue;
                if (IsObstacle(x, y)) continue;

                int targetY = FindStraightTarget(x, y);
                if (targetY >= 0)
                {
                    tasks.Add(AnimateStraightAsync(x, y, targetY));
                }
            }
        }
    }

    // Thu thập các task rơi chéo trong lượt
    private void CollectDiagonalFalls(List<UniTask> tasks)
    {
        int width = board.Width;
        int height = board.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                GameObject itemObj = board.NormalGrid[x, y];
                if (itemObj == null) continue;
                if (IsObstacle(x, y)) continue;

                var (destX, destY) = FindDiagonalTarget(x, y);
                if (destX >= 0)
                {
                    tasks.Add(AnimateDiagonalAsync(x, y, destX, destY));
                }
            }
        }
    }

    // Xử lý animation rơi thẳng
    private async UniTask AnimateStraightAsync(int fromX, int fromY, int targetY)
    {
        GameObject itemObj = board.NormalGrid[fromX, fromY];
        BoardCell targetCell = GetBoardCell(fromX, targetY);

        // Cập nhật logic grid ngay lập tức trước khi chạy tween
        board.NormalGrid[fromX, targetY] = itemObj;
        board.NormalGrid[fromX, fromY] = null;

        BoardCell fromCell = GetBoardCell(fromX, fromY);
        if (fromCell != null) fromCell.SetState(EnumStateBoardCell.Empty);

        if (targetCell != null)
        {
            targetCell.SetState(EnumStateBoardCell.Falling);
            targetCell.IsGettingFilled = true;
        }

        int distance = fromY - targetY;
        float duration = fallDuration * Mathf.Sqrt(distance);
        Vector3 destPos = GridUtils.GridToWorld(board.Grid, fromX, targetY);

        try
        {
            await itemObj.transform.DOMove(destPos, duration).SetEase(fallEase).ToUniTask();
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

    // Xử lý animation rơi chéo (qua waypoint entry và xuống đáy)
    private async UniTask AnimateDiagonalAsync(int fromX, int fromY, int destX, int destY)
    {
        GameObject itemObj = board.NormalGrid[fromX, fromY];
        BoardCell targetCell = GetBoardCell(destX, destY);

        // Cập nhật logic grid ngay lập tức trước khi chạy tween
        board.NormalGrid[destX, destY] = itemObj;
        board.NormalGrid[fromX, fromY] = null;

        BoardCell fromCell = GetBoardCell(fromX, fromY);
        if (fromCell != null) fromCell.SetState(EnumStateBoardCell.Empty);

        if (targetCell != null)
        {
            targetCell.SetState(EnumStateBoardCell.Falling);
            targetCell.IsGettingFilled = true;
        }

        float dx = Mathf.Abs(destX - fromX);
        float dy = fromY - destY;
        float distance = dx + dy;
        float duration = Mathf.Max(diagonalDuration, fallDuration * Mathf.Sqrt(distance));

        Vector3 entryPos = GridUtils.GridToWorld(board.Grid, destX, fromY - 1);
        Vector3 finalPos = GridUtils.GridToWorld(board.Grid, destX, destY);

        try
        {
            if (destY == fromY - 1)
            {
                await itemObj.transform.DOMove(finalPos, duration).SetEase(fallEase).ToUniTask();
            }
            else
            {
                Vector3[] path = new[] { entryPos, finalPos };
                await itemObj.transform.DOPath(path, duration, PathType.Linear).SetEase(fallEase).ToUniTask();
            }
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
}
