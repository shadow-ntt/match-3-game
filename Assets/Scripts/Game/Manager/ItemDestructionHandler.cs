using Cysharp.Threading.Tasks;
using UnityEngine;

// Xu ly tap trung viec gay sat thuong, kich no hoat anh va tra ve Pool cho cac item tren ban co
public static class ItemDestructionHandler
{
    // 1. Pha huy Co / UnderLayer (khi ngoc de tren no no)
    public static bool DestroyUnderItem(Board board, int x, int y, bool isByBooster = false)
    {
        if (board == null || !board.IsInBounds(x, y) || board.UnderGrid == null) return false;
        GameObject obj = board.UnderGrid[x, y];
        if (obj == null || obj.TryGetComponent<SpawnItem>(out _)) return false;

        return ProcessDestruction(board, obj, x, y, EnumBoardLayer.Under, isByBooster);
    }

    // 2. Kich no 1 o do Booster can quet (tac dong len ca Overlay, Mid va Under)
    public static bool ExplodeByBooster(Board board, int x, int y)
    {
        if (board == null || !board.IsInBounds(x, y)) return false;
        bool destroyedAny = false;

        if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null)
            destroyedAny |= ProcessDestruction(board, board.OverlayGrid[x, y], x, y, EnumBoardLayer.Overlay, isByBooster: true);

        // Khong pha huy Booster o tang Mid de danh cho BoosterActivationManager kich no day chuyen
        if (board.MidGrid != null && board.MidGrid[x, y] != null && !board.MidGrid[x, y].TryGetComponent<IBoosterActivatable>(out _))
            destroyedAny |= ProcessDestruction(board, board.MidGrid[x, y], x, y, EnumBoardLayer.Mid, isByBooster: true);

        destroyedAny |= DestroyUnderItem(board, x, y, isByBooster: true);

        return destroyedAny;
    }

    // 3. Pha huy vat can o ria ke ben cum match (CHI pha ObstacleItem)
    public static bool ExplodeAdjacentObstacle(Board board, int x, int y)
    {
        if (board == null || !board.IsInBounds(x, y)) return false;

        if (board.OverlayGrid != null && board.OverlayGrid[x, y] != null && board.OverlayGrid[x, y].TryGetComponent<ObstacleItem>(out _))
            return ProcessDestruction(board, board.OverlayGrid[x, y], x, y, EnumBoardLayer.Overlay, isByBooster: false);

        if (board.MidGrid != null && board.MidGrid[x, y] != null && board.MidGrid[x, y].TryGetComponent<ObstacleItem>(out _))
            return ProcessDestruction(board, board.MidGrid[x, y], x, y, EnumBoardLayer.Mid, isByBooster: false);

        if (board.UnderGrid != null && board.UnderGrid[x, y] != null && board.UnderGrid[x, y].TryGetComponent<ObstacleItem>(out _))
            return ProcessDestruction(board, board.UnderGrid[x, y], x, y, EnumBoardLayer.Under, isByBooster: false);

        return false;
    }

    // Overload cu giu lai de tuong thich neu con noi nao goi
    public static bool DestroyCell(Board board, int x, int y, ScoreManager sm = null, GoalTracker gt = null, Pooltem pool = null, bool isByBooster = false)
    {
        return isByBooster ? ExplodeByBooster(board, x, y) : ExplodeAdjacentObstacle(board, x, y);
    }

    // Ham loi: gay sat thuong -> Board giai phong grid -> ban Event -> chay VFX & tra ve Pool
    private static bool ProcessDestruction(Board board, GameObject obj, int x, int y, EnumBoardLayer layer, bool isByBooster)
    {
        var destructible = obj.GetComponent<IDestructible>();
        if (destructible != null)
        {
            destructible.TakeDamageAsync(1).Forget();
            if (!destructible.IsDestroyed) return true; // Con mau thi chua pha huy
        }

        // 1. Thong bao Board giai phong o trong Grid va cap nhat State cua BoardCell
        GameObject removedObj = board.ClearGridItem(x, y, layer);
        if (removedObj == null) removedObj = obj;

        // 2. Ban su kien de ScoreManager, GoalTracker tu dong cap nhat
        EnumItemBoard itemId = removedObj.TryGetComponent<IBoardItem>(out var item) ? item.ItemId : EnumItemBoard.Blank;
        bool isObstacle = removedObj.TryGetComponent<ObstacleItem>(out _);
        GameEventBus.RaiseItemDestroyed(itemId, isObstacle, isByBooster);

        // 3. Phat VFX bien mat va thu hoi ve Pooltem
        if (destructible != null)
        {
            PlayEffectAndReturn(destructible, removedObj).Forget();
        }
        else
        {
            Pooltem.Instance.ReturnBoardItem(removedObj);
        }

        return true;
    }

    // Phat hoat anh pha huy roi moi thu hoi ve pool
    private static async UniTaskVoid PlayEffectAndReturn(IDestructible destructible, GameObject obj)
    {
        await destructible.PlayDestroyEffectAsync();
        Pooltem.Instance.ReturnBoardItem(obj);
    }
}
