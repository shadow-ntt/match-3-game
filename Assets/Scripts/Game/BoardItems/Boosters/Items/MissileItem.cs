using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Booster Missile: bay den vat can gan nhat hoac random 1 gem bat ky
public class MissileItem : BoosterItem
{
    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;

        Vector2Int? target = FindSpecialTarget(board, x, y);
        if (!target.HasValue)
        {
            target = FindRandomGem(board, x, y);
        }

        if (target.HasValue)
        {
            cells.Add(target.Value);
        }

        return cells;
    }

    // Phat animation nhap nhay chuan bi phong dan
    public override async UniTask PlayActivationAnimationAsync()
    {
        await transform.DOScale(1.25f, 0.1f).SetLoops(2, LoopType.Yoyo).ToUniTask();
    }

    public override async UniTask ExecuteActivationEffectAsync(BoosterActivationContext context)
    {
        if (context?.EffectPlayer != null)
        {
            await context.EffectPlayer.PlayMissileEffectAsync(
                context.X, context.Y, context.AffectedCells, context.ChainBoosters);
        }
    }


    // Tim vat can gan nhat tren cac tang Overlay, Under hoac item khong phai MidLayer tren MidGrid
    private Vector2Int? FindSpecialTarget(Board board, int originX, int originY)
    {
        Vector2Int origin = new Vector2Int(originX, originY);
        Vector2Int? bestTarget = null;
        float minDistance = float.MaxValue;

        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (col == originX && row == originY) continue;

                bool isTarget = false;
                if (board.OverlayGrid != null && board.OverlayGrid[col, row] != null)
                {
                    isTarget = true;
                }
                else if (board.UnderGrid != null && board.UnderGrid[col, row] != null)
                {
                    var underObj = board.UnderGrid[col, row];
                    if (underObj != null && (!underObj.TryGetComponent<IBoardItem>(out var underItem) || underItem.ItemId != EnumItemBoard.Spawn))
                    {
                        isTarget = true;
                    }
                }
                else
                {
                    var obj = board.MidGrid[col, row];
                    if (obj != null && obj.TryGetComponent<IBoardItem>(out var item) && !BoardItemUtils.IsMidLayer(item))
                    {
                        isTarget = true;
                    }
                }

                if (isTarget)
                {
                    float dist = Vector2Int.Distance(origin, new Vector2Int(col, row));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestTarget = new Vector2Int(col, row);
                    }
                }
            }
        }

        return bestTarget;
    }

    // Chon ngau nhien mot gem tren ban co
    private Vector2Int? FindRandomGem(Board board, int originX, int originY)
    {
        var candidates = new List<Vector2Int>();
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (col == originX && row == originY) continue;
                if (board.MidGrid[col, row] != null)
                {
                    candidates.Add(new Vector2Int(col, row));
                }
            }
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
}
