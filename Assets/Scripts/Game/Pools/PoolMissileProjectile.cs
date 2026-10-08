using System.Collections.Generic;
using UnityEngine;
using Utils;

// Pool quan ly va tai su dung cac doi tuong ten lua MissileProjectile
public class PoolMissileProjectile : Singleton<PoolMissileProjectile>
{
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private int poolSize = 4;

    private readonly Queue<MissileProjectile> pool = new Queue<MissileProjectile>();

    public bool HasPrefab => missilePrefab != null;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        InitPool();
    }

    // Khoi tao truoc so luong projectile
    private void InitPool()
    {
        if (missilePrefab == null || poolSize <= 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(missilePrefab, transform);
            obj.SetActive(false);
            if (obj.TryGetComponent<MissileProjectile>(out var proj))
            {
                pool.Enqueue(proj);
            }
        }
    }

    // Lay 1 MissileProjectile tu pool hoac khoi tao moi neu thieu
    public MissileProjectile GetProjectile()
    {
        if (missilePrefab == null) return null;

        MissileProjectile proj = null;
        while (pool.Count > 0)
        {
            proj = pool.Dequeue();
            if (proj != null) break;
        }

        if (proj == null)
        {
            GameObject obj = Instantiate(missilePrefab, transform);
            proj = obj.GetComponent<MissileProjectile>();
            if (proj == null) proj = obj.AddComponent<MissileProjectile>();
        }

        proj.gameObject.SetActive(true);
        return proj;
    }

    // Thu hoi MissileProjectile ve pool
    public void ReturnProjectile(MissileProjectile proj)
    {
        if (proj == null) return;

        proj.gameObject.SetActive(false);
        proj.transform.SetParent(transform, false);
        pool.Enqueue(proj);
    }
}
