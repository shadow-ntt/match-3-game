using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolBlockBreakEffect : Singleton<PoolBlockBreakEffect>
{
    // Danh sach 7 ParticleSystem prefab tuong ung 7 mau trong EnumItemColor
    [Header("7 Particle Prefabs (0: Red, 1: Blue, 2: Green, 3: Yellow, 4: Purple, 5: Orange, 6: Pink)")]
    [SerializeField] private ParticleSystem[] particlePrefabs = new ParticleSystem[7];

    [SerializeField] private int poolSize = 15;

    // Hang doi luu tru ParticleSystem theo tung mau
    private readonly Dictionary<EnumItemColor, Queue<ParticleSystem>> pools = new Dictionary<EnumItemColor, Queue<ParticleSystem>>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        Init();
    }

    // Tao san poolSize luong particle cho moi mau duoc gan prefab
    private void Init()
    {
        for (int c = 1; c <= 7; c++)
        {
            EnumItemColor color = (EnumItemColor)c;
            ParticleSystem prefab = GetPrefab(color);
            if (prefab == null) continue;

            if (!pools.TryGetValue(color, out var queue))
            {
                queue = new Queue<ParticleSystem>();
                pools[color] = queue;
            }

            for (int i = 0; i < poolSize; i++)
            {
                ParticleSystem ps = Instantiate(prefab, transform);
                ps.gameObject.SetActive(false);
                queue.Enqueue(ps);
            }
        }
    }

    // Lay dung prefab theo EnumItemColor, neu mau chua gan thi fallback ve prefab dau tien hop le
    private ParticleSystem GetPrefab(EnumItemColor color)
    {
        int index = (int)color - 1;
        if (particlePrefabs != null && index >= 0 && index < particlePrefabs.Length && particlePrefabs[index] != null)
        {
            return particlePrefabs[index];
        }

        // Fallback ve prefab dau tien co san
        if (particlePrefabs != null)
        {
            for (int i = 0; i < particlePrefabs.Length; i++)
            {
                if (particlePrefabs[i] != null) return particlePrefabs[i];
            }
        }

        return null;
    }

    // Phat hieu ung theo Transform goc va EnumItemColor
    public void Play(Transform origin, EnumItemColor color = EnumItemColor.Red)
    {
        if (origin != null)
        {
            Play(origin.position, color);
        }
    }

    // Phat hieu ung theo toa do Vector3 va EnumItemColor
    public void Play(Vector3 position, EnumItemColor color = EnumItemColor.Red)
    {
        // Kiem tra xem co pool chua, neu chua thi tao pool
        if (!pools.TryGetValue(color, out var queue))
        {
            queue = new Queue<ParticleSystem>();
            pools[color] = queue;
        }

        ParticleSystem ps = null;
        while (queue.Count > 0)
        {
            ps = queue.Dequeue();
            if (ps != null) break;
        }

        if (ps == null)
        {
            ParticleSystem prefab = GetPrefab(color);
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolBlockBreakEffect] Khong tim thay particle prefab cho mau {color}!");
                return;
            }
            ps = Instantiate(prefab, transform);
        }

        ps.transform.position = position;
        ps.gameObject.SetActive(true);
        ps.Play();

        // Tinh thoi gian ton tai cua hat de thu hoi ve dung pool cua mau do
        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
        StartCoroutine(OnFinishEffect(ps, color, duration));
    }

    // Phat hieu ung theo EnumItemBoard
    public void Play(Transform origin, EnumItemBoard boardItem)
    {
        if ((int)boardItem >= 1 && (int)boardItem <= 7)
        {
            Play(origin, (EnumItemColor)boardItem);
        }
    }

    public void Play(Vector3 position, EnumItemBoard boardItem)
    {
        if ((int)boardItem >= 1 && (int)boardItem <= 7)
        {
            Play(position, (EnumItemColor)boardItem);
        }
    }

    // Overload phat hieu ung bang index mau dang so nguyen (int)
    public void Play(Transform origin, int colorIndex)
    {
        EnumItemColor color = (EnumItemColor)Mathf.Clamp(colorIndex, 1, 7);
        Play(origin, color);
    }

    // Overload phat hieu ung bang toa do Vector3 va int color
    public void Play(Vector3 position, int colorIndex)
    {
        EnumItemColor color = (EnumItemColor)Mathf.Clamp(colorIndex, 1, 7);
        Play(position, color);
    }

    // Cho hieu ung chay xong roi thu hoi ve hang doi cua dung mau
    private IEnumerator OnFinishEffect(ParticleSystem ps, EnumItemColor color, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (ps != null)
        {
            ps.gameObject.SetActive(false);
            if (!pools.TryGetValue(color, out var queue))
            {
                queue = new Queue<ParticleSystem>();
                pools[color] = queue;
            }
            queue.Enqueue(ps);
        }
    }
}
