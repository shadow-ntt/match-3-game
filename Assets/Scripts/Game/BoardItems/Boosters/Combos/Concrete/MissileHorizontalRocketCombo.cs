using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo Missile + Horizontal Rocket: No hang ngang tai tam va hang ngang tai muc tieu
public class MissileHorizontalRocketCombo : BoosterCombo
{
    public MissileHorizontalRocketCombo()
    {
        TypeA = EnumItemBoard.Missile;
        TypeB = EnumItemBoard.HorizontalRocket;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return (a == EnumItemBoard.Missile && b == EnumItemBoard.HorizontalRocket) ||
               (a == EnumItemBoard.HorizontalRocket && b == EnumItemBoard.Missile);
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        BoosterComboGridUtils.AddRow(board, cy, result);

        Vector2Int? target = BoosterComboGridUtils.FindSingleTarget(board, result);
        if (target.HasValue)
        {
            BoosterComboGridUtils.AddRow(board, target.Value.y, result);
        }

        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        bool isAMissile = objA.GetComponent<MissileItem>() != null;
        GameObject missileObj = isAMissile ? objA : objB;
        GameObject rocketObj = isAMissile ? objB : objA;

        Vector3 jumpPos = missileObj.transform.position + Vector3.up * 1.5f;
        var jumpUp = missileObj.transform.DOMove(jumpPos, 0.12f).SetEase(Ease.OutQuad);
        var scaleBase = rocketObj.transform.DOScale(1.2f, 0.12f);
        await UniTask.WhenAll(jumpUp.ToUniTask(), scaleBase.ToUniTask());

        var slamDown = missileObj.transform.DOMove(centerPos, 0.12f).SetEase(Ease.InQuad);
        await slamDown.ToUniTask();

        var impact = rocketObj.transform.DOScale(1.4f, 0.06f).SetLoops(2, LoopType.Yoyo);
        await impact.ToUniTask();
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.EffectPlayer == null) return;

        var effectPlayer = context.EffectPlayer;
        int cx = context.CenterX;
        int cy = context.CenterY;
        var centerWorldPos = context.CenterWorldPos;
        var affectedCells = context.AffectedCells;
        var chainBoosters = context.ChainBoosters;

        var row1Cells = affectedCells.FindAll(p => p.y == cy);
        var row2Cells = affectedCells.FindAll(p => p.y != cy);

        await effectPlayer.PlayRocketRowAsync(cx, cy, row1Cells, chainBoosters);

        if (row2Cells.Count > 0)
        {
            int targetY = row2Cells[0].y;
            Vector3 targetWorld = GridUtils.GridToWorld(context.Board.Grid, cx, targetY);
            await effectPlayer.FlyMissileTrajectoryAsync(centerWorldPos, targetWorld, 0.2f);
            await effectPlayer.PlayRocketRowAsync(cx, targetY, row2Cells, chainBoosters);
        }

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
