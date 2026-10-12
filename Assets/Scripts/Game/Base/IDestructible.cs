using Cysharp.Threading.Tasks;

// Giao dien danh cho cac item co the bi pha huy hoac nhan sat thuong tren ban co
public interface IDestructible
{
    // Luong mau hien tai va mau toi da cua vat the
    int CurrentHp { get; }
    int MaxHp { get; }

    // Trang thai da bi tieu diet hoan toan hay chua
    bool IsDestroyed { get; }

    // Nhan sat thuong, tra ve true neu item bi tieu diet hoan toan (Hp <= 0)
    UniTask<bool> TakeDamageAsync(int damage = 1);

    // Phat hoat anh bien mat va hieu ung hat VFX khi bi tieu diet
    UniTask PlayDestroyEffectAsync();
}
