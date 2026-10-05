// Interface chung cho cac item tren ban co
public interface IBoardItem
{
    EnumItemBoard ItemId { get; set; }
    void OnSpawn();
    void OnDespawn();
}
