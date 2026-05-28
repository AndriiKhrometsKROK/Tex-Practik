using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> _instancePrefabs = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        ObjectPoolManager pool = GetOrCreateInstance();
        return pool.SpawnInternal(prefab, position, rotation);
    }

    public static void Return(GameObject instance)
    {
        if (instance == null) return;

        ObjectPoolManager pool = Instance;
        if (pool == null)
        {
            Destroy(instance);
            return;
        }

        pool.ReturnInternal(instance);
    }

    public static void Return(GameObject instance, float delay)
    {
        if (instance == null) return;
        if (delay <= 0f)
        {
            Return(instance);
            return;
        }

        ObjectPoolManager pool = GetOrCreateInstance();
        pool.StartCoroutine(pool.ReturnAfterDelay(instance, delay));
    }

    private static ObjectPoolManager GetOrCreateInstance()
    {
        if (Instance != null) return Instance;

        GameObject poolObject = new GameObject("ObjectPoolManager");
        return poolObject.AddComponent<ObjectPoolManager>();
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> pool = GetPool(prefab);
        GameObject instance = null;

        while (pool.Count > 0 && instance == null)
        {
            instance = pool.Dequeue();
        }

        if (instance == null)
        {
            instance = Instantiate(prefab, position, rotation);
            PooledObject pooledObject = instance.GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                pooledObject = instance.AddComponent<PooledObject>();
            }

            pooledObject.Initialize(prefab);
            _instancePrefabs[instance] = prefab;
        }
        else
        {
            Transform instanceTransform = instance.transform;
            instanceTransform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
        }

        instance.GetComponent<PooledObject>()?.MarkSpawned();
        return instance;
    }

    private void ReturnInternal(GameObject instance)
    {
        PooledObject pooledObject = instance.GetComponent<PooledObject>();
        if (pooledObject != null && pooledObject.IsInPool) return;

        GameObject prefab = null;
        if (pooledObject != null)
        {
            prefab = pooledObject.SourcePrefab;
        }

        if (prefab == null)
        {
            _instancePrefabs.TryGetValue(instance, out prefab);
        }

        if (prefab == null)
        {
            Destroy(instance);
            return;
        }

        pooledObject?.MarkReturned();
        instance.SetActive(false);
        instance.transform.SetParent(transform);
        GetPool(prefab).Enqueue(instance);
    }

    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }

        return pool;
    }

    private IEnumerator ReturnAfterDelay(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (instance != null && instance.activeInHierarchy)
        {
            ReturnInternal(instance);
        }
    }
}
