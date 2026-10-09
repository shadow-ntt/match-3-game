using System.Collections.Generic;
using UnityEngine;
using Utils;

// Tinh toan vung anh huong khi ket hop 2 Booster voi nhau
public static class BoosterComboResolver
{
    // Tra ve danh sach cac o bi anh huong boi combo giua 2 booster tai tam (cx, cy)
    public static List<Vector2Int> GetComboAffectedCells(EnumItemBoard typeA, EnumItemBoard typeB, Board board, int cx, int cy)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null || board.MidGrid == null) return new List<Vector2Int>();

        // Chuan hoa thu tu de giam bot so truong hop can xu ly doi xung
        EnumItemBoard first = typeA <= typeB ? typeA : typeB;
        EnumItemBoard second = typeA <= typeB ? typeB : typeA;

        ResolveCombo(first, second, board, cx, cy, result);

        return new List<Vector2Int>(result);
    }

    // Xu ly tung cap booster
    private static void ResolveCombo(EnumItemBoard a, EnumItemBoard b, Board board, int cx, int cy, HashSet<Vector2Int> result)
    {
        // 1. Cap chua LightBall (305)
        if (b == EnumItemBoard.LightBall)
        {
            if (a == EnumItemBoard.LightBall)
            {
                // LightBall + LightBall: Xoa toan bo ban co
                AddAllBoardItems(board, result);
                return;
            }

            // LightBall + Booster khac: Chon mau gem co so luong nhieu nhat tren ban co, doi thanh booter dang ket hop
            EnumItemBoard dominantColor = PickDominantColor(board);
            if (dominantColor != EnumItemBoard.Blank)
            {
                AddAllGemsOfColor(board, dominantColor, result);
            }
            return;
        }

        // 2. Cap chua Missile (304)
        if (b == EnumItemBoard.Missile)
        {
            if (a == EnumItemBoard.Missile)
            {
                // Missile + Missile: Ban 3 qua ten lua tim 3 muc tieu
                AddSpecialTargets(board, 3, result);
                return;
            }

            if (a == EnumItemBoard.TNT)
            {
                // Missile + TNT: No vung 3x3 tai tam va no vung 3x3 tai muc tieu missile tim duoc
                AddArea(board, cx - 1, cx + 1, cy - 1, cy + 1, result);
                Vector2Int? target = FindSingleTarget(board, result);
                if (target.HasValue)
                {
                    AddArea(board, target.Value.x - 1, target.Value.x + 1, target.Value.y - 1, target.Value.y + 1, result);
                }
                return;
            }

            if (a == EnumItemBoard.HorizontalRocket)
            {
                // Missile + HRocket: No toan hang tai tam va toan hang tai muc tieu
                AddRow(board, cy, result);
                Vector2Int? target = FindSingleTarget(board, result);
                if (target.HasValue)
                {
                    AddRow(board, target.Value.y, result);
                }
                return;
            }

            if (a == EnumItemBoard.VerticalRocket)
            {
                // Missile + VRocket: No toan cot tai tam va toan cot tai muc tieu
                AddColumn(board, cx, result);
                Vector2Int? target = FindSingleTarget(board, result);
                if (target.HasValue)
                {
                    AddColumn(board, target.Value.x, result);
                }
                return;
            }
        }

        // 3. Cap chua TNT (303)
        if (b == EnumItemBoard.TNT)
        {
            if (a == EnumItemBoard.TNT)
            {
                // TNT + TNT: No vung 5x5 sieu lon quanh tam
                AddArea(board, cx - 2, cx + 2, cy - 2, cy + 2, result);
                return;
            }

            if (a == EnumItemBoard.HorizontalRocket || a == EnumItemBoard.VerticalRocket)
            {
                // TNT + Rocket (Ngang hoac Doc): No 3 hang ngang va 3 cot doc (Mega Rocket)
                for (int dy = -1; dy <= 1; dy++)
                {
                    AddRow(board, cy + dy, result);
                }
                for (int dx = -1; dx <= 1; dx++)
                {
                    AddColumn(board, cx + dx, result);
                }
                return;
            }
        }

        // 4. Cap Rocket voi nhau (301, 302)
        if (a == EnumItemBoard.HorizontalRocket && b == EnumItemBoard.HorizontalRocket)
        {
            // HRocket + HRocket: No 3 hang ngang (cy - 1, cy, cy + 1)
            for (int dy = -1; dy <= 1; dy++)
            {
                AddRow(board, cy + dy, result);
            }
            return;
        }

        if (a == EnumItemBoard.VerticalRocket && b == EnumItemBoard.VerticalRocket)
        {
            // VRocket + VRocket: No 3 cot doc (cx - 1, cx, cx + 1)
            for (int dx = -1; dx <= 1; dx++)
            {
                AddColumn(board, cx + dx, result);
            }
            return;
        }

        if ((a == EnumItemBoard.HorizontalRocket && b == EnumItemBoard.VerticalRocket) ||
            (a == EnumItemBoard.VerticalRocket && b == EnumItemBoard.HorizontalRocket))
        {
            // HRocket + VRocket: Dau thap no ca hang cy va cot cx
            AddRow(board, cy, result);
            AddColumn(board, cx, result);
            return;
        }
    }

    // Them tat ca o tren hang y
    private static void AddRow(Board board, int y, HashSet<Vector2Int> result)
    {
        if (y < 0 || y >= board.Height) return;
        for (int col = 0; col < board.Width; col++)
        {
            result.Add(new Vector2Int(col, y));
        }
    }

    // Them tat ca o tren cot x
    private static void AddColumn(Board board, int x, HashSet<Vector2Int> result)
    {
        if (x < 0 || x >= board.Width) return;
        for (int row = 0; row < board.Height; row++)
        {
            result.Add(new Vector2Int(x, row));
        }
    }

    // Them tat ca o trong hinh chu nhat tu [minX..maxX, minY..maxY]
    private static void AddArea(Board board, int minX, int maxX, int minY, int maxY, HashSet<Vector2Int> result)
    {
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
    private static void AddAllBoardItems(Board board, HashSet<Vector2Int> result)
    {
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
    private static void AddAllGemsOfColor(Board board, EnumItemBoard color, HashSet<Vector2Int> result)
    {
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

    // Chon mau gem thuong co so luong nhieu nhat tren ban co (hoa nhau thi chon random giua cac mau cao nhat)
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
    private static EnumItemBoard PickRandomNormalColor(Board board)
    {
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
    private static Vector2Int? FindSingleTarget(Board board, HashSet<Vector2Int> exclude)
    {
        var targets = new List<Vector2Int>();
        FindSpecialTargets(board, 1, targets, exclude);
        return targets.Count > 0 ? targets[0] : (Vector2Int?)null;
    }

    // Them N muc tieu dac biet vao ket qua
    private static void AddSpecialTargets(Board board, int count, HashSet<Vector2Int> result)
    {
        var targets = new List<Vector2Int>();
        FindSpecialTargets(board, count, targets, result);
        for (int i = 0; i < targets.Count; i++)
        {
            result.Add(targets[i]);
        }
    }

    // Tim danh sach N muc tieu dac biet
    private static void FindSpecialTargets(Board board, int count, List<Vector2Int> outList, HashSet<Vector2Int> exclude)
    {
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
}
