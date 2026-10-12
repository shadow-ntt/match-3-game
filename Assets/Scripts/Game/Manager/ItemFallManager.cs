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
    }

    // Thực hiện toàn bộ chu trình rơi (thẳng và chéo) bất đồng bộ bằng UniTask
    public async UniTask<bool> OnItemFallAsync()
    {
        if (isFalling || board == null || board.MidGrid == null || board.BoardCellGrid == null)
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
        if (board == null || !board.IsInBounds(x, y)) return true;
        if (board.BoardCellGrid == null || board.BoardCellGrid[x, y] == null) return true;
        return BoardItemUtils.HasObstacleAt(board, x, y);
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
    private BoardCell GetBoardCell(int x, int y) => board.GetBoardCell(x, y);

    // Kiểm tra ô có đang hợp lệ, trống và chưa bị đặt chỗ bởi item khác
    private bool IsCellEmptyAndAvailable(int x, int y) => board.IsCellAvailableForFill(x, y);

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

    // Kiểm tra ngọc tại (x, y) có thể trượt chéo sang hướng dx (dx = -1 hoặc 1) không
    private bool CanFallDiagonal(int x, int y, int dx)
    {
        int nx = x + dx;
        int ny = y - 1;
        if (!board.IsInBounds(nx, ny)) return false;

        // Ô bên dưới viên ngọc (x, y - 1) PHẢI BỊ CHẶN (ngoài biên bàn cờ hoặc là vật cản)
        // Nếu ô bên dưới trống hoặc là ngọc bình thường đã đứng vững, ngọc ở (x, y) không được trượt chéo
        // bool isBelowBlocked = (y - 1 < 0) || IsObstacle(x, y - 1);
        // if (!isBelowBlocked) return false;

        // Ô góc bên cạnh (nx, y)  LÀ VẬT CẢN 
        if (!IsObstacle(nx, y)) return false;

        // Ô đích (nx, ny) phải đang trống và sẵn sàng nhận ngọc
        if (!IsCellEmptyAndAvailable(nx, ny)) return false;

        // Ô đích (nx, ny) phải là ô BỊ CHẶN RƠI THẲNG TỪ TRÊN XUỐNG
        // Nếu cột nx thông suốt từ trên xuống (có thể nhận ngọc rơi thẳng từ Spawner hoặc ngọc trên rơi xuống),
        // thì cột nx sẽ tự lấp đầy, không để ngọc từ cột x trượt chéo sang cướp ô
        if (!IsBlockedFromFallingStraight(nx, ny)) return false;

        return true;
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

    // Thực hiện 1 lượt rơi (Ưu tiên rơi thẳng trước, khi không còn ô rơi thẳng mới xét rơi chéo)
    private async UniTask<bool> StepFallAsync()
    {
        // Phase 1: Ưu tiên tất cả các ô có thể rơi thẳng trước
        var straightTasks = new List<UniTask>();
        CollectStraightFalls(straightTasks);
        if (straightTasks.Count > 0)
        {
            await UniTask.WhenAll(straightTasks);
            return true;
        }

        // Phase 2: Kiểm tra rơi chéo cho các viên chưa thể rơi thẳng
        var diagonalTasks = new List<UniTask>();
        CollectDiagonalFalls(diagonalTasks);
        if (diagonalTasks.Count > 0)
        {
            await UniTask.WhenAll(diagonalTasks);
            return true;
        }

        return false;
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
                GameObject itemObj = board.MidGrid[x, y];
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
                GameObject itemObj = board.MidGrid[x, y];
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
        GameObject itemObj = board.MidGrid[fromX, fromY];
        BoardCell targetCell = GetBoardCell(fromX, targetY);

        // Cập nhật logic grid ngay lập tức trước khi chạy tween
        board.MidGrid[fromX, targetY] = itemObj;
        board.MidGrid[fromX, fromY] = null;

        BoardCell fromCell = GetBoardCell(fromX, fromY);
        if (fromCell != null) fromCell.SetState(EnumStateBoardCell.Empty);

        if (targetCell != null)
        {
            targetCell.SetState(EnumStateBoardCell.Falling);
            targetCell.IsGettingFilled = true;
        }

        int distance = fromY - targetY;
        float duration = BoardItemUtils.CalculateFallDuration(fallDuration, distance);
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
        GameObject itemObj = board.MidGrid[fromX, fromY];
        BoardCell targetCell = GetBoardCell(destX, destY);

        // Cập nhật logic grid ngay lập tức trước khi chạy tween
        board.MidGrid[destX, destY] = itemObj;
        board.MidGrid[fromX, fromY] = null;

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
        float duration = Mathf.Max(diagonalDuration, BoardItemUtils.CalculateFallDuration(fallDuration, distance));

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
