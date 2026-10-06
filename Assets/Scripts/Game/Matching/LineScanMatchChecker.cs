using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

// Thuat toan quet toan bo ban co va phan tich cac cum Match theo he toa do (X, Y)
public class LineScanMatchChecker
{
    private readonly Board _board;

    // Struct dai dien cho mot doan thang lien tuc cung mau
    private struct MatchLine
    {
        public int ItemID;
        public List<Vector2Int> Cells;
        public bool IsHorizontal;
    }

    public LineScanMatchChecker(Board board)
    {
        _board = board;
    }

    // Quet toan bo bang va tra ve danh sach tat ca cac Match doc lap
    public List<MatchData> FindAllMatches(Vector2Int? priorityCenter = null)
    {
        var resultMatches = new List<MatchData>();

        if (_board == null || _board.NormalGrid == null || _board.Width == 0 || _board.Height == 0)
        {
            return resultMatches;
        }

        int width = _board.Width;
        int height = _board.Height;

        // 1. Quet cac doan thang ngang va doc lien tuc >= 3
        var horizontalLines = ScanLines(width, height, isHorizontal: true);
        var verticalLines = ScanLines(width, height, isHorizontal: false);

        // 2. Xu ly cac doan giao nhau (T, L, +) tao TNT
        var usedHorizontals = new HashSet<int>();
        var usedVerticals = new HashSet<int>();
        ProcessIntersectingMatches(horizontalLines, verticalLines, usedHorizontals, usedVerticals, priorityCenter, resultMatches);

        // 3. Xu ly cac doan ngang va doc doc lap (LightBall, Rocket, Normal)
        ProcessIndependentMatches(horizontalLines, usedHorizontals, priorityCenter, resultMatches);
        ProcessIndependentMatches(verticalLines, usedVerticals, priorityCenter, resultMatches);

        // 4. Quet cac khoi o vuong 2x2 tao Missile
        ScanSquareMatches(width, height, priorityCenter, resultMatches);

        return resultMatches;
    }

    // Quet cac doan thang lien tuc >= 3 theo chieu ngang (x) hoac doc (y)
    private List<MatchLine> ScanLines(int width, int height, bool isHorizontal)
    {
        var lines = new List<MatchLine>();
        int outerLength = isHorizontal ? height : width;
        int innerLength = isHorizontal ? width : height;

        for (int o = 0; o < outerLength; o++)
        {
            int currentId = -1;
            var currentLine = new List<Vector2Int>();

            for (int i = 0; i < innerLength; i++)
            {
                // Ngang: i la x, o la y. Doc: o la x, i la y
                Vector2Int pos = isHorizontal ? new Vector2Int(i, o) : new Vector2Int(o, i);
                if (IsCellMatchable(pos, out int itemId))
                {
                    if (itemId == currentId)
                    {
                        currentLine.Add(pos);
                    }
                    else
                    {
                        if (currentLine.Count >= 3)
                        {
                            lines.Add(new MatchLine { ItemID = currentId, Cells = new List<Vector2Int>(currentLine), IsHorizontal = isHorizontal });
                        }

                        currentId = itemId;
                        currentLine.Clear();
                        currentLine.Add(pos);
                    }
                }
                else
                {
                    if (currentLine.Count >= 3)
                    {
                        lines.Add(new MatchLine { ItemID = currentId, Cells = new List<Vector2Int>(currentLine), IsHorizontal = isHorizontal });
                    }

                    currentId = -1;
                    currentLine.Clear();
                }
            }

            if (currentLine.Count >= 3)
            {
                lines.Add(new MatchLine { ItemID = currentId, Cells = new List<Vector2Int>(currentLine), IsHorizontal = isHorizontal });
            }
        }

        return lines;
    }

