using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Quan ly kich hoat hieu ung va no day chuyen cac Booster tren ban co
public class BoosterActivationManager : Singleton<BoosterActivationManager>
{
    [SerializeField] private Board board;
    [SerializeField] private PoolBlockBreakEffect poolParticle;
    [SerializeField] private float explosionDelay = 0.25f;

    private BoosterEffectPlayer _effectPlayer;
    private BoosterCombos _combos;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;
    public BoosterEffectPlayer EffectPlayer => _effectPlayer;
    public BoosterCombos Combos => _combos;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
        if (poolParticle == null) poolParticle = FindAnyObjectByType<PoolBlockBreakEffect>();

        _combos = new BoosterCombos();
        _effectPlayer = new BoosterEffectPlayer(this);
    }

    // Kich hoat booster tai (x, y), ho tro kich hoat day chuyen (Chain Reaction)
    // Tra ve true neu booster kich hoat thanh cong
    public async UniTask<bool> ActivateBoosterAsync(int x, int y, IBoardItem swapTarget = null)
    {
        var visited = new HashSet<Vector2Int>();
        return await ActivateBoosterInternalAsync(x, y, swapTarget, visited, 0);
    }

    // Kich hoat hieu ung combo giua 2 booster tai (xA, yA) va (xB, yB), ho tro day chuyen
    public async UniTask<bool> ActivateBoosterComboAsync(int xA, int yA, IBoardItem boosterA, int xB, int yB, IBoardItem boosterB)
    {
        if (board == null || board.MidGrid == null) return false;
        if (boosterA == null || boosterB == null) return false;

        var visited = new HashSet<Vector2Int>
        {
            new Vector2Int(xA, yA),
            new Vector2Int(xB, yB)
        };

        // Tam combo lay tai vi tri vuot den (xB, yB)
        int cx = xB;
        int cy = yB;

        // Tim Combo tu BoosterCombos
        var combo = _combos != null
            ? _combos.GetCombo(boosterA.ItemId, boosterB.ItemId)
            : null;

        if (combo == null) return false;

        // Lay danh sach cac o bi anh huong boi combo
        var affectedCells = combo.GetAffectedCells(board, cx, cy, boosterA.ItemId, boosterB.ItemId);

        GameObject objA = board.MidGrid[xA, yA];
        GameObject objB = board.MidGrid[xB, yB];
        Vector3 centerPos = objB != null ? objB.transform.position : GridUtils.GridToWorld(board.Grid, cx, cy);

        // Phat animation hop nhat combo truoc khi xoa
        await combo.PlayMergeAnimationAsync(objA, objB, centerPos);

        // Rung camera neu combo co cau hinh
        var shake = CameraShakeService.Instance;
        if (combo.ShakeType == CameraShakeType.Mega)
        {
            shake?.ShakeMega().Forget();
        }
        else if (combo.ShakeType == CameraShakeType.TNT)
        {
            shake?.ShakeTNT().Forget();
        }

        // Xoa 2 vien booster tham gia combo truoc
        RemoveItem(xA, yA);
        RemoveItem(xB, yB);

        // Gom cac booster khac nam trong vung no combo de kich hoat day chuyen
        var chainBoosters = CollectChainBoosters(affectedCells, visited);

        // Dong goi ngu canh va thuc thi hieu ung combo
        var context = new BoosterComboContext(
            board,
            this,
            cx, cy,
            xA, yA, xB, yB,
            boosterA.ItemId, boosterB.ItemId,
            affectedCells,
            chainBoosters,
            visited
        );

        await combo.ExecuteEffectAsync(context);

        // Kich hoat day chuyen cac booster nam trong vung no combo
        await ActivateChainBoostersAsync(chainBoosters, visited, 1);

        return true;
    }

    // Kich hoat day chuyen cac booster tu danh sach toa do duoc truyen vao
    public async UniTask TriggerChainBoostersAsync(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0 || board == null || board.MidGrid == null) return;

        var visited = new HashSet<Vector2Int>();
        var chainBoosters = CollectChainBoosters(cells, visited);
        await ActivateChainBoostersAsync(chainBoosters, visited, 0);
    }

    // Ham noi bo thuc hien kich hoat booster va de quy no day chuyen
    public async UniTask<bool> ActivateBoosterInternalAsync(
        int x, int y,
        IBoardItem swapTarget,
        HashSet<Vector2Int> visited,
        int depth)
    {
        if (depth > 15) return false;
        if (board == null || board.MidGrid == null || !board.IsInBounds(x, y)) return false;

        GameObject boosterObj = board.MidGrid[x, y];
        if (boosterObj == null) return false;
        if (!boosterObj.TryGetComponent<IBoosterActivatable>(out var booster)) return false;

        // Kiem tra dieu kien kich hoat cua chinh booster (vi du: LightBall can swapTarget hop le o depth 0)
        if (!booster.CanActivate(depth, swapTarget))
        {
            return false;
        }

        Vector2Int currentPos = new Vector2Int(x, y);
        visited.Add(currentPos);

        // Lay danh sach cac o bi anh huong
        var affectedCells = booster.GetAffectedCells(board, x, y, swapTarget);

        // Phat animation kich hoat cua chinh booster truoc khi xoa
        await booster.PlayActivationAnimationAsync();

        // Xoa ban than booster khoi ban co
        RemoveItem(x, y);

        // Gom cac booster khac nam trong vung no de kich hoat day chuyen
        var chainBoosters = CollectChainBoosters(affectedCells, visited);

        // Phat animation projectile / blast va pha huy cac o qua da hinh (Polymorphism)
        var context = new BoosterActivationContext(
            board, this, x, y, affectedCells, chainBoosters, visited, swapTarget, depth);
        await booster.ExecuteActivationEffectAsync(context);

        // Kich hoat day chuyen tung booster tim duoc trong vung no
        await ActivateChainBoostersAsync(chainBoosters, visited, depth + 1);

        return true;
    }



    // Phat hieu ung hat no va xoa item khoi o (uu tien OverlayGrid, UnderGrid, sau do den MidGrid)
    public void ExplodeAndRemoveCell(int cx, int cy)
    {
        if (board == null || !board.IsInBounds(cx, cy)) return;

        // 1. Neu co vat can tren OverlayGrid (da, bang tuyet, day xich...), uu tien pha huy truoc
        if (TryExplodeOverlay(cx, cy)) return;

        // 2. Neu co vat can tren UnderGrid (khong phai Spawner) thi pha huy UnderItem
        if (TryExplodeUnder(cx, cy)) return;

        // 3. Neu khong co vat can tang tren/duoi thi pha huy va xoa item tren MidGrid
        ExplodeMid(cx, cy);
    }

    // Pha huy vat can tang OverlayGrid tai o (cx, cy)
    private bool TryExplodeOverlay(int cx, int cy)
    {
        if (board.OverlayGrid == null) return false;
        GameObject overlayObj = board.OverlayGrid[cx, cy];
        if (overlayObj == null) return false;

        PlayExplodeParticle(overlayObj, fallbackToDefault: true);
        RemoveOverlayItem(cx, cy);
        return true;
    }

    // Pha huy vat can tang UnderGrid tai o (cx, cy) neu khong phai Spawner
    private bool TryExplodeUnder(int cx, int cy)
    {
        if (board.UnderGrid == null) return false;
        GameObject underObj = board.UnderGrid[cx, cy];
        if (underObj == null) return false;

        if (underObj.TryGetComponent<IBoardItem>(out var underItem) && underItem.ItemId == EnumItemBoard.Spawn)
        {
            return false;
        }

        PlayParticleDefault(underObj.transform.position);
        RemoveUnderItem(cx, cy);
        return true;
    }

    // Pha huy item tang giua MidGrid tai o (cx, cy)
    private void ExplodeMid(int cx, int cy)
    {
        if (board.MidGrid == null) return;
        GameObject obj = board.MidGrid[cx, cy];
        if (obj == null) return;

        PlayExplodeParticle(obj, fallbackToDefault: false);
        RemoveItem(cx, cy);
    }

    // Phat particle theo loai ngoc hoac mau mac dinh
    private void PlayExplodeParticle(GameObject obj, bool fallbackToDefault)
    {
        if (poolParticle == null || obj == null) return;

        if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsValidNormalItem(item))
        {
            poolParticle.Play(obj.transform.position, item.ItemId);
        }
        else if (fallbackToDefault)
        {
            PlayParticleDefault(obj.transform.position);
        }
    }

    private void PlayParticleDefault(Vector3 position)
    {
        if (poolParticle != null)
        {
            poolParticle.Play(position, 1);
        }
    }

    // Xoa 1 item khoi MidGrid va cap nhat trang thai BoardCell ve Empty
    private void RemoveItem(int x, int y) => board?.RemoveMidItem(x, y);

    // Xoa 1 item khoi OverlayGrid va cap nhat trang thai BoardCell neu can
    private void RemoveOverlayItem(int x, int y) => board?.RemoveOverlayItem(x, y);

    // Xoa 1 item khoi UnderGrid neu khong phai Spawner
    private void RemoveUnderItem(int x, int y) => board?.RemoveUnderItem(x, y);

    // Cho vu no hoan tat theo thoi gian delay cau hinh
    public UniTask DelayExplosionAsync(float multiplier = 1f)
    {
        float delay = explosionDelay * multiplier;
        return delay > 0f
            ? UniTask.Delay(System.TimeSpan.FromSeconds(delay))
            : UniTask.CompletedTask;
    }

    // Gom cac booster chua tham gia no trong vung anh huong de kich hoat day chuyen
    private List<Vector2Int> CollectChainBoosters(List<Vector2Int> affectedCells, HashSet<Vector2Int> visited)
    {
        var chainBoosters = new List<Vector2Int>();
        if (affectedCells == null || board == null || board.MidGrid == null) return chainBoosters;

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (visited.Contains(pos)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsBoosterItem(item))
            {
                visited.Add(pos);
                chainBoosters.Add(pos);
            }
        }
        return chainBoosters;
    }

    // Kich hoat day chuyen danh sach cac booster da thu thap duoc
    private async UniTask ActivateChainBoostersAsync(List<Vector2Int> chainBoosters, HashSet<Vector2Int> visited, int nextDepth)
    {
        if (chainBoosters == null || chainBoosters.Count == 0) return;

        for (int i = 0; i < chainBoosters.Count; i++)
        {
            Vector2Int cPos = chainBoosters[i];
            await ActivateBoosterInternalAsync(cPos.x, cPos.y, null, visited, nextDepth);
        }
    }




}


