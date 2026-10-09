using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Booster LightBall: xoa tat ca gem cung mau voi swapTarget (gem bi swap vao LightBall)
public class LightBallItem : BoosterItem
{
    // Phat animation phong to thu nho va xoay tron phat sang
    public override async UniTask PlayActivationAnimationAsync()
    {
        var scaleTween = transform.DOScale(1.35f, 0.12f).SetLoops(2, LoopType.Yoyo);
        var rotTween = transform.DORotate(new Vector3(0, 0, 180f), 0.24f, RotateMode.FastBeyond360);
        await UniTask.WhenAll(scaleTween.ToUniTask(), rotTween.ToUniTask());
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;
        EnumItemBoard targetColor = EnumItemBoard.Blank;
        if (swapTarget != null && BoardItemUtils.IsValidNormalItem(swapTarget))
        {
            targetColor = swapTarget.ItemId;
        }
        else
        {
            // Neu khong co swapTarget (do no day chuyen), chon ngau nhien 1 mau gem tren ban co
            targetColor = PickRandomColor(board);
        }

        if (targetColor == EnumItemBoard.Blank) return cells;

        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (col == x && row == y) continue;
                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (obj.TryGetComponent<IBoardItem>(out var item) && item.ItemId == targetColor)
                {
                    cells.Add(new Vector2Int(col, row));
                }
            }
        }

        return cells;
    }

    // Chon ngau nhien mot mau gem dang co tren ban co
    private EnumItemBoard PickRandomColor(Board board)
    {
        var colors = new List<EnumItemBoard>();
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsValidNormalItem(item))
                {
                    if (!colors.Contains(item.ItemId))
                    {
                        colors.Add(item.ItemId);
                    }
                }
            }
        }

        if (colors.Count == 0) return EnumItemBoard.Blank;
        return colors[Random.Range(0, colors.Count)];
    }
}
