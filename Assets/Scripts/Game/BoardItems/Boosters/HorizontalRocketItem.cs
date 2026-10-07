using System.Collections.Generic;
using UnityEngine;

// Booster ten lua ngang: no toan bo hang ngang Y cua o booster
public class HorizontalRocketItem : BoosterItem
{
    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) Debug.LogWarning("board == null || board.MidGrid == null");
        Debug.Log("board.Width:" + board.Width);
        for (int col = 0; col < board.Width; col++)
        {
            if (col == x) continue;
            if (board.MidGrid[col, y] != null)
            {
                cells.Add(new Vector2Int(col, y));
            }
        }

        return cells;
    }
}
