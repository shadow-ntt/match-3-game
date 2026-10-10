using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo 2 Rocket Ngang (HH + HH): No 3 hang ngang dong thoi
public class HorizontalRocketCombo : BoosterCombo
{
    public HorizontalRocketCombo()
    {
        TypeA = EnumItemBoard.HorizontalRocket;
        TypeB = EnumItemBoard.HorizontalRocket;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return a == EnumItemBoard.HorizontalRocket && b == EnumItemBoard.HorizontalRocket;
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        for (int dy = -1; dy <= 1; dy++)
        {
            BoosterComboGridUtils.AddRow(board, cy + dy, result);
        }

        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        var moveA = objA.transform.DOMove(centerPos, 0.15f);
        var moveB = objB.transform.DOMove(centerPos, 0.15f);
        var scaleA = objA.transform.DOScale(0.5f, 0.15f);
        var scaleB = objB.transform.DOScale(0.5f, 0.15f);
        await UniTask.WhenAll(moveA.ToUniTask(), moveB.ToUniTask(), scaleA.ToUniTask(), scaleB.ToUniTask());

        var flashA = objA.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
        var flashB = objB.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
        await UniTask.WhenAll(flashA.ToUniTask(), flashB.ToUniTask());
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.EffectPlayer == null) return;

        var board = context.Board;
        var effectPlayer = context.EffectPlayer;
        int cx = context.CenterX;
        int cy = context.CenterY;
        var affectedCells = context.AffectedCells;
        var chainBoosters = context.ChainBoosters;

        var tasks = new List<UniTask>();
        for (int dy = -1; dy <= 1; dy++)
        {
            int row = cy + dy;
            if (row >= 0 && row < board.Height)
            {
                tasks.Add(effectPlayer.PlayRocketRowAsync(cx, row, affectedCells, chainBoosters));
            }
        }
        await UniTask.WhenAll(tasks);

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
