using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Lop co so dai dien cho cac item Booster tren ban co (Rocket, TNT, Missile, LightBall)
public class BoosterItem : NormalLayerItem, IBoosterActivatable
{
    public virtual List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        return new List<Vector2Int>();
    }

    // Phat animation kich hoat co ban cua booster, cac class con co the override
    public virtual UniTask PlayActivationAnimationAsync()
    {
        return UniTask.CompletedTask;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
    }
}
