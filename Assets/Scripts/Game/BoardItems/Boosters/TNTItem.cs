using System.Collections.Generic;
using UnityEngine;

// Booster TNT: no vung 3x3 xung quanh tam booster
public class TNTItem : BoosterItem
{
    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (board.IsInBounds(nx, ny) && board.MidGrid[nx, ny] != null)
                {
                    cells.Add(new Vector2Int(nx, ny));
                }
            }
        }

        return cells;
    }
}
