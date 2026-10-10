using System.Collections.Generic;
using UnityEngine;
using Utils;

// Tien ich tinh toan toa do o va tim kiem muc tieu tren ban co cho cac Combo Booster
public static class BoosterComboGridUtils
{
    // Them tat ca o tren hang y
    public static void AddRow(Board board, int y, HashSet<Vector2Int> result)
    {
        if (board == null || y < 0 || y >= board.Height) return;
        for (int col = 0; col < board.Width; col++)
        {
            result.Add(new Vector2Int(col, y));
        }
    }

    // Them tat ca o tren cot x
    public static void AddColumn(Board board, int x, HashSet<Vector2Int> result)
    {
        if (board == null || x < 0 || x >= board.Width) return;
        for (int row = 0; row < board.Height; row++)
        {
            result.Add(new Vector2Int(x, row));
        }
    }

    // Them tat ca o trong hinh chu nhat tu [minX..maxX, minY..maxY]
    public static void AddArea(Board board, int minX, int maxX, int minY, int maxY, HashSet<Vector2Int> result)
    {
        if (board == null) return;
        for (int col = minX; col <= maxX; col++)
        {
            for (int row = minY; row <= maxY; row++)
            {
                if (board.IsInBounds(col, row))
                {
                    result.Add(new Vector2Int(col, row));
                }
            }
        }
    }

