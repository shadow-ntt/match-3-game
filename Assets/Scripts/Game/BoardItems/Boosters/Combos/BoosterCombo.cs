using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Lop co so truutuong cho tat ca cac loai Combo Booster (Pure C# Class)
public abstract class BoosterCombo
{
    public EnumItemBoard TypeA { get; protected set; } = EnumItemBoard.Blank;
    public EnumItemBoard TypeB { get; protected set; } = EnumItemBoard.Blank;
    public CameraShakeType ShakeType { get; protected set; } = CameraShakeType.None;
    public float PostExplosionDelay { get; protected set; } = -1f;

    // Kiem tra xem combo co khop voi cap booster (a, b) khong (khong phan biet thu tu)
    public virtual bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return (TypeA == a && TypeB == b) || (TypeA == b && TypeB == a);
    }

    // Tinh toan danh sach cac o bi anh huong tren ban co khi kich hoat combo tai tam (cx, cy)
    public abstract List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b);

    // Phat hoat anh hop nhat (merge) truoc khi kich hoat combo
    public abstract UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos);

    // Thuc thi hieu ung kich hoat, bay projectile, tia sang, vu no va xoa cac o bi anh huong
    public abstract UniTask ExecuteEffectAsync(BoosterComboContext context);

    // Tien ich dam bao khong bo sot bat ky o nao trong vung affectedCells
    protected void ExplodeRemainingAffectedCells(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.AffectedCells == null) return;

        var board = context.Board;
        var manager = context.Manager;
        var affected = context.AffectedCells;
        var chain = context.ChainBoosters;

        for (int i = 0; i < affected.Count; i++)
        {
            Vector2Int pos = affected[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (chain != null && chain.Contains(pos)) continue;
            if ((pos.x == context.XA && pos.y == context.YA) || (pos.x == context.XB && pos.y == context.YB)) continue;

            manager?.ExplodeAndRemoveCell(pos.x, pos.y);
        }
    }

    // Tien ich cho delay vu no
    protected async UniTask DelayPostExplosionAsync(BoosterComboContext context)
    {
        if (PostExplosionDelay > 0f)
        {
            await UniTask.Delay((int)(PostExplosionDelay * 1000));
        }
        else if (context?.Manager != null)
        {
            await context.Manager.DelayExplosionAsync();
        }
    }
}
