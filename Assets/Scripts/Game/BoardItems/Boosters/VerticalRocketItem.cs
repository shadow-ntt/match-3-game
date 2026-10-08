using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Booster ten lua doc: no toan bo cot doc X cua o booster
public class VerticalRocketItem : BoosterItem
{
    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;

        for (int row = 0; row < board.Height; row++)
        {
            if (row == y) continue;
            if (board.MidGrid[x, row] != null)
            {
                cells.Add(new Vector2Int(x, row));
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