    // Them tat ca o co item tren ban co
    public static void AddAllBoardItems(Board board, HashSet<Vector2Int> result)
    {
        if (board == null || board.MidGrid == null) return;
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (board.MidGrid[col, row] != null || (board.OverlayGrid != null && board.OverlayGrid[col, row] != null))
                {
                    result.Add(new Vector2Int(col, row));
                }
            }
        }
    }

    // Them tat ca gem co mau chi dinh
    public static void AddAllGemsOfColor(Board board, EnumItemBoard color, HashSet<Vector2Int> result)
    {
        if (board == null || board.MidGrid == null) return;
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (obj.TryGetComponent<IBoardItem>(out var item) && item.ItemId == color)
                {
                    result.Add(new Vector2Int(col, row));
                }
            }
        }
    }

    // Chon mau gem thuong co so luong nhieu nhat tren ban co (hoa nhau thi chon random)
    public static EnumItemBoard PickDominantColor(Board board)
    {
        if (board == null || board.MidGrid == null) return EnumItemBoard.Blank;

        var colorCounts = new Dictionary<EnumItemBoard, int>();
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsValidNormalItem(item))
                {
                    if (colorCounts.TryGetValue(item.ItemId, out int count))
                    {
                        colorCounts[item.ItemId] = count + 1;
                    }
                    else
                    {
                        colorCounts[item.ItemId] = 1;
                    }
                }
            }
        }

        int maxCount = 0;
        var topColors = new List<EnumItemBoard>();
        foreach (var kvp in colorCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                topColors.Clear();
                topColors.Add(kvp.Key);
            }
            else if (kvp.Value == maxCount)
            {
                topColors.Add(kvp.Key);
            }
        }

        if (topColors.Count == 0) return EnumItemBoard.Blank;
        return topColors[Random.Range(0, topColors.Count)];
    }

    // Chon ngau nhien mot mau gem thuong dang ton tai tren ban co
    public static EnumItemBoard PickRandomNormalColor(Board board)
    {
        if (board == null || board.MidGrid == null) return EnumItemBoard.Blank;

        var existingColors = new List<EnumItemBoard>();
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsValidNormalItem(item))
                {
                    if (!existingColors.Contains(item.ItemId))
                    {
                        existingColors.Add(item.ItemId);
                    }
                }
            }
        }

        if (existingColors.Count == 0) return EnumItemBoard.Blank;
        return existingColors[Random.Range(0, existingColors.Count)];
    }

    // Tim 1 muc tieu uu tien vat can hoac random gem
    public static Vector2Int? FindSingleTarget(Board board, HashSet<Vector2Int> exclude)
    {
        var targets = new List<Vector2Int>();
        FindSpecialTargets(board, 1, targets, exclude);
        return targets.Count > 0 ? targets[0] : (Vector2Int?)null;
    }

    // Them N muc tieu dac biet vao ket qua
    public static void AddSpecialTargets(Board board, int count, HashSet<Vector2Int> result)
    {
        var targets = new List<Vector2Int>();
        FindSpecialTargets(board, count, targets, result);
        for (int i = 0; i < targets.Count; i++)
        {
            result.Add(targets[i]);
        }
    }

    // Tim danh sach N muc tieu dac biet
    public static void FindSpecialTargets(Board board, int count, List<Vector2Int> outList, HashSet<Vector2Int> exclude)
    {
        if (board == null || outList == null) return;

        // 1. Uu tien vat can o OverlayGrid hoac UnderGrid
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var pos = new Vector2Int(col, row);
                if (exclude != null && exclude.Contains(pos)) continue;
                if (outList.Contains(pos)) continue;

                bool isObstacle = false;
                if (board.OverlayGrid != null && board.OverlayGrid[col, row] != null)
                {
                    isObstacle = true;
                }
                else if (board.UnderGrid != null && board.UnderGrid[col, row] != null)
                {
                    var underObj = board.UnderGrid[col, row];
                    if (underObj != null && (!underObj.TryGetComponent<IBoardItem>(out var underItem) || underItem.ItemId != EnumItemBoard.Spawn))
                    {
                        isObstacle = true;
                    }
                }

                if (isObstacle)
                {
                    outList.Add(pos);
                    if (outList.Count >= count) return;
                }
            }
        }

        // 2. Tim vat can tren MidGrid
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var pos = new Vector2Int(col, row);
                if (exclude != null && exclude.Contains(pos)) continue;
                if (outList.Contains(pos)) continue;

                var obj = board.MidGrid[col, row];
                if (obj == null) continue;
                if (!obj.TryGetComponent<IBoardItem>(out var item)) continue;

                if (!BoardItemUtils.IsMidLayer(item))
                {
                    outList.Add(pos);
                    if (outList.Count >= count) return;
                }
            }
        }

        // 3. Neu van chua du, lay ngau nhien gem thuong tren ban co
        var randomCandidates = new List<Vector2Int>();
        for (int col = 0; col < board.Width; col++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var pos = new Vector2Int(col, row);
                if (exclude != null && exclude.Contains(pos)) continue;
                if (outList.Contains(pos)) continue;

                if (board.MidGrid[col, row] != null)
                {
                    randomCandidates.Add(pos);
                }
            }
        }

        while (outList.Count < count && randomCandidates.Count > 0)
        {
            int idx = Random.Range(0, randomCandidates.Count);
            outList.Add(randomCandidates[idx]);
            randomCandidates.RemoveAt(idx);
        }
    }

    // Thu thap danh sach cac o theo huong buoc di (step) tu diem xuat phat cho den mep ban co
    public static List<Vector2Int> GetRayCells(Board board, int startX, int startY, Vector2Int step)
    {
        var cells = new List<Vector2Int>();
        if (board == null) return cells;

        int curX = startX + step.x;
        int curY = startY + step.y;
        while (board.IsInBounds(curX, curY))
        {
            cells.Add(new Vector2Int(curX, curY));
            curX += step.x;
            curY += step.y;
        }
        return cells;
    }

    // Sap xep danh sach cac o theo khoang cach tang dan toi tam center
    public static void SortByDistance(List<Vector2Int> cells, Vector2Int center)
    {
        if (cells == null || cells.Count <= 1) return;
        cells.Sort((a, b) =>
        {
            float distA = Vector2Int.Distance(center, a);
            float distB = Vector2Int.Distance(center, b);
            return distA.CompareTo(distB);
        });
    }
}
