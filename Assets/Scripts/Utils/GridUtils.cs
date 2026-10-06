using UnityEngine;

namespace Utils
{
    // Tien ich chuyen doi toa do giua he toa do Ban co (X, Y) va toa do World.
    // X: Cot (0 o ben trai, tang dan sang phai).
    // Y: Hang (0 o duoi cung, tang dan len tren).
    public static class GridUtils
    {
        // Chuyen doi tu toa do Grid (x, y) sang toa do World (tam o)
        public static Vector3 GridToWorld(Grid grid, int x, int y)
        {
            if (grid != null)
            {
                return grid.GetCellCenterWorld(new Vector3Int(x, y, 0));
            }
            return new Vector3(x, y, 0);
        }

        public static Vector3 GridToWorld(Grid grid, Vector2Int pos)
        {
            return GridToWorld(grid, pos.x, pos.y);
        }

        // Chuyen doi tu toa do World sang toa do Grid (x, y)
        public static Vector2Int WorldToGrid(Grid grid, Vector3 worldPos)
        {
            if (grid != null)
            {
                Vector3Int cellPos = grid.WorldToCell(worldPos);
                return new Vector2Int(cellPos.x, cellPos.y);
            }
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        // Chuyen doi vi tri click/cham (World) sang toa do Grid (x, y) co kiem tra gioi han
        public static bool WorldToGrid(Grid grid, Vector3 worldPos, int width, int height, out int x, out int y)
        {
            Vector2Int pos = WorldToGrid(grid, worldPos);
            x = pos.x;
            y = pos.y;
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
