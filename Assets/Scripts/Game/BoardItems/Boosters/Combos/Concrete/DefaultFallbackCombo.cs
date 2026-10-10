using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Combo du phong (fallback) khi khong tim thay cau hinh combo dac thu nao
public class DefaultFallbackCombo : BoosterCombo
{
    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return false;
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        BoosterComboGridUtils.AddArea(board, cx - 1, cx + 1, cy - 1, cy + 1, result);
        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        var defA = objA.transform.DOScale(1.3f, 0.1f).SetLoops(2, LoopType.Yoyo);
        var defB = objB.transform.DOScale(1.3f, 0.1f).SetLoops(2, LoopType.Yoyo);
        await UniTask.WhenAll(defA.ToUniTask(), defB.ToUniTask());
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null) return;

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
