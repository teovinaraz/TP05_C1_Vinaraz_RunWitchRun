using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private readonly Dictionary<GameObject, Stack<GameObject>> pools = new Dictionary<GameObject, Stack<GameObject>>();
    private readonly Transform root;

    public ObjectPool(Transform root)
    {
        this.root = root;
    }

    public GameObject Get(GameObject prefab, Vector3 position)
    {
        if (!pools.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pools[prefab] = stack;
        }

        GameObject instance = stack.Count > 0 ? stack.Pop() : null;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab, root);
            instance.AddComponent<PooledObject>().Source = prefab;
        }

        instance.transform.position = position;
        instance.SetActive(true);
        return instance;
    }

    public void Release(GameObject instance)
    {
        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null)
        {
            Object.Destroy(instance);
            return;
        }
        instance.SetActive(false);
        pools[pooled.Source].Push(instance);
    }
}
