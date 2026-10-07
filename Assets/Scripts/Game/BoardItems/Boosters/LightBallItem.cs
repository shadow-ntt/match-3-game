using System.Collections.Generic;
using UnityEngine;
using Utils;

// Booster LightBall: xoa tat ca gem cung mau voi swapTarget (gem bi swap vao LightBall)
public class LightBallItem : BoosterItem
{
    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;
        if (swapTarget == null || !BoardItemUtils.IsValidNormalItem(swapTarget)) return cells;

        EnumItemBoard targetColor = swapTarget.ItemId;
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
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
}
