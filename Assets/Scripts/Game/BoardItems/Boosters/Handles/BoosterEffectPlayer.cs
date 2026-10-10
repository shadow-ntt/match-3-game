using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Xu ly toan bo hieu ung hoat anh bay dan (projectiles), tia sang (beams) va song no (blast waves)
public class BoosterEffectPlayer
{
    private readonly BoosterActivationManager _manager;

    public BoosterEffectPlayer(BoosterActivationManager manager)
    {
        _manager = manager;
    }

    // Ban 1 cap dau dan rocket trai va phai tren 1 hang
    public async UniTask<bool> PlayRocketRowAsync(
        int originX,
        int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null || originY < 0 || originY >= board.Height) return false;

        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            ExplodeOriginIfValid(originX, originY, chainBoosters);

            var leftCells = BoosterComboGridUtils.GetRayCells(board, originX, originY, Vector2Int.left);
            var rightCells = BoosterComboGridUtils.GetRayCells(board, originX, originY, Vector2Int.right);

            Vector3 originWorldPos = GridUtils.GridToWorld(board.Grid, originX, originY);
            //tăng thêm 2 đơn vị nữa để tăng cảm giác
            Vector3 exitLeft = GridUtils.GridToWorld(board.Grid, -2, originY);
            Vector3 exitRight = GridUtils.GridToWorld(board.Grid, board.Width + 2, originY);

