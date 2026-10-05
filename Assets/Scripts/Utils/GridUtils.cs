using UnityEngine;

// Tien ich chuyen doi toa do giua Ma tran mang 2 chieu (Row, Col), O Grid (CellPos) va Toa do thuc (WorldPos).
// Goc toa do (0, 0) cua Grid duoc dat co dinh o goc duoi-trai cua ban co:
// - Cot (Col): Chay tu trai qua phai (0 -> cols - 1), tuong ung truc X tang dan.
// - Hang (Row): Hang 0 o tren cung, hang (rows - 1) o duoi cung, tuong ung truc Y giam dan.
public static class GridUtils
{
    // 1. Chuyen doi qua lai giua Cell va World qua Grid cua Unity

    // Lay toa do tam o trong khong gian World tu toa do Cell
    public static Vector3 CellToWorld(Grid grid, Vector3Int cellPos)
    {
        if (grid != null)
        {
            return grid.GetCellCenterWorld(cellPos);
        }
        return (Vector3)cellPos;
    }

    // Lay toa do Cell tren Grid tu vi tri World bat ky
    public static Vector3Int WorldToCell(Grid grid, Vector3 worldPos)
    {
        if (grid != null)
        {
            return grid.WorldToCell(worldPos);
        }
        return new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
    }

    // 2. Chuyen doi qua lai giua Ma tran (Row, Col) va Cell

    // Chuyen vi tri (row, col) trong mang thanh toa do Cell (x, y)
    // x = cot c (tu trai qua phai)
    // y = rows - 1 - r (vi hang 0 o tren dinh, con truc Y cua Unity huong len tren)
    public static Vector3Int MatrixToCell(int row, int col, int rows)
    {
        int cellX = col;
        int cellY = rows - 1 - row;
        return new Vector3Int(cellX, cellY, 0);
    }

    // Chuyen toa do Cell (x, y) nguoc lai thanh vi tri (row, col) trong ma tran
    // Tra ve true neu vi tri nam hop le trong pham vi ban co
    public static bool CellToMatrix(Vector3Int cellPos, int rows, int cols, out int row, out int col)
    {
        col = cellPos.x;
        row = rows - 1 - cellPos.y;

        return col >= 0 && col < cols && row >= 0 && row < rows;
    }

    // 3. Chuyen doi truc tiep tu Ma tran (Row, Col) ra vi tri World

    // Lay thang toa do World (tam o) cho phan tu o hang row, cot col
    public static Vector3 RowColToWorld(Grid grid, int row, int col, int rows)
    {
        Vector3Int cellPos = MatrixToCell(row, col, rows);
        return CellToWorld(grid, cellPos);
    }

    // Chuyen vi tri click chuot hoac cham (World) thanh vi tri (row, col) cua ma tran
    public static bool WorldToRowCol(Grid grid, Vector3 worldPos, int rows, int cols, out int row, out int col)
    {
        Vector3Int cellPos = WorldToCell(grid, worldPos);
        return CellToMatrix(cellPos, rows, cols, out row, out col);
    }
}
