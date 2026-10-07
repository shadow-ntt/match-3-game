using System.Collections.Generic;
using UnityEngine;

// Giao dien khai bao kha nang kich hoat hieu ung Booster
public interface IBoosterActivatable
{
    List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null);
}
