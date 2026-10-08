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

        // Xoa 2 vien booster tham gia combo truoc
        RemoveItem(xA, yA);
        RemoveItem(xB, yB);

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

        // Phat hieu ung no va xoa tung o thuong (khong xoa booster de chain kich hoat sau)
        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (chainBoosters.Contains(pos)) continue;

            // Bo qua vi tri cua 2 booster combo
            if ((pos.x == xA && pos.y == yA) || (pos.x == xB && pos.y == yB)) continue;

            ExplodeAndRemoveCell(pos.x, pos.y);
        }

        // Cho hieu ung no
        if (explosionDelay > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }

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
        Debug.Log("booster, x, y" + x + ", " + y + " ");
        Debug.Log("affectedCells" + affectedCells.Count);

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

            // TNT hoac booster khac: phat no dien rong dong loat
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!board.IsInBounds(pos.x, pos.y)) continue;
                if (chainBoosters.Contains(pos)) continue;

                ExplodeAndRemoveCell(pos.x, pos.y);
            }

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

    // Hieu ung phong 2 dau dan ten lua ngang ve 2 phia trai va phai
    private async UniTask PlayHorizontalRocketEffectAsync(
        int originX, int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            var leftCells = new List<Vector2Int>();
            for (int col = originX - 1; col >= 0; col--)
            {
                leftCells.Add(new Vector2Int(col, originY));
            }

            var rightCells = new List<Vector2Int>();
            for (int col = originX + 1; col < board.Width; col++)
            {
                rightCells.Add(new Vector2Int(col, originY));
            }

            Vector3 exitLeft = GridUtils.GridToWorld(board.Grid, -1, originY);
            Vector3 exitRight = GridUtils.GridToWorld(board.Grid, board.Width, originY);

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
        }
        else
        {
            // Fallback khi chua co prefab: xoa va phat particle
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!chainBoosters.Contains(pos)) ExplodeAndRemoveCell(pos.x, pos.y);
            }
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
        }
    }

    // Hieu ung phong 2 dau dan ten lua doc ve 2 phia duoi va tren
    private async UniTask PlayVerticalRocketEffectAsync(
        int originX, int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            var downCells = new List<Vector2Int>();
            for (int row = originY - 1; row >= 0; row--)
            {
                downCells.Add(new Vector2Int(originX, row));
            }

            var upCells = new List<Vector2Int>();
            for (int row = originY + 1; row < board.Height; row++)
            {
                upCells.Add(new Vector2Int(originX, row));
            }

            Vector3 exitDown = GridUtils.GridToWorld(board.Grid, originX, -1);
            Vector3 exitUp = GridUtils.GridToWorld(board.Grid, originX, board.Height);

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
        }
        else
        {
            // Fallback khi chua co prefab
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!chainBoosters.Contains(pos)) ExplodeAndRemoveCell(pos.x, pos.y);
            }
            if (explosionDelay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
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
            var tasks = new List<UniTask>();
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
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

    // Phat hieu ung hat no va xoa item khoi o
    private void ExplodeAndRemoveCell(int cx, int cy)
    {
        if (!board.IsInBounds(cx, cy)) return;
        GameObject obj = board.MidGrid[cx, cy];
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

