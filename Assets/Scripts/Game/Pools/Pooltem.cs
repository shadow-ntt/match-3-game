using System.Collections.Generic;
using UnityEngine;

public class Pooltem : Singleton<Pooltem>
{
    [Header("Pool Settings")]
    [SerializeField] private Transform poolParent;
    [SerializeField] private int amount = 25;

    [Header("Board Item Prefabs")]
    [SerializeField] private List<GameObject> boardItemPrefabs = new List<GameObject>();

    // Hang doi luu tru GameObject theo ItemId
    private readonly Dictionary<int, Queue<GameObject>> pool = new Dictionary<int, Queue<GameObject>>();

    // Theo doi cac GameObject dang hoat dong
    private readonly HashSet<GameObject> activeItems = new HashSet<GameObject>();

    // Map ghi nho ItemId cua tung GameObject khi spawn
    private readonly Dictionary<GameObject, int> objectToIdMap = new Dictionary<GameObject, int>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (poolParent == null)
        {
            poolParent = transform;
        }

        PrewarmPool();
    }

    // Khoi tao truoc cac prefab vao pool
    private void PrewarmPool()
    {
        if (amount <= 0 || boardItemPrefabs == null) return;

        for (int i = 0; i < boardItemPrefabs.Count; i++)
        {
            var prefab = boardItemPrefabs[i];
            if (prefab == null) continue;

            // TryGet IBoardItem, khong co thi bo qua
            if (!prefab.TryGetComponent<IBoardItem>(out var boardItem))
            {
                continue;
            }

            int id = (int)boardItem.ItemId;
            if (!pool.TryGetValue(id, out var queue))
            {
                queue = new Queue<GameObject>();
                pool[id] = queue;
            }

            for (int j = 0; j < amount; j++)
            {
                GameObject obj = Instantiate(prefab, poolParent);
                obj.SetActive(false);
                objectToIdMap[obj] = id;
                queue.Enqueue(obj);
            }
        }
    }

    // Tim prefab dua vao IBoardItem va ItemId
    private GameObject GetPrefab(int id)
    {
        if (boardItemPrefabs == null || boardItemPrefabs.Count == 0) return null;

        for (int i = 0; i < boardItemPrefabs.Count; i++)
        {
            var prefab = boardItemPrefabs[i];
            if (prefab == null) continue;

            // TryGet IBoardItem, khong duoc thi bo qua
            if (!prefab.TryGetComponent<IBoardItem>(out var boardItem))
            {
                continue;
            }

            // Neu duoc thi dua vao ItemId cua item ma lay
            if ((int)boardItem.ItemId == id)
            {
                return prefab;
            }
        }

        return null;
    }

    // ==========================================
    // SPAWN / GET BOARD ITEM
    // ==========================================

    // Lay BoardItem tu pool theo ItemId
    public GameObject GetBoardItem(int id, Transform parent = null)
    {
        GameObject prefab = GetPrefab(id);
        if (prefab == null)
        {
            Debug.LogWarning($"[PoolObject] Khong tim thay prefab co IBoardItem voi ItemId: {id}!");
            return null;
        }

        if (!pool.TryGetValue(id, out var queue))
        {
            queue = new Queue<GameObject>();
            pool[id] = queue;
        }

        GameObject obj = null;
        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj != null) break;
        }

        if (obj == null)
        {
            obj = Instantiate(prefab, parent != null ? parent : poolParent);
        }
        else if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }

        objectToIdMap[obj] = id;
        activeItems.Add(obj);
        obj.SetActive(true);

        if (obj.TryGetComponent<IBoardItem>(out var boardItem))
        {
            boardItem.OnSpawn();
        }

        return obj;
    }

    // Spawn BoardItem voi position, rotation va parent
    public GameObject SpawnBoardItem(int id, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = GetBoardItem(id, parent);
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        return obj;
    }

    // RETURN / DESPAWN BOARD ITEM
    // Tra GameObject ve pool
    public void ReturnBoardItem(GameObject item)
    {
        if (item == null) return;

        if (item.TryGetComponent<IBoardItem>(out var boardItem))
        {
            boardItem.OnDespawn();
        }

        item.SetActive(false);
        item.transform.SetParent(poolParent, false);

        activeItems.Remove(item);

        int id = objectToIdMap.TryGetValue(item, out int savedId) ? savedId : 0;
        if (!pool.TryGetValue(id, out var queue))
        {
            queue = new Queue<GameObject>();
            pool[id] = queue;
        }
        queue.Enqueue(item);
    }

    // Overload tien dung cho Component
    public void ReturnBoardItem(Component item)
    {
        if (item != null)
        {
            ReturnBoardItem(item.gameObject);
        }
    }

    // Thu hoi toan bo board items dang active ve pool
    public void ReturnAll()
    {
        var list = new List<GameObject>(activeItems);
        for (int i = 0; i < list.Count; i++)
        {
            ReturnBoardItem(list[i]);
        }
    }
}
