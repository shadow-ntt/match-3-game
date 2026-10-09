using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Quan ly kich hoat hieu ung va no day chuyen cac Booster tren ban co
public class BoosterActivationManager : Singleton<BoosterActivationManager>
{
    [SerializeField] private Board board;
    [SerializeField] private PoolBlockBreakEffect poolParticle;
    [SerializeField] private float explosionDelay = 0.25f;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
        if (poolParticle == null) poolParticle = FindAnyObjectByType<PoolBlockBreakEffect>();
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
        await PlayComboMergeAnimationAsync(objA, objB, boosterA.ItemId, boosterB.ItemId, centerPos);

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
            await HandleLightBallBoosterComboAsync(lightBallPos, partnerType, affectedCells, visited);
            return true;
        }

        // Gom cac booster khac nam trong vung no combo de kich hoat day chuyen
        var chainBoosters = new List<Vector2Int>();
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

        // Phat animation va hieu ung pha huy dac trung cho tung cap combo booster
        await PlayComboActivationEffectAsync(boosterA.ItemId, boosterB.ItemId, cx, cy, xA, yA, xB, yB, affectedCells, chainBoosters);

        // Kich hoat day chuyen cac booster nam trong vung no combo
        for (int i = 0; i < chainBoosters.Count; i++)
        {
            Vector2Int cPos = chainBoosters[i];
            await ActivateBoosterInternalAsync(cPos.x, cPos.y, null, visited, 1);
        }

        return true;
    }

    // Kich hoat day chuyen cac booster tu danh sach toa do duoc truyen vao
    public async UniTask TriggerChainBoostersAsync(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0 || board == null || board.MidGrid == null) return;

        var visited = new HashSet<Vector2Int>();
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (visited.Contains(pos)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsBoosterItem(item))
            {
                await ActivateBoosterInternalAsync(pos.x, pos.y, null, visited, 0);
            }
        }
    }

    // Ham noi bo thuc hien kich hoat booster va de quy no day chuyen
    private async UniTask<bool> ActivateBoosterInternalAsync(
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
        var chainBoosters = new List<Vector2Int>();
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

        // Phat animation projectile / blast va pha huy cac o theo loai booster
        if (isHRocket)
        {
            await PlayHorizontalRocketEffectAsync(x, y, boosterWorldPos, affectedCells, chainBoosters);
        }
        else if (isVRocket)
        {
            await PlayVerticalRocketEffectAsync(x, y, boosterWorldPos, affectedCells, chainBoosters);
        }
        else if (isMissile)
        {
            await PlayMissileEffectAsync(boosterWorldPos, affectedCells, chainBoosters);
        }
        else if (isLightBall)
        {
            await PlayLightBallEffectAsync(x, y, affectedCells, chainBoosters);
        }
        else
        {
            // Rung camera khi TNT don no
            var shake = CameraShakeService.Instance;
            shake?.ShakeTNT().Forget();

            // TNT hoac booster khac: phat no dien rong theo song
            await PlayWaveExplosionAsync(x, y, 1, affectedCells, chainBoosters);

            if (explosionDelay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            }
        }

        // Kich hoat day chuyen tung booster tim duoc trong vung no
        for (int i = 0; i < chainBoosters.Count; i++)
        {
            Vector2Int cPos = chainBoosters[i];
            await ActivateBoosterInternalAsync(cPos.x, cPos.y, null, visited, depth + 1);
        }

        return true;
    }

    // Ban 1 cap dau dan rocket trai va phai tren 1 hang
    private async UniTask<bool> PlayRocketRowAsync(
        int row,
        int originX,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        if (row < 0 || row >= board.Height) return false;

        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            // Pha huy o tai goc ban neu khong phai booster day chuyen
            Vector2Int originPos = new Vector2Int(originX, row);
            if (board.IsInBounds(originX, row) && !chainBoosters.Contains(originPos))
            {
                ExplodeAndRemoveCell(originX, row);
            }

            var leftCells = new List<Vector2Int>();
            for (int col = originX - 1; col >= 0; col--)
            {
                leftCells.Add(new Vector2Int(col, row));
            }

            var rightCells = new List<Vector2Int>();
            for (int col = originX + 1; col < board.Width; col++)
            {
                rightCells.Add(new Vector2Int(col, row));
            }

            Vector3 exitLeft = GridUtils.GridToWorld(board.Grid, -1, row);
            Vector3 exitRight = GridUtils.GridToWorld(board.Grid, board.Width, row);

            var projLeft = poolRocket.GetProjectile();
            var projRight = poolRocket.GetProjectile();

            async UniTask RunLeftAsync()
            {
                if (projLeft == null) return;
                await projLeft.FlyAlongLineAsync(originWorldPos, exitLeft, leftCells, board.Grid, (cell) =>
                {
                    if (chainBoosters.Contains(cell)) return;
                    ExplodeAndRemoveCell(cell.x, cell.y);
                });
                poolRocket.ReturnProjectile(projLeft);
            }

            async UniTask RunRightAsync()
            {
                if (projRight == null) return;
                await projRight.FlyAlongLineAsync(originWorldPos, exitRight, rightCells, board.Grid, (cell) =>
                {
                    if (chainBoosters.Contains(cell)) return;
                    ExplodeAndRemoveCell(cell.x, cell.y);
                });
                poolRocket.ReturnProjectile(projRight);
            }

            await UniTask.WhenAll(RunLeftAsync(), RunRightAsync());
            return false;
        }
        else
        {
            // Fallback khi chua co prefab: xoa cac o thuoc hang nay
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (pos.y == row && !chainBoosters.Contains(pos))
                {
                    ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }
            return true;
        }
    }

    // Ban 1 cap dau dan rocket len va xuong tren 1 cot
    private async UniTask<bool> PlayRocketColumnAsync(
        int col,
        int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        if (col < 0 || col >= board.Width) return false;

        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            // Pha huy o tai goc ban neu khong phai booster day chuyen
            Vector2Int originPos = new Vector2Int(col, originY);
            if (board.IsInBounds(col, originY) && !chainBoosters.Contains(originPos))
            {
                ExplodeAndRemoveCell(col, originY);
            }

            var downCells = new List<Vector2Int>();
            for (int row = originY - 1; row >= 0; row--)
            {
                downCells.Add(new Vector2Int(col, row));
            }

            var upCells = new List<Vector2Int>();
            for (int row = originY + 1; row < board.Height; row++)
            {
                upCells.Add(new Vector2Int(col, row));
            }

            Vector3 exitDown = GridUtils.GridToWorld(board.Grid, col, -1);
            Vector3 exitUp = GridUtils.GridToWorld(board.Grid, col, board.Height);

            var projDown = poolRocket.GetProjectile();
            var projUp = poolRocket.GetProjectile();

            async UniTask RunDownAsync()
            {
                if (projDown == null) return;
                await projDown.FlyAlongLineAsync(originWorldPos, exitDown, downCells, board.Grid, (cell) =>
                {
                    if (chainBoosters.Contains(cell)) return;
                    ExplodeAndRemoveCell(cell.x, cell.y);
                });
                poolRocket.ReturnProjectile(projDown);
            }

            async UniTask RunUpAsync()
            {
                if (projUp == null) return;
                await projUp.FlyAlongLineAsync(originWorldPos, exitUp, upCells, board.Grid, (cell) =>
                {
                    if (chainBoosters.Contains(cell)) return;
                    ExplodeAndRemoveCell(cell.x, cell.y);
                });
                poolRocket.ReturnProjectile(projUp);
            }

            await UniTask.WhenAll(RunDownAsync(), RunUpAsync());
            return false;
        }
        else
        {
            // Fallback khi chua co prefab: xoa cac o thuoc cot nay
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (pos.x == col && !chainBoosters.Contains(pos))
                {
                    ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }
            return true;
        }
    }

    // Hieu ung phong 2 dau dan ten lua ngang ve 2 phia trai va phai
    private async UniTask PlayHorizontalRocketEffectAsync(
        int originX, int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        bool usedFallback = await PlayRocketRowAsync(originY, originX, originWorldPos, affectedCells, chainBoosters);
        if (usedFallback && explosionDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }
    }

    // Hieu ung phong 2 dau dan ten lua doc ve 2 phia duoi va tren
    private async UniTask PlayVerticalRocketEffectAsync(
        int originX, int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        bool usedFallback = await PlayRocketColumnAsync(originX, originY, originWorldPos, affectedCells, chainBoosters);
        if (usedFallback && explosionDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }
    }

    // Hieu ung ten lua dan duong bay uon cong toi muc tieu
    private async UniTask PlayMissileEffectAsync(
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        if (affectedCells.Count == 0) return;

        Vector2Int targetPos = affectedCells[0];
        Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, targetPos.x, targetPos.y);

        var poolMissile = PoolMissileProjectile.Instance;
        if (poolMissile != null && poolMissile.HasPrefab)
        {
            var proj = poolMissile.GetProjectile();
            if (proj != null)
            {
                await proj.FlyToTargetAsync(originWorldPos, targetWorld, 0.45f);
                poolMissile.ReturnProjectile(proj);
            }
        }

        if (!chainBoosters.Contains(targetPos))
        {
            ExplodeAndRemoveCell(targetPos.x, targetPos.y);
        }

        if (explosionDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }
    }

    // Hieu ung phat no lan toa tu LightBall den cac gem cung mau
    private async UniTask PlayLightBallEffectAsync(
        int originX, int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        Vector2Int center = new Vector2Int(originX, originY);
        Vector3 originWorldPos = GridUtils.GridToWorld(board.Grid, originX, originY);

        var poolBeam = PoolLightBallBeam.Instance;
        if (poolBeam != null && poolBeam.HasPrefab)
        {
            // Tim mau cua gem de to mau tia sang
            Color beamColor = Color.yellow;
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (board.IsInBounds(pos.x, pos.y))
                {
                    var obj = board.MidGrid[pos.x, pos.y];
                    if (obj != null && obj.TryGetComponent<IBoardItem>(out var item))
                    {
                        beamColor = GetItemColor(item.ItemId);
                        break;
                    }
                }
            }

            // Ban cac tia sang dong loat tu LightBall toi tung gem muc tieu
            await PlayLightBallBeamsAsync(originWorldPos, affectedCells, beamColor);

            // Sau khi tia sang ban toi noi, phat no va xoa tat ca gem dong loat
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!chainBoosters.Contains(pos))
                {
                    ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }

            if (explosionDelay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            }
        }
        else
        {
            // Fallback khi chua co prefab: xoa tuan tu lan toa theo khoang cach
            var sortedCells = new List<Vector2Int>(affectedCells);
            sortedCells.Sort((a, b) =>
            {
                float distA = Vector2Int.Distance(center, a);
                float distB = Vector2Int.Distance(center, b);
                return distA.CompareTo(distB);
            });

            for (int i = 0; i < sortedCells.Count; i++)
            {
                Vector2Int pos = sortedCells[i];
                if (!chainBoosters.Contains(pos))
                {
                    ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }

            if (explosionDelay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            }
        }
    }

    // Phat hieu ung hat no va xoa item khoi o (uu tien OverlayGrid, UnderGrid, sau do den MidGrid)
    private void ExplodeAndRemoveCell(int cx, int cy)
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
    private void RemoveItem(int x, int y)
    {
        if (board != null)
        {
            board.RemoveMidItem(x, y);
            return;
        }

        if (!board.IsInBounds(x, y)) return;
        GameObject itemObj = board.MidGrid[x, y];
        if (itemObj == null) return;

        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(itemObj);
        }
        board.MidGrid[x, y] = null;

        if (board.BoardCellGrid != null &&
            board.BoardCellGrid[x, y] != null &&
            board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Empty;
            cell.IsGettingFilled = false;
        }
    }

    // Xoa 1 item khoi OverlayGrid va cap nhat trang thai BoardCell neu can
    private void RemoveOverlayItem(int x, int y)
    {
        if (board != null)
        {
            board.RemoveOverlayItem(x, y);
            return;
        }

        if (!board.IsInBounds(x, y) || board.OverlayGrid == null) return;
        GameObject overlayObj = board.OverlayGrid[x, y];
        if (overlayObj == null) return;

        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(overlayObj);
        }
        board.OverlayGrid[x, y] = null;

        bool isMidEmpty = board.MidGrid == null || board.MidGrid[x, y] == null;
        if (isMidEmpty && board.BoardCellGrid != null &&
            board.BoardCellGrid[x, y] != null &&
            board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Empty;
            cell.IsGettingFilled = false;
        }
    }

    // Xoa 1 item khoi UnderGrid neu khong phai Spawner
    private void RemoveUnderItem(int x, int y)
    {
        if (board != null)
        {
            board.RemoveUnderItem(x, y);
            return;
        }

        if (!board.IsInBounds(x, y) || board.UnderGrid == null) return;
        GameObject underObj = board.UnderGrid[x, y];
        if (underObj == null) return;

        if (underObj.TryGetComponent<IBoardItem>(out var underItem) && underItem.ItemId == EnumItemBoard.Spawn)
        {
            return;
        }

        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(underObj);
        }
        board.UnderGrid[x, y] = null;

        bool isMidEmpty = board.MidGrid == null || board.MidGrid[x, y] == null;
        bool isOverlayEmpty = board.OverlayGrid == null || board.OverlayGrid[x, y] == null;
        if (isMidEmpty && isOverlayEmpty && board.BoardCellGrid != null &&
            board.BoardCellGrid[x, y] != null &&
            board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Empty;
            cell.IsGettingFilled = false;
        }
    }

    // Ban cac tia sang LineRenderer tu diem phat toi danh sach muc tieu
    private async UniTask PlayLightBallBeamsAsync(
        Vector3 originWorldPos,
        List<Vector2Int> beamTargets,
        Color beamColor)
    {
        if (beamTargets == null || beamTargets.Count == 0) return;

        var poolBeam = PoolLightBallBeam.Instance;
        if (poolBeam == null || !poolBeam.HasPrefab) return;

        var tasks = new List<UniTask>();
        for (int i = 0; i < beamTargets.Count; i++)
        {
            Vector2Int pos = beamTargets[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, pos.x, pos.y);
            var beam = poolBeam.GetBeam();
            if (beam != null)
            {
                async UniTask ShootAndReturn()
                {
                    await beam.ShootAsync(originWorldPos, targetWorld, 0.2f, beamColor);
                    poolBeam.ReturnBeam(beam);
                }
                tasks.Add(ShootAndReturn());
            }
        }

        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks);
        }
    }

    // Phat no theo cac vong song lan toa tu tam ra ngoai
    private async UniTask PlayWaveExplosionAsync(
        int cx, int cy,
        int radius,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        for (int ring = 0; ring <= radius; ring++)
        {
            bool anyExploded = false;
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!board.IsInBounds(pos.x, pos.y)) continue;
                int dist = Mathf.Max(Mathf.Abs(pos.x - cx), Mathf.Abs(pos.y - cy));
                if (dist == ring || (ring == radius && dist > radius))
                {
                    if (!chainBoosters.Contains(pos))
                    {
                        ExplodeAndRemoveCell(pos.x, pos.y);
                        anyExploded = true;
                    }
                }
            }

            if (anyExploded && ring < radius)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.05f));
            }
        }
    }

    // Ban 1 ten lua Missile toi 1 toa do muc tieu va pha huy o do
    private async UniTask FlySingleMissileAsync(
        Vector3 originWorldPos,
        Vector2Int targetPos,
        List<Vector2Int> chainBoosters)
    {
        if (!board.IsInBounds(targetPos.x, targetPos.y)) return;

        var poolMissile = PoolMissileProjectile.Instance;
        if (poolMissile != null && poolMissile.HasPrefab)
        {
            Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, targetPos.x, targetPos.y);
            var proj = poolMissile.GetProjectile();
            if (proj != null)
            {
                await proj.FlyToTargetAsync(originWorldPos, targetWorld, 0.45f);
                poolMissile.ReturnProjectile(proj);
            }
        }
        if (!chainBoosters.Contains(targetPos))
        {
            ExplodeAndRemoveCell(targetPos.x, targetPos.y);
        }
    }

    // Dieu phoi va phat hoat anh pha huy vung anh huong dac trung cho tung cap combo booster
    private async UniTask PlayComboActivationEffectAsync(
        EnumItemBoard typeA, EnumItemBoard typeB,
        int cx, int cy,
        int xA, int yA, int xB, int yB,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        EnumItemBoard first = typeA <= typeB ? typeA : typeB;
        EnumItemBoard second = typeA <= typeB ? typeB : typeA;
        Vector3 centerWorldPos = GridUtils.GridToWorld(board.Grid, cx, cy);

        bool isRocketFirst = first == EnumItemBoard.HorizontalRocket || first == EnumItemBoard.VerticalRocket;
        bool isRocketSecond = second == EnumItemBoard.HorizontalRocket || second == EnumItemBoard.VerticalRocket;

        if (second == EnumItemBoard.LightBall)
        {
            await PlayComboLightBallAsync(first, cx, cy, centerWorldPos, affectedCells, chainBoosters);
        }
        else if (second == EnumItemBoard.Missile)
        {
            await PlayComboMissileAsync(first, cx, cy, centerWorldPos, affectedCells, chainBoosters);
        }
        else if (second == EnumItemBoard.TNT)
        {
            await PlayComboTNTAsync(first, cx, cy, centerWorldPos, affectedCells, chainBoosters);
        }
        else if (isRocketFirst && isRocketSecond)
        {
            await PlayComboRocketRocketAsync(first, second, cx, cy, centerWorldPos, affectedCells, chainBoosters);
        }
        else
        {
            // Fallback: pha huy tat ca cac o thuong
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!board.IsInBounds(pos.x, pos.y)) continue;
                if (chainBoosters.Contains(pos)) continue;
                if ((pos.x == xA && pos.y == yA) || (pos.x == xB && pos.y == yB)) continue;
                ExplodeAndRemoveCell(pos.x, pos.y);
            }
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }

        // Dam bao khong sot bat ky o nao trong vung affectedCells
        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (chainBoosters.Contains(pos)) continue;
            if ((pos.x == xA && pos.y == yA) || (pos.x == xB && pos.y == yB)) continue;
            ExplodeAndRemoveCell(pos.x, pos.y);
        }
    }

    // Xu ly hoat anh pha huy cho combo LightBall + LightBall (xoa toan bo ban co)
    private async UniTask PlayComboLightBallAsync(
        EnumItemBoard partnerType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        // LightBall + LightBall: xoa toan bo ban co
        if (partnerType == EnumItemBoard.LightBall)
        {
            var shake = CameraShakeService.Instance;
            shake?.ShakeMega().Forget();

            await PlayLightBallBeamsAsync(centerWorldPos, affectedCells, Color.yellow);
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!chainBoosters.Contains(pos))
                {
                    ExplodeAndRemoveCell(pos.x, pos.y);
                    if (board.MidGrid != null && board.MidGrid[pos.x, pos.y] != null)
                    {
                        ExplodeAndRemoveCell(pos.x, pos.y);
                    }
                }
            }
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }
    }

    // Xu ly combo LightBall + Booster khac:
    // 1. Chieu tia sang tu LightBall toi tat ca gem duoc chon (dominant color)
    // 2. Chuyen doi tat ca gem do thanh Booster dang ket hop (kem hieu ung pop)
    // 3. Kich hoat lan luot / day chuyen tat ca cac Booster vua tao
    private async UniTask HandleLightBallBoosterComboAsync(
        Vector3 centerPos,
        EnumItemBoard partnerType,
        List<Vector2Int> targetCells,
        HashSet<Vector2Int> visited)
    {
        if (targetCells == null || targetCells.Count == 0) return;

        // 1. Lay mau cua gem de to mau tia sang
        Color beamColor = GetFirstItemColor(targetCells);

        // 2. Ban cac tia sang dong loat tu LightBall toi tung gem muc tieu
        await PlayLightBallBeamsAsync(centerPos, targetCells, beamColor);

        // 3. Chuyen doi cac gem thanh Booster tuong ung
        var convertedPositions = new List<Vector2Int>();
        var popTasks = new List<UniTask>();

        for (int i = 0; i < targetCells.Count; i++)
        {
            Vector2Int pos = targetCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;

            GameObject newBooster = ConvertGemToBooster(pos.x, pos.y, partnerType);
            if (newBooster != null)
            {
                convertedPositions.Add(pos);

                // Hieu ung pop xuat hien booter
                newBooster.transform.localScale = Vector3.zero;
                var popTween = newBooster.transform.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack);
                popTasks.Add(popTween.ToUniTask());
            }
        }

        if (popTasks.Count > 0)
        {
            await UniTask.WhenAll(popTasks);
        }

        // Delay nho de nguoi choi nhin ro cac booter moi duoc tao truoc khi no
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f));

        // 4. Kich hoat lan luot / day chuyen tat ca cac booter vua tao
        for (int i = 0; i < convertedPositions.Count; i++)
        {
            Vector2Int pos = convertedPositions[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (visited.Contains(pos)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsBoosterItem(item))
            {
                await ActivateBoosterInternalAsync(pos.x, pos.y, null, visited, 1);

                if (explosionDelay > 0f)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay * 0.5f));
                }
            }
        }
    }

    // Thay the 1 vien gem tai (x, y) thanh 1 vien Booster loai targetType
    private GameObject ConvertGemToBooster(int x, int y, EnumItemBoard targetType)
    {
        if (board == null || board.MidGrid == null || !board.IsInBounds(x, y)) return null;

        GameObject existing = board.MidGrid[x, y];
        if (existing == null) return null;

        // Chi convert gem thuong
        if (!existing.TryGetComponent<IBoardItem>(out var item) || !BoardItemUtils.IsValidNormalItem(item))
        {
            return null;
        }

        // Phat hieu ung hat no tai vien gem truoc khi bien doi
        if (poolParticle != null)
        {
            poolParticle.Play(existing.transform.position, item.ItemId);
        }

        Vector3 worldPos = GridUtils.GridToWorld(board.Grid, x, y);
        Transform parent = board.MidTilemap != null ? board.MidTilemap.transform : board.transform;

        // Tra gem cu ve pool
        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(existing);
        }
        board.MidGrid[x, y] = null;

        // Sinh booter moi tu pool
        GameObject newBooster = Pooltem.Instance != null ? Pooltem.Instance.SpawnBoardItem((int)targetType, worldPos, Quaternion.identity, parent) : null;
        if (newBooster == null) return null;

        board.MidGrid[x, y] = newBooster;

        if (board.BoardCellGrid != null &&
            board.BoardCellGrid[x, y] != null &&
            board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Occupied;
            cell.IsGettingFilled = false;
        }

        return newBooster;
    }

    // Xu ly hoat anh pha huy cho cac combo chua Missile
    private async UniTask PlayComboMissileAsync(
        EnumItemBoard partnerType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        // 1. Missile + Missile: 3 missile bay dong loat
        if (partnerType == EnumItemBoard.Missile)
        {
            var tasks = new List<UniTask>();
            for (int i = 0; i < affectedCells.Count; i++)
            {
                tasks.Add(FlySingleMissileAsync(centerWorldPos, affectedCells[i], chainBoosters));
            }
            await UniTask.WhenAll(tasks);
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }

        // 2. Missile + TNT: 3x3 tai tam -> missile bay -> 3x3 tai muc tieu
        if (partnerType == EnumItemBoard.TNT)
        {
            var centerArea = affectedCells.FindAll(p => Mathf.Abs(p.x - cx) <= 1 && Mathf.Abs(p.y - cy) <= 1);
            var targetArea = affectedCells.FindAll(p => Mathf.Abs(p.x - cx) > 1 || Mathf.Abs(p.y - cy) > 1);

            var shake = CameraShakeService.Instance;
            shake?.ShakeTNT().Forget();

            await PlayWaveExplosionAsync(cx, cy, 1, centerArea, chainBoosters);

            if (targetArea.Count > 0)
            {
                int targetX = 0, targetY = 0;
                for (int i = 0; i < targetArea.Count; i++)
                {
                    targetX += targetArea[i].x;
                    targetY += targetArea[i].y;
                }
                targetX = Mathf.RoundToInt((float)targetX / targetArea.Count);
                targetY = Mathf.RoundToInt((float)targetY / targetArea.Count);

                Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, targetX, targetY);
                var poolMissile = PoolMissileProjectile.Instance;
                if (poolMissile != null && poolMissile.HasPrefab)
                {
                    var proj = poolMissile.GetProjectile();
                    if (proj != null)
                    {
                        await proj.FlyToTargetAsync(centerWorldPos, targetWorld, 0.25f);
                        poolMissile.ReturnProjectile(proj);
                    }
                }

                shake?.ShakeTNT().Forget();
                await PlayWaveExplosionAsync(targetX, targetY, 1, targetArea, chainBoosters);
            }

            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }

        // 3. Missile + HorizontalRocket: rocket hang cy -> missile bay -> rocket hang muc tieu
        if (partnerType == EnumItemBoard.HorizontalRocket)
        {
            var row1Cells = affectedCells.FindAll(p => p.y == cy);
            var row2Cells = affectedCells.FindAll(p => p.y != cy);

            await PlayRocketRowAsync(cy, cx, centerWorldPos, row1Cells, chainBoosters);

            if (row2Cells.Count > 0)
            {
                int targetY = row2Cells[0].y;
                Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, cx, targetY);
                var poolMissile = PoolMissileProjectile.Instance;
                if (poolMissile != null && poolMissile.HasPrefab)
                {
                    var proj = poolMissile.GetProjectile();
                    if (proj != null)
                    {
                        await proj.FlyToTargetAsync(centerWorldPos, targetWorld, 0.2f);
                        poolMissile.ReturnProjectile(proj);
                    }
                }
                await PlayRocketRowAsync(targetY, cx, targetWorld, row2Cells, chainBoosters);
            }

            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }

        // 4. Missile + VerticalRocket: rocket cot cx -> missile bay -> rocket cot muc tieu
        if (partnerType == EnumItemBoard.VerticalRocket)
        {
            var col1Cells = affectedCells.FindAll(p => p.x == cx);
            var col2Cells = affectedCells.FindAll(p => p.x != cx);

            await PlayRocketColumnAsync(cx, cy, centerWorldPos, col1Cells, chainBoosters);

            if (col2Cells.Count > 0)
            {
                int targetX = col2Cells[0].x;
                Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, targetX, cy);
                var poolMissile = PoolMissileProjectile.Instance;
                if (poolMissile != null && poolMissile.HasPrefab)
                {
                    var proj = poolMissile.GetProjectile();
                    if (proj != null)
                    {
                        await proj.FlyToTargetAsync(centerWorldPos, targetWorld, 0.2f);
                        poolMissile.ReturnProjectile(proj);
                    }
                }
                await PlayRocketColumnAsync(targetX, cy, targetWorld, col2Cells, chainBoosters);
            }

            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }
    }

    // Xu ly hoat anh pha huy cho cac combo chua TNT (TNT+TNT hoac TNT+Rocket)
    private async UniTask PlayComboTNTAsync(
        EnumItemBoard partnerType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        // 1. TNT + TNT: song no 5x5 quanh tam
        if (partnerType == EnumItemBoard.TNT)
        {
            var shake = CameraShakeService.Instance;
            shake?.ShakeMega().Forget();

            await PlayWaveExplosionAsync(cx, cy, 2, affectedCells, chainBoosters);
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }

        // 2. Rocket + TNT: Mega Rocket ban 3 hang va 3 cot dong thoi
        if (partnerType == EnumItemBoard.HorizontalRocket || partnerType == EnumItemBoard.VerticalRocket)
        {
            var shake = CameraShakeService.Instance;
            shake?.ShakeTNT().Forget();

            var tasks = new List<UniTask>();
            for (int dy = -1; dy <= 1; dy++)
            {
                int row = cy + dy;
                if (row >= 0 && row < board.Height)
                {
                    Vector3 rowOrigin = GridUtils.GridToWorld(board.Grid, cx, row);
                    tasks.Add(PlayRocketRowAsync(row, cx, rowOrigin, affectedCells, chainBoosters));
                }
            }
            for (int dx = -1; dx <= 1; dx++)
            {
                int col = cx + dx;
                if (col >= 0 && col < board.Width)
                {
                    Vector3 colOrigin = GridUtils.GridToWorld(board.Grid, col, cy);
                    tasks.Add(PlayRocketColumnAsync(col, cy, colOrigin, affectedCells, chainBoosters));
                }
            }

            await UniTask.WhenAll(tasks);
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }
    }

    // Xu ly hoat anh pha huy cho cac combo Rocket + Rocket (HH+HH, VV+VV, HH+VV)
    private async UniTask PlayComboRocketRocketAsync(
        EnumItemBoard firstType,
        EnumItemBoard secondType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        // 1. HH + HH: ban 3 hang ngang dong thoi
        if (firstType == EnumItemBoard.HorizontalRocket && secondType == EnumItemBoard.HorizontalRocket)
        {
            var tasks = new List<UniTask>();
            for (int dy = -1; dy <= 1; dy++)
            {
                int row = cy + dy;
                if (row >= 0 && row < board.Height)
                {
                    Vector3 rowOrigin = GridUtils.GridToWorld(board.Grid, cx, row);
                    tasks.Add(PlayRocketRowAsync(row, cx, rowOrigin, affectedCells, chainBoosters));
                }
            }
            await UniTask.WhenAll(tasks);
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }

        // 2. VV + VV: ban 3 cot doc dong thoi
        if (firstType == EnumItemBoard.VerticalRocket && secondType == EnumItemBoard.VerticalRocket)
        {
            var tasks = new List<UniTask>();
            for (int dx = -1; dx <= 1; dx++)
            {
                int col = cx + dx;
                if (col >= 0 && col < board.Width)
                {
                    Vector3 colOrigin = GridUtils.GridToWorld(board.Grid, col, cy);
                    tasks.Add(PlayRocketColumnAsync(col, cy, colOrigin, affectedCells, chainBoosters));
                }
            }
            await UniTask.WhenAll(tasks);
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            return;
        }

        // 3. HH + VV: dau thap ban 1 hang va 1 cot dong thoi
        await UniTask.WhenAll(
            PlayRocketRowAsync(cy, cx, centerWorldPos, affectedCells, chainBoosters),
            PlayRocketColumnAsync(cx, cy, centerWorldPos, affectedCells, chainBoosters)
        );
        if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
    }

    // Lay mau cua vien ngoc hop le dau tien trong danh sach de to mau tia sang
    private Color GetFirstItemColor(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];
            if (board.IsInBounds(pos.x, pos.y))
            {
                var obj = board.MidGrid[pos.x, pos.y];
                if (obj != null && obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsValidNormalItem(item))
                {
                    return GetItemColor(item.ItemId);
                }
            }
        }
        return Color.yellow;
    }



    // Phat animation hop nhat va hieu ung dac trung cho tung cap booster khi tao combo
    private async UniTask PlayComboMergeAnimationAsync(
        GameObject objA,
        GameObject objB,
        EnumItemBoard typeA,
        EnumItemBoard typeB,
        Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        // Chuan hoa thu tu de xu ly cap doi xung
        bool swapOrder = typeA > typeB;
        GameObject firstObj = swapOrder ? objB : objA;
        GameObject secondObj = swapOrder ? objA : objB;
        EnumItemBoard firstType = swapOrder ? typeB : typeA;
        EnumItemBoard secondType = swapOrder ? typeA : typeB;

        bool isRocketA = firstType == EnumItemBoard.HorizontalRocket || firstType == EnumItemBoard.VerticalRocket;
        bool isRocketB = secondType == EnumItemBoard.HorizontalRocket || secondType == EnumItemBoard.VerticalRocket;

        // 1. Cap chua LightBall
        if (secondType == EnumItemBoard.LightBall)
        {
            // LightBall phong to va xoay tron, vien kia thu nho va bi hut vao tam
            var moveOther = firstObj.transform.DOMove(secondObj.transform.position, 0.18f);
            var scaleOther = firstObj.transform.DOScale(0.2f, 0.18f);
            var scaleLB = secondObj.transform.DOScale(1.5f, 0.18f);
            var rotLB = secondObj.transform.DORotate(new Vector3(0, 0, 360f), 0.22f, RotateMode.FastBeyond360);
            await UniTask.WhenAll(moveOther.ToUniTask(), scaleOther.ToUniTask(), scaleLB.ToUniTask(), rotLB.ToUniTask());

            var flashLB = secondObj.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
            await flashLB.ToUniTask();
            return;
        }

        // 2. Cap Rocket + Rocket
        if (isRocketA && isRocketB)
        {
            // 2 Rocket thu nho va lao vao nhau tai tam
            var moveA = firstObj.transform.DOMove(centerPos, 0.15f);
            var moveB = secondObj.transform.DOMove(centerPos, 0.15f);
            var scaleA = firstObj.transform.DOScale(0.5f, 0.15f);
            var scaleB = secondObj.transform.DOScale(0.5f, 0.15f);
            await UniTask.WhenAll(moveA.ToUniTask(), moveB.ToUniTask(), scaleA.ToUniTask(), scaleB.ToUniTask());

            var flashA = firstObj.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
            var flashB = secondObj.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
            await UniTask.WhenAll(flashA.ToUniTask(), flashB.ToUniTask());
            return;
        }

        // 3. Cap TNT + TNT hoac TNT + Rocket
        if (secondType == EnumItemBoard.TNT || (firstType == EnumItemBoard.TNT && isRocketB))
        {
            // Nhap nhay mau do va rung manh truoc khi no
            var srA = firstObj.GetComponentInChildren<SpriteRenderer>();
            var srB = secondObj.GetComponentInChildren<SpriteRenderer>();
            var tasks = new List<UniTask>();

            if (srA != null) tasks.Add(srA.DOColor(Color.red, 0.05f).SetLoops(4, LoopType.Yoyo).ToUniTask());
            if (srB != null) tasks.Add(srB.DOColor(Color.red, 0.05f).SetLoops(4, LoopType.Yoyo).ToUniTask());

            var shakeA = firstObj.transform.DOShakePosition(0.2f, 0.1f, 15);
            var shakeB = secondObj.transform.DOShakePosition(0.2f, 0.1f, 15);
            tasks.Add(shakeA.ToUniTask());
            tasks.Add(shakeB.ToUniTask());
            await UniTask.WhenAll(tasks);

            var pulseA = firstObj.transform.DOScale(1.4f, 0.08f).SetLoops(2, LoopType.Yoyo);
            var pulseB = secondObj.transform.DOScale(1.4f, 0.08f).SetLoops(2, LoopType.Yoyo);
            await UniTask.WhenAll(pulseA.ToUniTask(), pulseB.ToUniTask());
            return;
        }

        // 4. Cap Missile + Missile
        if (firstType == EnumItemBoard.Missile && secondType == EnumItemBoard.Missile)
        {
            // Ca 2 xoay 360 do va phong to thu nho
            var rotA = firstObj.transform.DORotate(new Vector3(0, 0, 360f), 0.15f, RotateMode.FastBeyond360);
            var rotB = secondObj.transform.DORotate(new Vector3(0, 0, -360f), 0.15f, RotateMode.FastBeyond360);
            var scaleA = firstObj.transform.DOScale(1.3f, 0.08f).SetLoops(2, LoopType.Yoyo);
            var scaleB = secondObj.transform.DOScale(1.3f, 0.08f).SetLoops(2, LoopType.Yoyo);
            await UniTask.WhenAll(rotA.ToUniTask(), rotB.ToUniTask(), scaleA.ToUniTask(), scaleB.ToUniTask());
            return;
        }

        // 5. Cap Missile + Rocket hoac Missile + TNT
        if (secondType == EnumItemBoard.Missile)
        {
            // Missile phong vut len roi roi xuong tam
            Vector3 jumpPos = secondObj.transform.position + Vector3.up * 1.5f;
            var jumpUp = secondObj.transform.DOMove(jumpPos, 0.12f).SetEase(Ease.OutQuad);
            var scaleBase = firstObj.transform.DOScale(1.2f, 0.12f);
            await UniTask.WhenAll(jumpUp.ToUniTask(), scaleBase.ToUniTask());

            var slamDown = secondObj.transform.DOMove(centerPos, 0.12f).SetEase(Ease.InQuad);
            await slamDown.ToUniTask();

            var impact = firstObj.transform.DOScale(1.4f, 0.06f).SetLoops(2, LoopType.Yoyo);
            await impact.ToUniTask();
            return;
        }

        // 6. Truong hop mac dinh (fallback)
        var defA = firstObj.transform.DOScale(1.3f, 0.1f).SetLoops(2, LoopType.Yoyo);
        var defB = secondObj.transform.DOScale(1.3f, 0.1f).SetLoops(2, LoopType.Yoyo);
        await UniTask.WhenAll(defA.ToUniTask(), defB.ToUniTask());
    }

    // Tra ve mau sac dai dien cho tung loai ngoc
    private Color GetItemColor(EnumItemBoard itemId)
    {
        switch (itemId)
        {
            case EnumItemBoard.Red: return Color.red;
            case EnumItemBoard.Blue: return new Color(0.2f, 0.6f, 1f);
            case EnumItemBoard.Green: return Color.green;
            case EnumItemBoard.Yellow: return Color.yellow;
            case EnumItemBoard.Purple: return new Color(0.7f, 0.2f, 1f);
            case EnumItemBoard.Orange: return new Color(1f, 0.5f, 0f);
            case EnumItemBoard.Pink: return new Color(1f, 0.4f, 0.7f);
            default: return Color.yellow;
        }
    }
}

