using System.Collections.Generic;
using UnityEngine;
using Utils;

// Pool quan ly va tai su dung cac doi tuong tia sang LightBallBeam
public class PoolLightBallBeam : Singleton<PoolLightBallBeam>
{
    [SerializeField] private GameObject beamPrefab;
    [SerializeField] private int poolSize = 20;

    private readonly Queue<LightBallBeam> pool = new Queue<LightBallBeam>();

    public bool HasPrefab => beamPrefab != null;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        InitPool();
    }

    // Khoi tao truoc so luong beam trong pool
    private void InitPool()
    {
        if (beamPrefab == null || poolSize <= 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(beamPrefab, transform);
            obj.SetActive(false);
            if (obj.TryGetComponent<LightBallBeam>(out var beam))
            {
                pool.Enqueue(beam);
            }
        }
    }

    // Lay 1 LightBallBeam tu pool hoac tao moi neu thieu
    public LightBallBeam GetBeam()
    {
        if (beamPrefab == null) return null;

        LightBallBeam beam = null;
        while (pool.Count > 0)
        {
            beam = pool.Dequeue();
            if (beam != null) break;
        }

        if (beam == null)
        {
            GameObject obj = Instantiate(beamPrefab, transform);
            beam = obj.GetComponent<LightBallBeam>();
            if (beam == null) beam = obj.AddComponent<LightBallBeam>();
        }

        beam.gameObject.SetActive(true);
        return beam;
    }

    // Thu hoi LightBallBeam ve pool
    public void ReturnBeam(LightBallBeam beam)
    {
        if (beam == null) return;

        beam.gameObject.SetActive(false);
        beam.transform.SetParent(transform, false);
        pool.Enqueue(beam);
    }
}
