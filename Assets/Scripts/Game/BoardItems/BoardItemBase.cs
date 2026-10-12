using DG.Tweening;
using UnityEngine;

// Lop co so truu tuong cho moi item tren ban co match-3
public abstract class BoardItemBase : MonoBehaviour, IBoardItem
{
    [SerializeField] protected EnumItemBoard itemId;

    public EnumItemBoard ItemId
    {
        get => itemId;
        set => itemId = value;
    }

    // Hook khoi tao khi lay item tu pool
    public virtual void OnSpawn()
    {
    }

    // Hook thu hoi item ve pool, huy tween va reset transform ve gia tri mac dinh
    public virtual void OnDespawn()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}
