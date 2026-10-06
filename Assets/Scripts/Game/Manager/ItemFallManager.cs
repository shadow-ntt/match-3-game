using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Quan ly logic roi item xuong cac o trong theo he toa do (X, Y)
public class ItemFallManager : Singleton<ItemFallManager>
{
    [SerializeField] private Board board;
    [SerializeField] private float fallDuration = 0.2f;
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

    // Wrapper bat dong bo cho OnItemFall su dung UniTask
    public async UniTask<bool> OnItemFallAsync()
    {
        if (isFalling || board == null || board.NormalGrid == null || board.BoardCellGrid == null)
        {
            return false;
        }

        var utcs = new UniTaskCompletionSource<bool>();
        bool hasFalling = OnItemFall(() => utcs.TrySetResult(true));
        if (!hasFalling) return false;

        return await utcs.Task;
    }

    // Kiem tra tat ca cac boardcell, neu o ben duoi (y - 1) la Empty thi cho item roi xuong bang DOTween
    public bool OnItemFall(System.Action onComplete = null)
    {
        if (isFalling || board == null || board.NormalGrid == null || board.BoardCellGrid == null)
        {
            onComplete?.Invoke();
            return false;
        }

        int width = board.Width;
        int height = board.Height;
        int fallCount = 0;
        int activeTweens = 0;

        // Duyet tung cot tu duoi len tren (x chay tu 0 den width - 1)
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                GameObject itemObj = board.NormalGrid[x, y];
                if (itemObj == null) continue;

                // Kiem tra o ngay ben duoi (y - 1)
                GameObject belowCellObj = board.BoardCellGrid[x, y - 1];
                if (belowCellObj == null) continue;

                if (!belowCellObj.TryGetComponent<BoardCell>(out var belowCell) || !belowCell.IsEmpty)
                {
                    continue;
                }

                // Tim o trong thap nhat o ben duoi trong cung cot x
                int targetY = y - 1;
                while (targetY - 1 >= 0)
                {
                    GameObject nextCellObj = board.BoardCellGrid[x, targetY - 1];
                    if (nextCellObj != null && nextCellObj.TryGetComponent<BoardCell>(out var nextCell) && nextCell.IsEmpty)
                    {
                        targetY--;
                    }
                    else
                    {
                        break;
                    }
                }

                // Cap nhat trang thai BoardCell va vi tri trong NormalGrid
                GameObject currentCellObj = board.BoardCellGrid[x, y];
                if (currentCellObj != null && currentCellObj.TryGetComponent<BoardCell>(out var currentCell))
                {
                    currentCell.State = EnumStateBoardCell.Empty;
                }

                GameObject targetCellObj = board.BoardCellGrid[x, targetY];
                BoardCell targetCell = targetCellObj != null ? targetCellObj.GetComponent<BoardCell>() : null;
                if (targetCell != null)
                {
                    targetCell.State = EnumStateBoardCell.Falling;
                }

                board.NormalGrid[x, targetY] = itemObj;
                board.NormalGrid[x, y] = null;

                // Tinh vi tri World dich va khoang cach roi bang DOTween
                Vector3 targetWorldPos = GridUtils.GridToWorld(board.Grid, x, targetY);
                int distance = y - targetY;
                float duration = fallDuration * Mathf.Sqrt(distance);

                fallCount++;
                activeTweens++;

                itemObj.transform.DOMove(targetWorldPos, duration).SetEase(fallEase).OnComplete(() =>
                {
                    if (targetCell != null)
                    {
                        targetCell.State = EnumStateBoardCell.Occupied;
                    }

                    activeTweens--;
                    if (activeTweens == 0)
                    {
                        isFalling = false;
                        onComplete?.Invoke();
                    }
                });
            }
        }

        if (fallCount > 0)
        {
            isFalling = true;
        }
        else
        {
            onComplete?.Invoke();
        }

        return fallCount > 0;
    }
}
