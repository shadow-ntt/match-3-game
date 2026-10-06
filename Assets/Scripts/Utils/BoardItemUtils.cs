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

        // Kiểm tra item có đủ điều kiện hoán đổi (không phải Blank và thuộc tầng Normal)
        public static bool CanSwap(IBoardItem item)
        {
            return item != null && !IsBlank(item) && IsValidNormalItem(item);
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
    }
}
