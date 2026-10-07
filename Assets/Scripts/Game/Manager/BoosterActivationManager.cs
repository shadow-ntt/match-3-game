using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Quan ly kich hoat hieu ung cac Booster tren ban co
public class BoosterActivationManager : Singleton<BoosterActivationManager>
{
    [SerializeField] private Board board;
    [SerializeField] private PoolBlockBreakEffect poolParticle;
    [SerializeField] private float explosionDelay = 0.25f;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
        if (poolParticle == null) poolParticle = FindAnyObjectByType<PoolBlockBreakEffect>();
    }

    // Kich hoat booster tai (x, y), voi swapTarget la gem bi swap vao (dung cho LightBall)
    // Tra ve true neu booster kich hoat thanh cong
    public async UniTask<bool> ActivateBoosterAsync(int x, int y, IBoardItem swapTarget = null)
    {
        if (board == null || board.MidGrid == null || !board.IsInBounds(x, y)) return false;

        GameObject boosterObj = board.MidGrid[x, y];
        if (boosterObj == null) return false;
        if (!boosterObj.TryGetComponent<IBoosterActivatable>(out var booster)) return false;

        // Kiem tra hop le voi LightBall (can target la gem thuong hop le)
        if (booster is LightBallItem && (swapTarget == null || !BoardItemUtils.IsValidNormalItem(swapTarget)))
        {
            return false;
        }

        // Lay danh sach cac o bi anh huong
        var affectedCells = booster.GetAffectedCells(board, x, y, swapTarget);
        Debug.Log("booster, x, y" + x + ", " + y + " ");
        Debug.Log("affectedCells" + affectedCells.Count);
        // Xoa ban than booster truoc
        RemoveItem(x, y);

        // Phat hieu ung no va xoa tung o bi anh huong
        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item))
            {
                if (poolParticle != null && BoardItemUtils.IsValidNormalItem(item))
                {
                    poolParticle.Play(obj.transform.position, item.ItemId);
                }
            }

            RemoveItem(pos.x, pos.y);
        }

        // Cho hieu ung no
        if (explosionDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }

        return true;
    }

    // Kich hoat hieu ung combo giua 2 booster tai (xA, yA) va (xB, yB)
    public async UniTask<bool> ActivateBoosterComboAsync(int xA, int yA, IBoardItem boosterA, int xB, int yB, IBoardItem boosterB)
    {
        if (board == null || board.MidGrid == null) return false;
        if (boosterA == null || boosterB == null) return false;

        // Tam combo lay tai vi tri vuot den (xB, yB)
        int cx = xB;
        int cy = yB;

        // Lay danh sach cac o bi anh huong boi combo
        var affectedCells = BoosterComboResolver.GetComboAffectedCells(boosterA.ItemId, boosterB.ItemId, board, cx, cy);

        // Xoa 2 vien booster tham gia combo truoc
        RemoveItem(xA, yA);
        RemoveItem(xB, yB);

        // Phat hieu ung no va xoa tung o bi anh huong
        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;

            // Bo qua vi tri cua 2 booster neu co trong danh sach vi da xoa o tren
            if ((pos.x == xA && pos.y == yA) || (pos.x == xB && pos.y == yB)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item))
            {
                if (poolParticle != null && BoardItemUtils.IsValidNormalItem(item))
                {
                    poolParticle.Play(obj.transform.position, item.ItemId);
                }
            }

            RemoveItem(pos.x, pos.y);
        }

        // Cho hieu ung no
        if (explosionDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }

        return true;
    }

    // Xoa 1 item khoi MidGrid va cap nhat trang thai BoardCell ve Empty
    private void RemoveItem(int x, int y)
    {
        if (!board.IsInBounds(x, y)) return;
        GameObject itemObj = board.MidGrid[x, y];
        if (itemObj == null) return;

        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(itemObj);
        }
        board.MidGrid[x, y] = null;

        if (board.BoardCellGrid != null &&
            board.BoardCellGrid[x, y] != null &&
            board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Empty;
            cell.IsGettingFilled = false;
        }
    }
}
