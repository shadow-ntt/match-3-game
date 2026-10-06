using UnityEngine;

public class BoardCell : MonoBehaviour, IBoardItem
{
    [SerializeField] private EnumItemBoard itemId;
    [SerializeField] private EnumStateBoardCell state = EnumStateBoardCell.Occupied;

    private int x;
    private int y;

    public EnumItemBoard ItemId
    {
        get => itemId;
        set => itemId = value;
    }

    public EnumStateBoardCell State
    {
        get => state;
        set => state = value;
    }

    // Kiem tra o co dang trong khong co item hay khong
    public bool IsEmpty => state == EnumStateBoardCell.Empty;

    public int X
    {
        get => x;
        set => x = value;
    }

    public int Y
    {
        get => y;
        set => y = value;
    }

    public Vector2Int GridPos => new Vector2Int(x, y);

    public int Col
    {
        get => x;
        set => x = value;
    }

    public int Row
    {
        get => y;
        set => y = value;
    }

    public virtual void OnSpawn()
    {
        state = EnumStateBoardCell.Occupied;
    }

    public virtual void OnDespawn()
    {
        state = EnumStateBoardCell.Empty;
    }
}