            await UniTask.WhenAll(
                LaunchRocketBranchAsync(poolRocket, originWorldPos, exitLeft, leftCells, chainBoosters),
                LaunchRocketBranchAsync(poolRocket, originWorldPos, exitRight, rightCells, chainBoosters)
            );
            return false;
        }

        ExplodeCells(affectedCells, chainBoosters, pos => pos.y == originY);
        return true;
    }

    // Ban 1 cap dau dan rocket len va xuong tren 1 cot
    public async UniTask<bool> PlayRocketColumnAsync(
        int originX,
        int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null || originX < 0 || originX >= board.Width) return false;

        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            ExplodeOriginIfValid(originX, originY, chainBoosters);

            var downCells = BoosterComboGridUtils.GetRayCells(board, originX, originY, Vector2Int.down);
            var upCells = BoosterComboGridUtils.GetRayCells(board, originX, originY, Vector2Int.up);

            Vector3 originWorldPos = GridUtils.GridToWorld(board.Grid, originX, originY);
            //tăng thêm 2 đơn vị nữa để tăng cảm giác
            Vector3 exitDown = GridUtils.GridToWorld(board.Grid, originX, -2);
            Vector3 exitUp = GridUtils.GridToWorld(board.Grid, originX, board.Height + 2);

            await UniTask.WhenAll(
                LaunchRocketBranchAsync(poolRocket, originWorldPos, exitDown, downCells, chainBoosters),
                LaunchRocketBranchAsync(poolRocket, originWorldPos, exitUp, upCells, chainBoosters)
            );
            return false;
        }

        ExplodeCells(affectedCells, chainBoosters, pos => pos.x == originX);
        return true;
    }

    // Ban 1 dau dan rocket theo 1 nhanh tu diem ban toi diem thoat mep ban co
    private async UniTask LaunchRocketBranchAsync(
        PoolRocketProjectile pool,
        Vector3 startWorldPos,
        Vector3 exitWorldPos,
        List<Vector2Int> pathCells,
        List<Vector2Int> chainBoosters)
    {
        if (pool == null || !pool.HasPrefab) return;

        var proj = pool.GetProjectile();
        if (proj == null) return;

        var board = _manager.Board;
        Grid grid = board != null ? board.Grid : null;

        await proj.FlyAlongLineAsync(startWorldPos, exitWorldPos, pathCells, grid, (cell) =>
        {
            if (chainBoosters != null && chainBoosters.Contains(cell)) return;
            _manager.ExplodeAndRemoveCell(cell.x, cell.y);
        });

        pool.ReturnProjectile(proj);
    }

    // Pha huy o tai tam ban neu khong thuoc danh sach booster day chuyen
    private void ExplodeOriginIfValid(int originX, int originY, List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null || !board.IsInBounds(originX, originY)) return;

        Vector2Int originPos = new Vector2Int(originX, originY);
        if (chainBoosters == null || !chainBoosters.Contains(originPos))
        {
            _manager.ExplodeAndRemoveCell(originX, originY);
        }
    }

    // Kich no danh sach cac o thoa man dieu kien va khong thuoc booster day chuyen
    private void ExplodeCells(List<Vector2Int> cells, List<Vector2Int> chainBoosters, System.Predicate<Vector2Int> filter = null)
    {
        if (cells == null) return;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];
            if ((filter == null || filter(pos)) && (chainBoosters == null || !chainBoosters.Contains(pos)))
            {
                _manager.ExplodeAndRemoveCell(pos.x, pos.y);
            }
        }
    }

    // Hieu ung phong 2 dau dan ten lua ngang ve 2 phia trai va phai
    public async UniTask PlayHorizontalRocketEffectAsync(
        int originX, int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        bool usedFallback = await PlayRocketRowAsync(originX, originY, affectedCells, chainBoosters);
        if (usedFallback) await _manager.DelayExplosionAsync();
    }

    // Hieu ung phong 2 dau dan ten lua doc ve 2 phia duoi va tren
    public async UniTask PlayVerticalRocketEffectAsync(
        int originX, int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        bool usedFallback = await PlayRocketColumnAsync(originX, originY, affectedCells, chainBoosters);
        if (usedFallback) await _manager.DelayExplosionAsync();
    }

    // Ban 1 dau dan missile bay theo quy dao uon cong tu fromPos den toPos
    public async UniTask FlyMissileTrajectoryAsync(Vector3 fromPos, Vector3 toPos, float duration = 0.45f)
    {
        var poolMissile = PoolMissileProjectile.Instance;
        if (poolMissile != null && poolMissile.HasPrefab)
        {
            var proj = poolMissile.GetProjectile();
            if (proj != null)
            {
                await proj.FlyToTargetAsync(fromPos, toPos, duration);
                poolMissile.ReturnProjectile(proj);
            }
        }
    }

    // Ban 1 ten lua Missile toi 1 toa do muc tieu va pha huy o do (qua toa do o tren ban co)
    public UniTask FlySingleMissileAsync(
        int originX, int originY,
        Vector2Int targetPos,
        List<Vector2Int> chainBoosters,
        float duration = 0.45f)
    {
        var board = _manager.Board;
        Vector3 originWorldPos = board != null && board.Grid != null
            ? GridUtils.GridToWorld(board.Grid, originX, originY)
            : Vector3.zero;
        return FlySingleMissileAsync(originWorldPos, targetPos, chainBoosters, duration);
    }

    // Ban 1 ten lua Missile toi 1 toa do muc tieu va pha huy o do (qua toa do the gioi)
    public async UniTask FlySingleMissileAsync(
        Vector3 originWorldPos,
        Vector2Int targetPos,
        List<Vector2Int> chainBoosters,
        float duration = 0.45f)
    {
        var board = _manager.Board;
        if (board == null || !board.IsInBounds(targetPos.x, targetPos.y)) return;

        Vector3 targetWorld = GridUtils.GridToWorld(board.Grid, targetPos.x, targetPos.y);
        await FlyMissileTrajectoryAsync(originWorldPos, targetWorld, duration);

        if (!chainBoosters.Contains(targetPos))
        {
            _manager.ExplodeAndRemoveCell(targetPos.x, targetPos.y);
        }
    }

    // Hieu ung ten lua dan duong bay uon cong toi muc tieu
    public async UniTask PlayMissileEffectAsync(
        int originX, int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        if (affectedCells.Count == 0) return;

        Vector2Int targetPos = affectedCells[0];
        await FlySingleMissileAsync(originX, originY, targetPos, chainBoosters);
        await _manager.DelayExplosionAsync();
    }

    // Ban cac tia sang LineRenderer tu diem phat toi danh sach muc tieu
    public async UniTask PlayLightBallBeamsAsync(
        Vector3 originWorldPos,
        List<Vector2Int> beamTargets,
        Color beamColor)
    {
        if (beamTargets == null || beamTargets.Count == 0) return;

        var poolBeam = PoolLightBallBeam.Instance;
        if (poolBeam == null || !poolBeam.HasPrefab) return;

        var board = _manager.Board;
        if (board == null) return;

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

    // Hieu ung phat no lan toa tu LightBall den cac gem cung mau
    public async UniTask PlayLightBallEffectAsync(
        int originX, int originY,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters,
        Color beamColor = default)
    {
        var board = _manager.Board;
        if (board == null) return;

        if (beamColor == default) beamColor = Color.yellow;

        Vector2Int center = new Vector2Int(originX, originY);
        Vector3 originWorldPos = GridUtils.GridToWorld(board.Grid, originX, originY);

        var poolBeam = PoolLightBallBeam.Instance;
        if (poolBeam != null && poolBeam.HasPrefab)
        {
            // Ban cac tia sang dong loat tu LightBall toi tung gem muc tieu
            await PlayLightBallBeamsAsync(originWorldPos, affectedCells, beamColor);

            // Sau khi tia sang ban toi noi, phat no va xoa tat ca gem dong loat
            ExplodeCells(affectedCells, chainBoosters);

            await _manager.DelayExplosionAsync();
        }
        else
        {
            // Fallback khi chua co prefab: xoa tuan tu lan toa theo khoang cach
            var sortedCells = new List<Vector2Int>(affectedCells);
            BoosterComboGridUtils.SortByDistance(sortedCells, center);

            ExplodeCells(sortedCells, chainBoosters);

            await _manager.DelayExplosionAsync();
        }
    }

    // Phat no theo cac vong song lan toa tu tam ra ngoai
    public async UniTask PlayWaveExplosionAsync(
        int cx, int cy,
        int radius,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

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
                        _manager.ExplodeAndRemoveCell(pos.x, pos.y);
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
}
