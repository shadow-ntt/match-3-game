using Cysharp.Threading.Tasks;

// Giao dien danh cho cac item co the tham gia ghep cum match-3
public interface IMatchable
{
    // Dinh danh mau de kiem tra match
    EnumItemBoard ColorId { get; }

    // Co the tham gia match hay khong
    bool CanMatch { get; }

    // Hoat anh phan hoi khi nam trong cum match duoc kich no
    UniTask OnMatchedAsync();
}
