using UnityEngine;

public class BoardCell : MonoBehaviour, IBoardItem
{
    [SerializeField] private EnumItemBoard itemId;
    [SerializeField] private EnumStateBoardCell state = EnumStateBoardCell.Occupied;

    private int x;
    private int y;
    private bool _isGettingFilled;

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

    public void SetState(EnumStateBoardCell newState)
    {
        state = newState;
    }

    // Danh dau o da duoc dat cho boi item dang roi den de tranh ngoc khac chiem
    public bool IsGettingFilled
    {
        get => _isGettingFilled;
        set => _isGettingFilled = value;
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
        _isGettingFilled = false;
    }

    public virtual void OnDespawn()
    {
        state = EnumStateBoardCell.Empty;
        _isGettingFilled = false;
    }
}