    // Xu ly cac cap doan ngang va doc giao nhau cung mau de tao TNT (chu T, L, +)
    private void ProcessIntersectingMatches(List<MatchLine> horizontalLines, List<MatchLine> verticalLines,
        HashSet<int> usedHorizontals, HashSet<int> usedVerticals, Vector2Int? priorityCenter, List<MatchData> resultMatches)
    {
        for (int h = 0; h < horizontalLines.Count; h++)
        {
            if (usedHorizontals.Contains(h)) continue;

            for (int v = 0; v < verticalLines.Count; v++)
            {
                if (usedVerticals.Contains(v)) continue;
                if (horizontalLines[h].ItemID != verticalLines[v].ItemID) continue;

                var intersection = horizontalLines[h].Cells.Intersect(verticalLines[v].Cells).ToList();
                if (intersection.Count > 0)
                {
                    usedHorizontals.Add(h);
                    usedVerticals.Add(v);

                    var tntCells = new HashSet<Vector2Int>(horizontalLines[h].Cells);
                    tntCells.UnionWith(verticalLines[v].Cells);

                    // Uu tien o swipe cua nguoi choi neu nam trong cum, hoac lay diem giao nhau
                    Vector2Int center = (priorityCenter.HasValue && tntCells.Contains(priorityCenter.Value))
                        ? priorityCenter.Value
                        : intersection[0];

                    resultMatches.Add(new MatchData
                    {
                        matchID = horizontalLines[h].ItemID,
                        MatchType = MatchType.TNT,
                        centerCell = center,
                        Matches = tntCells.ToList()
                    });

                    break;
                }
            }
        }
    }

    // Xu ly cac doan thang doc lap khong giao nhau (LightBall >= 5, Rocket == 4, Normal == 3)
    private void ProcessIndependentMatches(List<MatchLine> lines, HashSet<int> usedIndices,
        Vector2Int? priorityCenter, List<MatchData> resultMatches)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (usedIndices.Contains(i)) continue;

            var line = lines[i];
            MatchType type = GetLineMatchType(line.Cells.Count, line.IsHorizontal);
            resultMatches.Add(CreateMatchData(line.Cells, line.ItemID, type, priorityCenter));
        }
    }

    // Xac dinh loai match dua tren do dai doan thang
    private MatchType GetLineMatchType(int cellCount, bool isHorizontal)
    {
        if (cellCount >= 5) return MatchType.LightBall;
        if (cellCount == 4) return isHorizontal ? MatchType.VerticalRocket : MatchType.HorizontalRocket;
        return MatchType.Normal;
    }

    // Quet cac khoi o vuong 2x2 tao Missile (bo qua cac o da tham gia vao match truoc do)
    private void ScanSquareMatches(int width, int height, Vector2Int? priorityCenter, List<MatchData> resultMatches)
    {
        var matchedCellsSet = new HashSet<Vector2Int>();
        foreach (var match in resultMatches)
        {
            matchedCellsSet.UnionWith(match.Matches);
        }

        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                Vector2Int p1 = new Vector2Int(x, y);
                Vector2Int p2 = new Vector2Int(x + 1, y);
                Vector2Int p3 = new Vector2Int(x, y + 1);
                Vector2Int p4 = new Vector2Int(x + 1, y + 1);

                if (matchedCellsSet.Contains(p1) || matchedCellsSet.Contains(p2) ||
                    matchedCellsSet.Contains(p3) || matchedCellsSet.Contains(p4))
                {
                    continue;
                }

                if (IsCellMatchable(p1, out int id1) &&
                    IsCellMatchable(p2, out int id2) && id1 == id2 &&
                    IsCellMatchable(p3, out int id3) && id1 == id3 &&
                    IsCellMatchable(p4, out int id4) && id1 == id4)
                {
                    var squareCells = new List<Vector2Int> { p1, p2, p3, p4 };
                    resultMatches.Add(CreateMatchData(squareCells, id1, MatchType.Missile, priorityCenter));
                    matchedCellsSet.UnionWith(squareCells);
                }
            }
        }
    }

    // Tao MatchData va xac dinh tam tao booster centerCell
    private MatchData CreateMatchData(List<Vector2Int> cells, int itemId, MatchType type, Vector2Int? priorityCenter)
    {
        Vector2Int center = (priorityCenter.HasValue && cells.Contains(priorityCenter.Value))
            ? priorityCenter.Value
            : (cells.Count > 0 ? cells[0] : Vector2Int.zero);

        return new MatchData
        {
            matchID = itemId,
            MatchType = type,
            centerCell = center,
            Matches = cells
        };
    }

    // Kiem tra o co hop le de tham gia Match hay khong
    private bool IsCellMatchable(Vector2Int pos, out int itemId)
    {
        itemId = -1;
        if (_board == null || !_board.IsInBounds(pos)) return false;

        GameObject cellObj = _board.NormalGrid[pos.x, pos.y];
        if (cellObj == null) return false;

        if (!cellObj.TryGetComponent<IBoardItem>(out var item)) return false;
        if (!BoardItemUtils.IsValidNormalItem(item)) return false;

        itemId = (int)item.ItemId;
        return true;
    }
}
