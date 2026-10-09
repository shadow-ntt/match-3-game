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
    private BoosterComboEffectPlayer _comboEffectPlayer;
    private LightBallComboHandler _lightBallComboHandler;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;
    public BoosterEffectPlayer EffectPlayer => _effectPlayer;
    public BoosterComboEffectPlayer ComboEffectPlayer => _comboEffectPlayer;
    public LightBallComboHandler LightBallComboHandler => _lightBallComboHandler;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
        if (poolParticle == null) poolParticle = FindAnyObjectByType<PoolBlockBreakEffect>();

        _effectPlayer = new BoosterEffectPlayer(this);
        _comboEffectPlayer = new BoosterComboEffectPlayer(this);
        _lightBallComboHandler = new LightBallComboHandler(this);
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

        // Lay danh sach cac o bi anh huong boi combo
        var affectedCells = BoosterComboResolver.GetComboAffectedCells(boosterA.ItemId, boosterB.ItemId, board, cx, cy);

        GameObject objA = board.MidGrid[xA, yA];
        GameObject objB = board.MidGrid[xB, yB];
        Vector3 centerPos = objB != null ? objB.transform.position : GridUtils.GridToWorld(board.Grid, cx, cy);

        // Phat animation hop nhat combo truoc khi xoa
        await _comboEffectPlayer.PlayComboMergeAnimationAsync(objA, objB, boosterA.ItemId, boosterB.ItemId, centerPos);

        // Rung camera neu combo co chua TNT
        var shake = CameraShakeService.Instance;
        if (boosterA.ItemId == EnumItemBoard.TNT && boosterB.ItemId == EnumItemBoard.TNT)
        {
            shake?.ShakeMega().Forget();
        }
        else if (boosterA.ItemId == EnumItemBoard.TNT || boosterB.ItemId == EnumItemBoard.TNT)
        {
            shake?.ShakeTNT().Forget();
        }

        Vector3 lightBallPos = (boosterA.ItemId == EnumItemBoard.LightBall && objA != null)
            ? objA.transform.position
            : (objB != null ? objB.transform.position : centerPos);

        // Xoa 2 vien booster tham gia combo truoc
        RemoveItem(xA, yA);
        RemoveItem(xB, yB);

        // Kiem tra combo LightBall + Booster khac (khong phai LightBall + LightBall)
        bool isLightBallWithOtherBooster = (boosterA.ItemId == EnumItemBoard.LightBall || boosterB.ItemId == EnumItemBoard.LightBall)
                                           && (boosterA.ItemId != boosterB.ItemId);

        if (isLightBallWithOtherBooster)
        {
            EnumItemBoard partnerType = boosterA.ItemId == EnumItemBoard.LightBall ? boosterB.ItemId : boosterA.ItemId;
            await _lightBallComboHandler.HandleLightBallBoosterComboAsync(lightBallPos, partnerType, affectedCells, visited);
            return true;
        }

        // Gom cac booster khac nam trong vung no combo de kich hoat day chuyen
        var chainBoosters = CollectChainBoosters(affectedCells, visited);

        // Phat animation va hieu ung pha huy dac trung cho tung cap combo booster
        await _comboEffectPlayer.PlayComboActivationEffectAsync(boosterA.ItemId, boosterB.ItemId, cx, cy, xA, yA, xB, yB, affectedCells, chainBoosters);

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

        // Neu nguoi choi chu dong vuot LightBall (depth == 0) ma swapTarget khong hop le thi huy
        if (depth == 0 && booster is LightBallItem && (swapTarget == null || !BoardItemUtils.IsValidNormalItem(swapTarget)))
        {
            return false;
        }

        Vector2Int currentPos = new Vector2Int(x, y);
        visited.Add(currentPos);

        // Lay danh sach cac o bi anh huong
        var affectedCells = booster.GetAffectedCells(board, x, y, swapTarget);

        // Luu lai vi tri the gioi va nhan dien loai booster truoc khi xoa
        Vector3 boosterWorldPos = boosterObj.transform.position;
        bool isHRocket = booster is HorizontalRocketItem;
        bool isVRocket = booster is VerticalRocketItem;
        bool isMissile = booster is MissileItem;
        bool isLightBall = booster is LightBallItem;

        // Phat animation kich hoat cua chinh booster truoc khi xoa
        await booster.PlayActivationAnimationAsync();

        // Xoa ban than booster khoi ban co
        RemoveItem(x, y);

        // Gom cac booster khac nam trong vung no de kich hoat day chuyen
        var chainBoosters = CollectChainBoosters(affectedCells, visited);

        // Phat animation projectile / blast va pha huy cac o theo loai booster
        if (isHRocket)
        {
            await _effectPlayer.PlayHorizontalRocketEffectAsync(x, y, boosterWorldPos, affectedCells, chainBoosters);
        }
        else if (isVRocket)
        {
            await _effectPlayer.PlayVerticalRocketEffectAsync(x, y, boosterWorldPos, affectedCells, chainBoosters);
        }
        else if (isMissile)
        {
            await _effectPlayer.PlayMissileEffectAsync(boosterWorldPos, affectedCells, chainBoosters);
        }
        else if (isLightBall)
        {
            await _effectPlayer.PlayLightBallEffectAsync(x, y, affectedCells, chainBoosters);
        }
        else
        {
            // Rung camera khi TNT don no
            var shake = CameraShakeService.Instance;
            shake?.ShakeTNT().Forget();

            // TNT hoac booster khac: phat no dien rong theo song
            await _effectPlayer.PlayWaveExplosionAsync(x, y, 1, affectedCells, chainBoosters);

            await DelayExplosionAsync();
        }

        // Kich hoat day chuyen tung booster tim duoc trong vung no
        await ActivateChainBoostersAsync(chainBoosters, visited, depth + 1);

        return true;
    }



    // Phat hieu ung hat no va xoa item khoi o (uu tien OverlayGrid, UnderGrid, sau do den MidGrid)
    public void ExplodeAndRemoveCell(int cx, int cy)
    {
        if (!board.IsInBounds(cx, cy)) return;

        // 1. Neu co vat can tren OverlayGrid (da, bang tuyet, day xich...), uu tien pha huy OverlayItem truoc
        if (board.OverlayGrid != null && board.OverlayGrid[cx, cy] != null)
        {
            GameObject overlayObj = board.OverlayGrid[cx, cy];
            if (overlayObj != null)
            {
                if (poolParticle != null)
                {
                    if (overlayObj.TryGetComponent<IBoardItem>(out var overlayItem) && BoardItemUtils.IsValidNormalItem(overlayItem))
                    {
                        poolParticle.Play(overlayObj.transform.position, overlayItem.ItemId);
                    }
                    else
                    {
                        poolParticle.Play(overlayObj.transform.position, 1);
                    }
                }
                RemoveOverlayItem(cx, cy);
                return;
            }
        }

        // 2. Neu co vat can tren UnderGrid (khong phai Spawner) thi pha huy UnderItem
        if (board.UnderGrid != null && board.UnderGrid[cx, cy] != null)
        {
            GameObject underObj = board.UnderGrid[cx, cy];
            if (underObj != null && (!underObj.TryGetComponent<IBoardItem>(out var underItem) || underItem.ItemId != EnumItemBoard.Spawn))
            {
                if (poolParticle != null)
                {
                    poolParticle.Play(underObj.transform.position, 1);
                }
                RemoveUnderItem(cx, cy);
                return;
            }
        }

        // 3. Neu khong co vat can tang tren/duoi thi pha huy va xoa item tren MidGrid
        GameObject obj = board.MidGrid != null ? board.MidGrid[cx, cy] : null;
        if (obj == null) return;

        if (obj.TryGetComponent<IBoardItem>(out var item))
        {
            if (poolParticle != null && BoardItemUtils.IsValidNormalItem(item))
            {
                poolParticle.Play(obj.transform.position, item.ItemId);
            }
        }

        RemoveItem(cx, cy);
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


