using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo TNT + Horizontal Rocket: Mega Rocket ban 3 hang ngang va 3 cot doc
public class TNTHorizontalRocketCombo : BoosterCombo
{
    public TNTHorizontalRocketCombo()
    {
        TypeA = EnumItemBoard.TNT;
        TypeB = EnumItemBoard.HorizontalRocket;
        ShakeType = CameraShakeType.TNT;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return (a == EnumItemBoard.TNT && b == EnumItemBoard.HorizontalRocket) ||
               (a == EnumItemBoard.HorizontalRocket && b == EnumItemBoard.TNT);
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        for (int dy = -1; dy <= 1; dy++)
        {
            BoosterComboGridUtils.AddRow(board, cy + dy, result);
        }
        for (int dx = -1; dx <= 1; dx++)
        {
            BoosterComboGridUtils.AddColumn(board, cx + dx, result);
        }

        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        var srA = objA.GetComponentInChildren<SpriteRenderer>();
        var srB = objB.GetComponentInChildren<SpriteRenderer>();
        var tasks = new List<UniTask>();

        if (srA != null) tasks.Add(srA.DOColor(Color.red, 0.05f).SetLoops(4, LoopType.Yoyo).ToUniTask());
        if (srB != null) tasks.Add(srB.DOColor(Color.red, 0.05f).SetLoops(4, LoopType.Yoyo).ToUniTask());

        var shakeA = objA.transform.DOShakePosition(0.2f, 0.1f, 15);
        var shakeB = objB.transform.DOShakePosition(0.2f, 0.1f, 15);
        tasks.Add(shakeA.ToUniTask());
        tasks.Add(shakeB.ToUniTask());
        await UniTask.WhenAll(tasks);

        var pulseA = objA.transform.DOScale(1.4f, 0.08f).SetLoops(2, LoopType.Yoyo);
        var pulseB = objB.transform.DOScale(1.4f, 0.08f).SetLoops(2, LoopType.Yoyo);
        await UniTask.WhenAll(pulseA.ToUniTask(), pulseB.ToUniTask());
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.EffectPlayer == null) return;

        CameraShakeService.Instance?.ShakeTNT().Forget();

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

        for (int dx = -1; dx <= 1; dx++)
        {
            int col = cx + dx;
            if (col >= 0 && col < board.Width)
            {
                tasks.Add(effectPlayer.PlayRocketColumnAsync(col, cy, affectedCells, chainBoosters));
            }
        }

        await UniTask.WhenAll(tasks);

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
