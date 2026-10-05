using UnityEngine;
using UnityEngine.Tilemaps;

// Custom Tile chua idItem (dung enum EnumItemBoard), co the tao truc tiep bang Create Asset Menu
[CreateAssetMenu(fileName = "NewTileItem", menuName = "Tiles/TileItem")]
public class TileItem : Tile
{
    // Id cua item tren ban co (dung enum EnumItemBoard)\
    [Header("Cấu hình Loại Item")]
    [Tooltip("Chọn loại item tương ứng trên bàn cờ từ EnumItemBoard")]
    public EnumItemBoard idItem = EnumItemBoard.Red;

    // Lay id so nguyen tuong ung
    public int ItemId => (int)idItem;
}
