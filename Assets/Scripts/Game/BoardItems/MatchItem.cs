using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Dai dien cho ngoc thong thuong (Red, Blue, Green, Yellow, Purple, Orange, Pink)
public class MatchItem : BoardItemBase, IMatchable, ISwappable, IDestructible
{
    private PoolBlockBreakEffect poolParticle;
    private int currentHp = 1;

    public EnumItemBoard ColorId => itemId;
    public bool CanMatch => true;
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

    // Hoat anh phan hoi khi nam trong cum match duoc kich no
    public virtual async UniTask OnMatchedAsync()
    {
        await transform.DOScale(1.12f, 0.05f).SetEase(Ease.OutQuad).ToUniTask();
    }

    // Nhan sat thuong
    public virtual UniTask<bool> TakeDamageAsync(int damage = 1)
    {
        currentHp -= damage;
        return UniTask.FromResult(IsDestroyed);
    }

    // Phat hoat anh bien mat co rut ve 0 va hat VFX theo dung mau ngoc
    public virtual async UniTask PlayDestroyEffectAsync()
    {

        poolParticle.Play(transform.position, itemId);
        // Hoat anh co rut nhanh ve 0 truoc khi thu hoi ve pool
        await transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack).ToUniTask();
    }
}
