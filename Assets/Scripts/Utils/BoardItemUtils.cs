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
            return item != null && IsValidNormalItem(item.ItemId);
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

        // Kiểm tra vật cản (Ground: 200 hoặc vật cản tầng 3 Under)
        public static bool IsObstacle(int id)
        {
            return id == (int)EnumItemBoard.Stone;
        }

        public static bool IsObstacle(EnumItemBoard itemBoard)
        {
            return itemBoard == EnumItemBoard.Stone;
        }

        public static bool IsObstacle(IBoardItem item)
        {
            return item != null && (item.ItemId == EnumItemBoard.Stone || item is UnderLayerItem);
        }

        public static bool IsObstacle(GameObject obj)
        {
            if (obj == null) return false;
            if (obj.TryGetComponent<IBoardItem>(out var item))
            {
                return IsObstacle(item);
            }
            return false;
        }

        // Kiểm tra item có đủ điều kiện hoán đổi (không phải Blank, không phải vật cản và thuộc tầng Normal)
        public static bool CanSwap(IBoardItem item)
        {
            return item != null && !IsBlank(item) && !IsObstacle(item) && IsValidNormalItem(item);
        }

        // Kiểm tra 2 item có đủ điều kiện hoán đổi cho nhau không (đều hợp lệ và khác ItemId)
        public static bool CanSwap(IBoardItem itemA, IBoardItem itemB)
        {
            return CanSwap(itemA) && CanSwap(itemB) && itemA.ItemId != itemB.ItemId;
        }

        public static bool CanSwap(int id)
        {
            return !IsBlank(id) && IsValidNormalItem(id);
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

        // Kiểm tra ô (x, y) có hợp lệ, đang trống và sẵn sàng nhận ngọc rơi hoặc sinh từ trên trời không
        public static bool IsCellAvailableForFill(this Board board, int x, int y)
        {
            if (board == null || !board.IsInBounds(x, y)) return false;
            if (board.BoardCellGrid == null || board.BoardCellGrid[x, y] == null) return false;

            // Nếu là điểm sinh (Spawn) thì không chứa ngọc thường
            if (board.BoardCellGrid[x, y].TryGetComponent<IBoardItem>(out var boardItem) && boardItem.ItemId == EnumItemBoard.Spawn) return false;

            // Tầng 4 (Overlay): Nếu có vật cản che phủ (băng, xích...) thì không thể lấp vào
            if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null) return false;

            // Tầng 3 (Under): Nếu có vật cản tầng dưới (hộp gỗ, đá...) thì không thể lấp vào
            if (board.UnderGrid != null && board.UnderGrid[x, y] != null) return false;

            // Tầng 2 (Normal): Nếu ô đã có ngọc thì không thể lấp vào
            if (board.NormalGrid != null && board.NormalGrid[x, y] != null) return false;

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
