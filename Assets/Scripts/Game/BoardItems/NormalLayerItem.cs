using UnityEngine;

public class NormalLayerItem : MonoBehaviour, IBoardItem
{
    [SerializeField] private EnumItemBoard itemId;
    public EnumItemBoard ItemId
    {
        get => itemId;
        set => itemId = value;
    }

    public virtual void OnSpawn()
    {
        // Hook called when retrieved from pool
    }

    public virtual void OnDespawn()
    {
        // Hook called before returning to pool
    }
}
