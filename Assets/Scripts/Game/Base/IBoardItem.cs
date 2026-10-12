using UnityEngine;

// Interface chung cho cac item tren ban co
public interface IBoardItem
{
    EnumItemBoard ItemId { get; set; }
    Transform transform { get; }
    GameObject gameObject { get; }
    void OnSpawn();
    void OnDespawn();
}
