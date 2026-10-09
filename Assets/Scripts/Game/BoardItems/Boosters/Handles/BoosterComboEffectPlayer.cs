using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly toan bo hoat anh hop nhat (merge) va hoat anh kich hoat cac cap Combo booster
public class BoosterComboEffectPlayer
{
    private readonly BoosterActivationManager _manager;

    public BoosterComboEffectPlayer(BoosterActivationManager manager)
    {
        _manager = manager;
    }

    private BoosterEffectPlayer EffectPlayer => _manager.EffectPlayer;

    // Phat animation hop nhat va hieu ung dac trung cho tung cap booster khi tao combo
    public async UniTask PlayComboMergeAnimationAsync(
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

    // Dieu phoi va phat hoat anh pha huy vung anh huong dac trung cho tung cap combo booster
    public async UniTask PlayComboActivationEffectAsync(
        EnumItemBoard typeA, EnumItemBoard typeB,
        int cx, int cy,
        int xA, int yA, int xB, int yB,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

        EnumItemBoard first = typeA <= typeB ? typeA : typeB;
        EnumItemBoard second = typeA <= typeB ? typeB : typeA;
        Vector3 centerWorldPos = GridUtils.GridToWorld(board.Grid, cx, cy);

        bool isRocketFirst = first == EnumItemBoard.HorizontalRocket || first == EnumItemBoard.VerticalRocket;
        bool isRocketSecond = second == EnumItemBoard.HorizontalRocket || second == EnumItemBoard.VerticalRocket;

        if (second == EnumItemBoard.LightBall)
        {
            // Combo chua LightBall ma den duoc day la LightBall + LightBall (vi LightBall voi booster khac da xu ly rieng)
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
                _manager.ExplodeAndRemoveCell(pos.x, pos.y);
            }
            await _manager.DelayExplosionAsync();
        }

        // Dam bao khong sot bat ky o nao trong vung affectedCells
        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (chainBoosters.Contains(pos)) continue;
            if ((pos.x == xA && pos.y == yA) || (pos.x == xB && pos.y == yB)) continue;
            _manager.ExplodeAndRemoveCell(pos.x, pos.y);
        }
    }

    // Xu ly hoat anh pha huy cho combo LightBall + LightBall (xoa toan bo ban co)
    public async UniTask PlayComboLightBallAsync(
        EnumItemBoard partnerType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

        // LightBall + LightBall: xoa toan bo ban co
        if (partnerType == EnumItemBoard.LightBall)
        {
            var shake = CameraShakeService.Instance;
            shake?.ShakeMega().Forget();

            await EffectPlayer.PlayLightBallBeamsAsync(centerWorldPos, affectedCells, Color.yellow);
            for (int i = 0; i < affectedCells.Count; i++)
            {
                Vector2Int pos = affectedCells[i];
                if (!chainBoosters.Contains(pos))
                {
                    _manager.ExplodeAndRemoveCell(pos.x, pos.y);
                    if (board.MidGrid != null && board.MidGrid[pos.x, pos.y] != null)
                    {
                        _manager.ExplodeAndRemoveCell(pos.x, pos.y);
                    }
                }
            }
            await _manager.DelayExplosionAsync();
            return;
        }
    }

    // Xu ly hoat anh pha huy cho cac combo chua Missile
    public async UniTask PlayComboMissileAsync(
        EnumItemBoard partnerType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

        // 1. Missile + Missile: 3 missile bay dong loat
        if (partnerType == EnumItemBoard.Missile)
        {
            var tasks = new List<UniTask>();
            for (int i = 0; i < affectedCells.Count; i++)
            {
                tasks.Add(EffectPlayer.FlySingleMissileAsync(centerWorldPos, affectedCells[i], chainBoosters));
            }
            await UniTask.WhenAll(tasks);
            await _manager.DelayExplosionAsync();
            return;
        }

        // 2. Missile + TNT: 3x3 tai tam -> missile bay -> 3x3 tai muc tieu
        if (partnerType == EnumItemBoard.TNT)
        {
            var centerArea = affectedCells.FindAll(p => Mathf.Abs(p.x - cx) <= 1 && Mathf.Abs(p.y - cy) <= 1);
            var targetArea = affectedCells.FindAll(p => Mathf.Abs(p.x - cx) > 1 || Mathf.Abs(p.y - cy) > 1);

            var shake = CameraShakeService.Instance;
            shake?.ShakeTNT().Forget();

            await EffectPlayer.PlayWaveExplosionAsync(cx, cy, 1, centerArea, chainBoosters);

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
                await EffectPlayer.FlyMissileTrajectoryAsync(centerWorldPos, targetWorld, 0.25f);

                shake?.ShakeTNT().Forget();
                await EffectPlayer.PlayWaveExplosionAsync(targetX, targetY, 1, targetArea, chainBoosters);
            }

            await _manager.DelayExplosionAsync();
            return;
        }

        // 3. Missile + HorizontalRocket: rocket hang cy -> missile bay -> rocket hang muc tieu
        if (partnerType == EnumItemBoard.HorizontalRocket)
        {
            var row1Cells = affectedCells.FindAll(p => p.y == cy);
            var row2Cells = affectedCells.FindAll(p => p.y != cy);

            await EffectPlayer.PlayRocketRowAsync(cy, cx, centerWorldPos, row1Cells, chainBoosters);

            if (row2Cells.Count > 0)
            {
                int targetY = row2Cells[0].y;
                Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, cx, targetY);
                await EffectPlayer.FlyMissileTrajectoryAsync(centerWorldPos, targetWorld, 0.2f);
                await EffectPlayer.PlayRocketRowAsync(targetY, cx, targetWorld, row2Cells, chainBoosters);
            }

            await _manager.DelayExplosionAsync();
            return;
        }

        // 4. Missile + VerticalRocket: rocket cot cx -> missile bay -> rocket cot muc tieu
        if (partnerType == EnumItemBoard.VerticalRocket)
        {
            var col1Cells = affectedCells.FindAll(p => p.x == cx);
            var col2Cells = affectedCells.FindAll(p => p.x != cx);

            await EffectPlayer.PlayRocketColumnAsync(cx, cy, centerWorldPos, col1Cells, chainBoosters);

            if (col2Cells.Count > 0)
            {
                int targetX = col2Cells[0].x;
                Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, targetX, cy);
                await EffectPlayer.FlyMissileTrajectoryAsync(centerWorldPos, targetWorld, 0.2f);
                await EffectPlayer.PlayRocketColumnAsync(targetX, cy, targetWorld, col2Cells, chainBoosters);
            }

            await _manager.DelayExplosionAsync();
            return;
        }
    }

    // Xu ly hoat anh pha huy cho cac combo chua TNT (TNT+TNT hoac TNT+Rocket)
    public async UniTask PlayComboTNTAsync(
        EnumItemBoard partnerType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

        // 1. TNT + TNT: song no 5x5 quanh tam
        if (partnerType == EnumItemBoard.TNT)
        {
            var shake = CameraShakeService.Instance;
            shake?.ShakeMega().Forget();

            await EffectPlayer.PlayWaveExplosionAsync(cx, cy, 2, affectedCells, chainBoosters);
            await _manager.DelayExplosionAsync();
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
                    tasks.Add(EffectPlayer.PlayRocketRowAsync(row, cx, rowOrigin, affectedCells, chainBoosters));
                }
            }
            for (int dx = -1; dx <= 1; dx++)
            {
                int col = cx + dx;
                if (col >= 0 && col < board.Width)
                {
                    Vector3 colOrigin = GridUtils.GridToWorld(board.Grid, col, cy);
                    tasks.Add(EffectPlayer.PlayRocketColumnAsync(col, cy, colOrigin, affectedCells, chainBoosters));
                }
            }

            await UniTask.WhenAll(tasks);
            await _manager.DelayExplosionAsync();
            return;
        }
    }

    // Xu ly hoat anh pha huy cho cac combo Rocket + Rocket (HH+HH, VV+VV, HH+VV)
    public async UniTask PlayComboRocketRocketAsync(
        EnumItemBoard firstType,
        EnumItemBoard secondType,
        int cx, int cy,
        Vector3 centerWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

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
                    tasks.Add(EffectPlayer.PlayRocketRowAsync(row, cx, rowOrigin, affectedCells, chainBoosters));
                }
            }
            await UniTask.WhenAll(tasks);
            await _manager.DelayExplosionAsync();
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
                    tasks.Add(EffectPlayer.PlayRocketColumnAsync(col, cy, colOrigin, affectedCells, chainBoosters));
                }
            }
            await UniTask.WhenAll(tasks);
            await _manager.DelayExplosionAsync();
            return;
        }

        // 3. HH + VV: dau thap ban 1 hang va 1 cot dong thoi
        await UniTask.WhenAll(
            EffectPlayer.PlayRocketRowAsync(cy, cx, centerWorldPos, affectedCells, chainBoosters),
            EffectPlayer.PlayRocketColumnAsync(cx, cy, centerWorldPos, affectedCells, chainBoosters)
        );
        await _manager.DelayExplosionAsync();
    }
}
