using System.Collections.Generic;
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

    // Tim vat can tren cac tang Overlay, Under hoac item khong phai MidLayer tren MidGrid
    private Vector2Int? FindSpecialTarget(Board board, int originX, int originY)
    {
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (col == originX && row == originY) continue;

                if (board.OverlayGrid != null && board.OverlayGrid[col, row] != null)
                {
                    return new Vector2Int(col, row);
                }

                if (board.UnderGrid != null && board.UnderGrid[col, row] != null)
                {
                    return new Vector2Int(col, row);
                }

                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (!obj.TryGetComponent<IBoardItem>(out var item)) continue;

                if (!BoardItemUtils.IsMidLayer(item))
                {
                    return new Vector2Int(col, row);
                }
            }
        }

        return null;
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
