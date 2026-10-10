using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo Missile + TNT: No 3x3 tai tam va no 3x3 tai muc tieu missile tim duoc
public class MissileTNTCombo : BoosterCombo
{
    public MissileTNTCombo()
    {
        TypeA = EnumItemBoard.Missile;
        TypeB = EnumItemBoard.TNT;
        ShakeType = CameraShakeType.TNT;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return (a == EnumItemBoard.Missile && b == EnumItemBoard.TNT) ||
               (a == EnumItemBoard.TNT && b == EnumItemBoard.Missile);
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        // 1. No vung 3x3 tai tam
        BoosterComboGridUtils.AddArea(board, cx - 1, cx + 1, cy - 1, cy + 1, result);

        // 2. Tim 1 muc tieu dac biet va no 3x3 tai do
        Vector2Int? target = BoosterComboGridUtils.FindSingleTarget(board, result);
        if (target.HasValue)
        {
            BoosterComboGridUtils.AddArea(board, target.Value.x - 1, target.Value.x + 1, target.Value.y - 1, target.Value.y + 1, result);
        }

        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        bool isAMissile = objA.GetComponent<MissileItem>() != null;
        GameObject missileObj = isAMissile ? objA : objB;
        GameObject tntObj = isAMissile ? objB : objA;

        Vector3 jumpPos = missileObj.transform.position + Vector3.up * 1.5f;
        var jumpUp = missileObj.transform.DOMove(jumpPos, 0.12f).SetEase(Ease.OutQuad);
        var scaleBase = tntObj.transform.DOScale(1.2f, 0.12f);
        await UniTask.WhenAll(jumpUp.ToUniTask(), scaleBase.ToUniTask());

        var slamDown = missileObj.transform.DOMove(centerPos, 0.12f).SetEase(Ease.InQuad);
        await slamDown.ToUniTask();

        var impact = tntObj.transform.DOScale(1.4f, 0.06f).SetLoops(2, LoopType.Yoyo);
        await impact.ToUniTask();
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.EffectPlayer == null) return;

        var board = context.Board;
        var effectPlayer = context.EffectPlayer;
        int cx = context.CenterX;
        int cy = context.CenterY;
        var centerWorldPos = context.CenterWorldPos;
        var affectedCells = context.AffectedCells;
        var chainBoosters = context.ChainBoosters;

        var centerArea = affectedCells.FindAll(p => Mathf.Abs(p.x - cx) <= 1 && Mathf.Abs(p.y - cy) <= 1);
        var targetArea = affectedCells.FindAll(p => Mathf.Abs(p.x - cx) > 1 || Mathf.Abs(p.y - cy) > 1);

        var shake = CameraShakeService.Instance;
        shake?.ShakeTNT().Forget();

        await effectPlayer.PlayWaveExplosionAsync(cx, cy, 1, centerArea, chainBoosters);

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
            await effectPlayer.FlyMissileTrajectoryAsync(centerWorldPos, targetWorld, 0.25f);

            shake?.ShakeTNT().Forget();
            await effectPlayer.PlayWaveExplosionAsync(targetX, targetY, 1, targetArea, chainBoosters);
        }

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
