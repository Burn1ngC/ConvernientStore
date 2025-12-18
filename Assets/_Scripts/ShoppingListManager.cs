using System.Collections.Generic;
using UnityEngine;

public class ShoppingListManager : MonoBehaviour
{
    public List<ItemData> allItems = new();
    public int pickCount = 3;

    private readonly List<ItemData> current = new();

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        current.Clear();
        if (allItems == null || allItems.Count == 0) return;

        // 简单随机不重复
        var pool = new List<ItemData>(allItems);
        for (int i = 0; i < pickCount && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            current.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
    }

    public List<ItemData> GetCurrentList()
    {
        return current;
    }
}
