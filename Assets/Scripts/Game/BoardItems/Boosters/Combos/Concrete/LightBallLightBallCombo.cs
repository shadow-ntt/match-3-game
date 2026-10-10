using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo LightBall + LightBall: Xoa toan bo tat ca cac vien tren ban co
public class LightBallLightBallCombo : BoosterCombo
{
    public LightBallLightBallCombo()
    {
        TypeA = EnumItemBoard.LightBall;
        TypeB = EnumItemBoard.LightBall;
        ShakeType = CameraShakeType.Mega;
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return a == EnumItemBoard.LightBall && b == EnumItemBoard.LightBall;
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        BoosterComboGridUtils.AddAllBoardItems(board, result);
        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        var moveOther = objA.transform.DOMove(objB.transform.position, 0.18f);
        var scaleOther = objA.transform.DOScale(0.2f, 0.18f);
        var scaleLB = objB.transform.DOScale(1.5f, 0.18f);
        var rotLB = objB.transform.DORotate(new Vector3(0, 0, 360f), 0.22f, RotateMode.FastBeyond360);
        await UniTask.WhenAll(moveOther.ToUniTask(), scaleOther.ToUniTask(), scaleLB.ToUniTask(), rotLB.ToUniTask());

        var flashLB = objB.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
        await flashLB.ToUniTask();
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.EffectPlayer == null) return;

        var board = context.Board;
        var manager = context.Manager;
        var affectedCells = context.AffectedCells;
        var chainBoosters = context.ChainBoosters;
        var centerWorldPos = context.CenterWorldPos;

        var shake = CameraShakeService.Instance;
        shake?.ShakeMega().Forget();

        await context.EffectPlayer.PlayLightBallBeamsAsync(centerWorldPos, affectedCells, Color.yellow);

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!chainBoosters.Contains(pos))
            {
                manager?.ExplodeAndRemoveCell(pos.x, pos.y);
                if (board.MidGrid != null && board.MidGrid[pos.x, pos.y] != null)
                {
                    manager?.ExplodeAndRemoveCell(pos.x, pos.y);
                }
            }
        }

        ExplodeRemainingAffectedCells(context);
        await DelayPostExplosionAsync(context);
    }
}
