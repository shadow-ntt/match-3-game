using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo sieu no TNT + TNT (vung no 5x5 quanh tam)
public class TNTTNTCombo : BoosterCombo
{
    public TNTTNTCombo()
    {
        TypeA = EnumItemBoard.TNT;
        TypeB = EnumItemBoard.TNT;
        ShakeType = CameraShakeType.Mega;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return a == EnumItemBoard.TNT && b == EnumItemBoard.TNT;
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        BoosterComboGridUtils.AddArea(board, cx - 2, cx + 2, cy - 2, cy + 2, result);
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

        CameraShakeService.Instance?.ShakeMega().Forget();

        await context.EffectPlayer.PlayWaveExplosionAsync(
            context.CenterX,
            context.CenterY,
            2,
            context.AffectedCells,
            context.ChainBoosters
        );

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
