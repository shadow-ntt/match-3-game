using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Booster ten lua ngang: no toan bo hang ngang Y cua o booster
public class HorizontalRocketItem : BoosterItem
{
    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;
        for (int col = 0; col < board.Width; col++)
        {
            if (col == x) continue;
            if (board.MidGrid[col, y] != null || (board.OverlayGrid != null && board.OverlayGrid[col, y] != null))
            {
                cells.Add(new Vector2Int(col, y));
            }
        }

        return cells;
    }

    // Phat animation nhap nhay sang (flash scale) truoc khi phong dan
    public override async UniTask PlayActivationAnimationAsync()
    {
        await transform.DOScale(1.3f, 0.08f).SetLoops(2, LoopType.Yoyo).ToUniTask();
    }
}

