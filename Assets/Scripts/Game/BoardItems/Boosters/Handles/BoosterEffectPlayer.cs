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
        int row,
        int originX,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null || row < 0 || row >= board.Height) return false;

        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            // Pha huy o tai goc ban neu khong phai booster day chuyen
            Vector2Int originPos = new Vector2Int(originX, row);
            if (board.IsInBounds(originX, row) && !chainBoosters.Contains(originPos))
            {
                _manager.ExplodeAndRemoveCell(originX, row);
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
                    _manager.ExplodeAndRemoveCell(cell.x, cell.y);
                });
                poolRocket.ReturnProjectile(projLeft);
            }

            async UniTask RunRightAsync()
            {
                if (projRight == null) return;
                await projRight.FlyAlongLineAsync(originWorldPos, exitRight, rightCells, board.Grid, (cell) =>
                {
                    if (chainBoosters.Contains(cell)) return;
                    _manager.ExplodeAndRemoveCell(cell.x, cell.y);
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
                    _manager.ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }
            return true;
        }
    }

    // Ban 1 cap dau dan rocket len va xuong tren 1 cot
    public async UniTask<bool> PlayRocketColumnAsync(
        int col,
        int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null || col < 0 || col >= board.Width) return false;

        var poolRocket = PoolRocketProjectile.Instance;
        if (poolRocket != null && poolRocket.HasPrefab)
        {
            // Pha huy o tai goc ban neu khong phai booster day chuyen
            Vector2Int originPos = new Vector2Int(col, originY);
            if (board.IsInBounds(col, originY) && !chainBoosters.Contains(originPos))
            {
                _manager.ExplodeAndRemoveCell(col, originY);
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
                    _manager.ExplodeAndRemoveCell(cell.x, cell.y);
                });
                poolRocket.ReturnProjectile(projDown);
            }

            async UniTask RunUpAsync()
            {
                if (projUp == null) return;
                await projUp.FlyAlongLineAsync(originWorldPos, exitUp, upCells, board.Grid, (cell) =>
                {
                    if (chainBoosters.Contains(cell)) return;
                    _manager.ExplodeAndRemoveCell(cell.x, cell.y);
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
                    _manager.ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }
            return true;
        }
    }

    // Hieu ung phong 2 dau dan ten lua ngang ve 2 phia trai va phai
    public async UniTask PlayHorizontalRocketEffectAsync(
        int originX, int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        bool usedFallback = await PlayRocketRowAsync(originY, originX, originWorldPos, affectedCells, chainBoosters);
        if (usedFallback) await _manager.DelayExplosionAsync();
    }

    // Hieu ung phong 2 dau dan ten lua doc ve 2 phia duoi va tren
    public async UniTask PlayVerticalRocketEffectAsync(
        int originX, int originY,
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        bool usedFallback = await PlayRocketColumnAsync(originX, originY, originWorldPos, affectedCells, chainBoosters);
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

    // Ban 1 ten lua Missile toi 1 toa do muc tieu va pha huy o do
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
        Vector3 originWorldPos,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters)
    {
        if (affectedCells.Count == 0) return;

        Vector2Int targetPos = affectedCells[0];
        await FlySingleMissileAsync(originWorldPos, targetPos, chainBoosters);
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
        List<Vector2Int> chainBoosters)
    {
        var board = _manager.Board;
        if (board == null) return;

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
                        beamColor = BoosterColorUtils.GetItemColor(item.ItemId);
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
                    _manager.ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }

            await _manager.DelayExplosionAsync();
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
                    _manager.ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }

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
