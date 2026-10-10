using System;
using OdinSerializer;
using UnityEngine;

// Dinh nghia mot muc tieu thu thap hoac pha huy trong man choi
[Serializable]
public class GoalEntry
{
    // Loai vat pham can thu thap hoac pha huy (hien thi Dropdown trong Unity Inspector)
    [Tooltip("Loại item mục tiêu cần thu thập hoặc phá hủy")]
    [OdinSerialize, SerializeField] private EnumItemBoard itemType = EnumItemBoard.Red;

    // So luong yeu cau de hoan thanh muc tieu
    [Tooltip("Số lượng yêu cầu cần hoàn thành")]
    [OdinSerialize, SerializeField] private int amount = 10;

    public GoalEntry()
    {
        itemType = EnumItemBoard.Red;
        amount = 10;
    }

    public GoalEntry(int itemId, int amount)
    {
        this.itemType = (EnumItemBoard)itemId;
        this.amount = Mathf.Max(0, amount);
    }

    public GoalEntry(EnumItemBoard itemType, int amount)
    {
        this.itemType = itemType;
        this.amount = Mathf.Max(0, amount);
    }

    public int ItemId
    {
        get => (int)itemType;
        set => itemType = (EnumItemBoard)value;
    }

    public EnumItemBoard ItemType
    {
        get => itemType;
        set => itemType = value;
    }

    public int Amount
    {
        get => amount;
        set => amount = Mathf.Max(0, value);
    }
}
