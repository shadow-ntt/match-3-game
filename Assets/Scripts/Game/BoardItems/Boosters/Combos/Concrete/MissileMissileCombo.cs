using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo Missile + Missile: Ban 3 qua ten lua tu tim muc tieu
public class MissileMissileCombo : BoosterCombo
{
    public MissileMissileCombo()
    {
        TypeA = EnumItemBoard.Missile;
        TypeB = EnumItemBoard.Missile;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return a == EnumItemBoard.Missile && b == EnumItemBoard.Missile;
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        BoosterComboGridUtils.AddSpecialTargets(board, 3, result);
        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        var rotA = objA.transform.DORotate(new Vector3(0, 0, 360f), 0.15f, RotateMode.FastBeyond360);
        var rotB = objB.transform.DORotate(new Vector3(0, 0, -360f), 0.15f, RotateMode.FastBeyond360);
        var scaleA = objA.transform.DOScale(1.3f, 0.08f).SetLoops(2, LoopType.Yoyo);
        var scaleB = objB.transform.DOScale(1.3f, 0.08f).SetLoops(2, LoopType.Yoyo);
        await UniTask.WhenAll(rotA.ToUniTask(), rotB.ToUniTask(), scaleA.ToUniTask(), scaleB.ToUniTask());
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.EffectPlayer == null) return;

        var effectPlayer = context.EffectPlayer;
        var centerWorldPos = context.CenterWorldPos;
        var affectedCells = context.AffectedCells;
        var chainBoosters = context.ChainBoosters;

        var tasks = new List<UniTask>();
        for (int i = 0; i < affectedCells.Count; i++)
        {
            tasks.Add(effectPlayer.FlySingleMissileAsync(centerWorldPos, affectedCells[i], chainBoosters));
        }
        await UniTask.WhenAll(tasks);

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
