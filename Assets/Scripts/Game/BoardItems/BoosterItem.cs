using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Lop co so dai dien cho cac item Booster tren ban co (Rocket, TNT, Missile, LightBall)
public class BoosterItem : BoardItemBase, ISwappable, IBoosterActivatable, IDestructible
{
    private PoolBlockBreakEffect poolParticle;
    private int currentHp = 1;

    public bool CanSwap => true;

    public int CurrentHp => currentHp;
    public int MaxHp => 1;
    public bool IsDestroyed => currentHp <= 0;

    protected virtual void Start()
    {
        poolParticle = PoolBlockBreakEffect.Instance;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        currentHp = 1;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        currentHp = 1;
    }

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

    // Nhan sat thuong (bi kich no bi dong boi booster khac hoac match)
    public virtual UniTask<bool> TakeDamageAsync(int damage = 1)
    {
        currentHp -= damage;
        return UniTask.FromResult(IsDestroyed);
    }

    // Phat hoat anh bien mat khi bi tieu diet bi dong
    public virtual async UniTask PlayDestroyEffectAsync()
    {
        poolParticle.Play(transform.position, itemId);
        await transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack).ToUniTask();
    }
}
