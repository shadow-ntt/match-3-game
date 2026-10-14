using UnityEngine;

namespace Utils
{
    // Tiện ích kiểm tra và xử lý các loại Item trên bàn cờ Match-3
    public static class BoardItemUtils
    {
        // Kiểm tra ô nền bàn cờ hợp lệ (Board: 0, Spawn: 1)
        public static bool IsValidBoardCell(int id)
        {
            return id == (int)EnumItemBoard.Board || id == (int)EnumItemBoard.Spawn;
        }

        public static bool IsValidBoardCell(EnumItemBoard itemBoard)
        {
            return itemBoard == EnumItemBoard.Board || itemBoard == EnumItemBoard.Spawn;
        }

        // Kiểm tra ngọc thông thường bằng switch-case (Red: 102 ... Pink: 108)
        public static bool IsValidNormalItem(int id)
        {
            switch ((EnumItemBoard)id)
            {
                case EnumItemBoard.Red:
                case EnumItemBoard.Blue:
                case EnumItemBoard.Green:
                case EnumItemBoard.Yellow:
                case EnumItemBoard.Purple:
                case EnumItemBoard.Orange:
                case EnumItemBoard.Pink:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidNormalItem(EnumItemBoard itemBoard)
        {
            return IsValidNormalItem((int)itemBoard);
        }

        public static bool IsValidNormalItem(IBoardItem item)
        {
            return item is IMatchable matchable && matchable.CanMatch;
        }

        // Kiem tra item co phai la Booster khong (301-305)
        public static bool IsBoosterItem(int id)
        {
            return id >= (int)EnumItemBoard.HorizontalRocket && id <= (int)EnumItemBoard.LightBall;
        }

        public static bool IsBoosterItem(EnumItemBoard itemBoard)
        {
            return IsBoosterItem((int)itemBoard);
        }

        public static bool IsBoosterItem(IBoardItem item)
        {
            return item is IBoosterActivatable;
        }

        // Kiem tra item co thuoc tang Mid Layer khong (Normal Item: 102..108 hoac Booster: 301..305)
        public static bool IsMidLayer(int id)
        {
            return IsValidNormalItem(id) || IsBoosterItem(id);
        }

        public static bool IsMidLayer(EnumItemBoard itemBoard)
        {
            return IsMidLayer((int)itemBoard);
        }

        public static bool IsMidLayer(IBoardItem item)
        {
            return item != null && (item is IMatchable || item is IBoosterActivatable || IsMidLayer(item.ItemId));
        }

        // Kiểm tra item tầng Under hợp lệ
        public static bool IsValidUnderItem(int id)
        {
            return id != (int)EnumItemBoard.Blank && id > 0;
        }

        // Kiểm tra item tầng Overlay hợp lệ
        public static bool IsValidOverlayItem(int id)
        {
            return id != (int)EnumItemBoard.Blank && id > 0;
        }

        // Kiểm tra ô trống (Blank: -1)
        public static bool IsBlank(int id)
        {
            return id == (int)EnumItemBoard.Blank;
        }

        public static bool IsBlank(IBoardItem item)
        {
            return item == null || item.ItemId == EnumItemBoard.Blank;
        }

        // Kiểm tra vật cản (Stone: 301 hoặc vật cản tầng Overlay/Obstacle: 300..399)
        public static bool IsObstacle(int id)
        {
            return id == (int)EnumItemBoard.Stone || (id >= 300 && id < 400);
        }

        public static bool IsObstacle(EnumItemBoard itemBoard)
        {
            return IsObstacle((int)itemBoard);
        }

        // Kiểm tra item thuộc tầng UnderLayer (Grass: 201...)
        public static bool IsUnderLayerItem(int id)
        {
            return id == (int)EnumItemBoard.Grass || (id >= 200 && id < 300);
        }

        public static bool IsUnderLayerItem(IBoardItem item)
        {
            return item is UnderLayerItem;
        }

        public static bool IsUnderLayerItem(GameObject obj)
        {
            return obj != null && obj.TryGetComponent<UnderLayerItem>(out _);
        }

        public static bool IsObstacle(IBoardItem item)
        {
            return item is ObstacleItem;
        }

        public static bool IsObstacle(GameObject obj)
        {
            return obj != null && obj.TryGetComponent<ObstacleItem>(out _);
        }

        // Kiểm tra tọa độ (x, y) có chứa vật cản ở bất kỳ layer nào không
        public static bool HasObstacleAt(Board board, int x, int y)
        {
            if (board == null || !board.IsInBounds(x, y)) return false;
            if (board.OverlayGrid != null && IsObstacle(board.OverlayGrid[x, y])) return true;
            if (board.UnderGrid != null && IsObstacle(board.UnderGrid[x, y])) return true;
            if (board.MidGrid != null && IsObstacle(board.MidGrid[x, y])) return true;
            return false;
        }

        // Kiểm tra tọa độ (x, y) có item tầng Overlay che phủ không
        public static bool HasOverlay(this Board board, int x, int y)
        {
            return board != null && board.IsInBounds(x, y) && board.OverlayGrid != null && board.OverlayGrid[x, y] != null;
        }

        public static bool HasOverlay(this Board board, Vector2Int pos)
        {
            return board.HasOverlay(pos.x, pos.y);
        }

        // Kiem tra item co du dieu kien hoan doi (qua interface ISwappable)
        public static bool CanSwap(IBoardItem item)
        {
            return item is ISwappable swappable && swappable.CanSwap;
        }

        // Kiem tra 2 item co du dieu kien hoan doi cho nhau khong
        public static bool CanSwap(IBoardItem itemA, IBoardItem itemB)
        {
            if (!CanSwap(itemA) || !CanSwap(itemB)) return false;
            if (itemA is IBoosterActivatable && itemB is IBoosterActivatable) return true;
            return itemA.ItemId != itemB.ItemId;
        }

        public static bool CanSwap(int id)
        {
            return !IsBlank(id) && IsMidLayer(id);
        }

        // Kiem tra 2 o (xA, yA) va (xB, yB) tren ban co co du dieu kien hoan doi khong (bao gom kiem tra Overlayer)
        public static bool CanSwap(this Board board, int xA, int yA, int xB, int yB)
        {
            if (board == null || !board.IsInBounds(xA, yA) || !board.IsInBounds(xB, yB)) return false;
            if (board.HasOverlay(xA, yA) || board.HasOverlay(xB, yB)) return false;
            if (board.MidGrid == null) return false;

            GameObject objA = board.MidGrid[xA, yA];
            GameObject objB = board.MidGrid[xB, yB];
            if (objA == null || objB == null) return false;

            IBoardItem itemA = objA.GetComponent<IBoardItem>();
            IBoardItem itemB = objB.GetComponent<IBoardItem>();
            return CanSwap(itemA, itemB);
        }

        // ==========================================
        // TIỆN ÍCH TRUY VẤN VÀ THAO TÁC BOARD CELL
        // ==========================================

        // Lấy BoardCell tại tọa độ (x, y) một cách an toàn
        public static BoardCell GetBoardCell(this Board board, int x, int y)
        {
            if (board == null || !board.IsInBounds(x, y) || board.BoardCellGrid == null) return null;
            GameObject cellObj = board.BoardCellGrid[x, y];
            return cellObj != null ? cellObj.GetComponent<BoardCell>() : null;
        }

        // Lấy tọa độ Y cao nhất có BoardCell hợp lệ của cột x
        public static int GetTopY(this Board board, int x)
        {
            if (board == null || board.BoardCellGrid == null || !board.IsInBounds(x, 0)) return -1;
            for (int y = board.Height - 1; y >= 0; y--)
            {
                if (board.BoardCellGrid[x, y] != null) return y;
            }
            return -1;
        }

        // Kiểm tra ô (x, y) có phải là điểm sinh (Spawn) không
        public static bool IsSpawnerCell(this Board board, int x, int y)
        {
            if (board == null || !board.IsInBounds(x, y)) return false;
            if (board.UnderGrid != null && board.UnderGrid[x, y] != null && board.UnderGrid[x, y].TryGetComponent<SpawnItem>(out _)) return true;
            if (board.BoardCellGrid != null && board.BoardCellGrid[x, y] != null && board.BoardCellGrid[x, y].TryGetComponent<SpawnItem>(out _)) return true;
            return false;
        }

        // Kiểm tra ô (x, y) có hợp lệ, đang trống và sẵn sàng nhận ngọc rơi hoặc sinh từ trên trời không
        public static bool IsCellAvailableForFill(this Board board, int x, int y)
        {
            if (board == null || !board.IsInBounds(x, y)) return false;
            if (board.BoardCellGrid == null || board.BoardCellGrid[x, y] == null) return false;

            // Nếu là điểm sinh (Spawn) thì không chứa ngọc thường
            if (board.IsSpawnerCell(x, y)) return false;

            // Tầng 4 (Overlay): Nếu có vật cản che phủ (băng, xích...) thì không thể lấp vào
            if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null) return false;

            // Tầng 3 (Under): Nếu ô chứa vật cản chặn đường thì không thể lấp vào (UnderLayerItem như cỏ không chặn)
            if (board.UnderGrid != null && IsObstacle(board.UnderGrid[x, y])) return false;

            // Tầng 2 (Mid): Nếu ô đã có item thì không thể lấp vào
            if (board.MidGrid != null && board.MidGrid[x, y] != null) return false;

            // Trạng thái ô: Nếu đang được ngọc khác rơi tới hoặc đang spawn lấp vào
            BoardCell cell = board.GetBoardCell(x, y);
            if (cell != null && cell.IsGettingFilled) return false;

            return true;
        }

        // Tính thời lượng rơi chuẩn hóa theo khoảng cách ô (grid distance)
        public static float CalculateFallDuration(float baseDuration, float distanceInCells)
        {
            return baseDuration * Mathf.Sqrt(Mathf.Max(1f, distanceInCells));
        }
    }
}
