// Giao dien danh cho cac item co the hoan doi vi tri bang thao tac vuot
public interface ISwappable
{
    // Kiem tra item co cho phep nguoi choi vuot de hoan doi vi tri hay khong
    bool CanSwap { get; }
}
