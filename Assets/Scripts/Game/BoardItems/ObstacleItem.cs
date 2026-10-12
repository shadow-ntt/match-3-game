using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Lop dai dien cho chuong ngai vat tren ban co (Stone, Bush, Grass, Box...)
public class ObstacleItem : BoardItemBase, IDestructible
{
    [SerializeField] private int maxHp = 1;
    private int currentHp;
    private PoolBlockBreakEffect poolParticle;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDestroyed => currentHp <= 0;

    protected virtual void Awake()
    {
        currentHp = maxHp > 0 ? maxHp : 1;
    }

    protected virtual void Start()
    {
        poolParticle = PoolBlockBreakEffect.Instance;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        currentHp = maxHp > 0 ? maxHp : 1;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        currentHp = maxHp > 0 ? maxHp : 1;
    }

    // Nhan sat thuong, rung lac giat nhe neu con song
    public virtual async UniTask<bool> TakeDamageAsync(int damage = 1)
    {
        currentHp -= damage;
        if (currentHp > 0)
        {
            await transform.DOShakePosition(0.12f, 0.08f, 15).ToUniTask();
        }
        return IsDestroyed;
    }

    // Phat hoat anh bien mat va hat VFX
    public virtual async UniTask PlayDestroyEffectAsync()
    {
        poolParticle.Play(transform.position, itemId);
        await transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.InBack).ToUniTask();
    }
}
