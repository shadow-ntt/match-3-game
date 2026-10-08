using System.Collections.Generic;
using UnityEngine;
using Utils;

// Pool quan ly va tai su dung cac doi tuong dau dan ten lua RocketProjectile
public class PoolRocketProjectile : Singleton<PoolRocketProjectile>
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int poolSize = 8;

    private readonly Queue<RocketProjectile> pool = new Queue<RocketProjectile>();

    public bool HasPrefab => projectilePrefab != null;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        InitPool();
    }

    // Khoi tao truoc so luong projectile
    private void InitPool()
    {
        if (projectilePrefab == null || poolSize <= 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(projectilePrefab, transform);
            obj.SetActive(false);
            if (obj.TryGetComponent<RocketProjectile>(out var proj))
            {
                pool.Enqueue(proj);
            }
        }
    }

    // Lay 1 RocketProjectile tu pool hoac khoi tao moi neu thieu
    public RocketProjectile GetProjectile()
    {
        if (projectilePrefab == null) return null;

        RocketProjectile proj = null;
        while (pool.Count > 0)
        {
            proj = pool.Dequeue();
            if (proj != null) break;
        }

        if (proj == null)
        {
            GameObject obj = Instantiate(projectilePrefab, transform);
            proj = obj.GetComponent<RocketProjectile>();
            if (proj == null) proj = obj.AddComponent<RocketProjectile>();
        }

        proj.gameObject.SetActive(true);
        return proj;
    }

    // Thu hoi RocketProjectile ve pool
    public void ReturnProjectile(RocketProjectile proj)
    {
        if (proj == null) return;

        proj.gameObject.SetActive(false);
        proj.transform.SetParent(transform, false);
        pool.Enqueue(proj);
    }
}
