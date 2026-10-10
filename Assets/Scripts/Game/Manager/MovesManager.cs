using UnityEngine;
using Utils;

// Quan ly so luot di chuyen con lai cua man choi
public class MovesManager : Singleton<MovesManager>
{
    private int _movesLeft;
    private int _movesTotal;
    private bool _isLevelEnded;

    public int MovesLeft => _movesLeft;
    public int MovesTotal => _movesTotal;
    public bool HasMovesLeft => _movesLeft > 0;
    public bool IsLevelEnded => _isLevelEnded;

    // Khoi tao so luot di cho man choi
    public void Initialize(int totalMoves)
    {
        _movesTotal = Mathf.Max(0, totalMoves);
        _movesLeft = _movesTotal;
        _isLevelEnded = false;
        GameEventBus.RaiseMovesChanged(_movesLeft, _movesTotal);
    }

    // Tru 1 luot di khi nguoi choi hoan doi thanh cong
    public void ConsumeMove()
    {
        if (_isLevelEnded || _movesLeft <= 0) return;

        _movesLeft--;
        GameEventBus.RaiseMovesChanged(_movesLeft, _movesTotal);
    }

    // Cong them luot di (item bo tro hoac thuong)
    public void AddBonusMoves(int amount)
    {
        if (amount <= 0 || _isLevelEnded) return;

        _movesLeft += amount;
        GameEventBus.RaiseMovesChanged(_movesLeft, _movesTotal);
    }

    // Danh dau ket thuc man choi de chan tiep tuc tru luot
    public void SetLevelEnded(bool ended)
    {
        _isLevelEnded = ended;
    }
}
