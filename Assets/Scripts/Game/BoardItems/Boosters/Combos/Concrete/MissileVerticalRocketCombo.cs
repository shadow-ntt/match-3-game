using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo Missile + Vertical Rocket: No cot doc tai tam va cot doc tai muc tieu
public class MissileVerticalRocketCombo : BoosterCombo
{
    public MissileVerticalRocketCombo()
    {
        TypeA = EnumItemBoard.Missile;
        TypeB = EnumItemBoard.VerticalRocket;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return (a == EnumItemBoard.Missile && b == EnumItemBoard.VerticalRocket) ||
               (a == EnumItemBoard.VerticalRocket && b == EnumItemBoard.Missile);
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        BoosterComboGridUtils.AddColumn(board, cx, result);

        Vector2Int? target = BoosterComboGridUtils.FindSingleTarget(board, result);
        if (target.HasValue)
        {
            BoosterComboGridUtils.AddColumn(board, target.Value.x, result);
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

        var col1Cells = affectedCells.FindAll(p => p.x == cx);
        var col2Cells = affectedCells.FindAll(p => p.x != cx);

        await effectPlayer.PlayRocketColumnAsync(cx, cy, col1Cells, chainBoosters);

        if (col2Cells.Count > 0)
        {
            int targetX = col2Cells[0].x;
            Vector3 targetWorld = GridUtils.GridToWorld(context.Board.Grid, targetX, cy);
            await effectPlayer.FlyMissileTrajectoryAsync(centerWorldPos, targetWorld, 0.2f);
            await effectPlayer.PlayRocketColumnAsync(targetX, cy, col2Cells, chainBoosters);
        }

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
