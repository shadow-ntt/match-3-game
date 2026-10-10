using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Lop co so dai dien cho cac item Booster tren ban co (Rocket, TNT, Missile, LightBall)
public class BoosterItem : NormalLayerItem, IBoosterActivatable
{
    public virtual bool CanActivate(int depth, IBoardItem swapTarget = null)
    {
        return true;
    }

    public virtual List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        return new List<Vector2Int>();
    }

    // Phat animation kich hoat co ban cua booster, cac class con co the override
    public virtual UniTask PlayActivationAnimationAsync()
    {
        return UniTask.CompletedTask;
    }

    // Thuc thi hieu ung kich hoat dac thu cua booster, class con override de goi BoosterEffectPlayer
    public virtual async UniTask ExecuteActivationEffectAsync(BoosterActivationContext context)
    {
        if (context?.EffectPlayer != null)
        {
            await context.EffectPlayer.PlayWaveExplosionAsync(context.X, context.Y, 1, context.AffectedCells, context.ChainBoosters);
        }
        if (context?.Manager != null)
        {
            await context.Manager.DelayExplosionAsync();
        }
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
