using UnityEngine;
using TMPro;

public class ChecklistListUI : MonoBehaviour
{
    [Header("Refs")]
    public ShoppingListManager listManager;
    public RectTransform content;      // 必须是 ChecklistDrawer/Content
    public GameObject itemRowPrefab;   // ItemRowTMP prefab

    [Header("Options")]
    public bool generateOnStart = true;

    void Start()
    {
        if (generateOnStart)
        {
            if (listManager == null)
            {
                Debug.LogError("[ChecklistListUI] listManager is null.");
                return;
            }

            listManager.Generate();
            Rebuild();
        }
    }

    public void Rebuild()
    {
        if (listManager == null || content == null || itemRowPrefab == null)
        {
            Debug.LogError($"[ChecklistListUI] Missing refs. manager={listManager}, content={content}, prefab={itemRowPrefab}");
            return;
        }

        // 清空旧的（只删 prefab clone，不动你的 ToggleButton）
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        var list = listManager.GetCurrentList();
        Debug.Log($"[ChecklistListUI] Rebuild: {list.Count} items -> parent '{content.name}'");

        foreach (var item in list)
        {
            var go = Instantiate(itemRowPrefab, content);
            var t = go.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.text = item.displayName;
            else Debug.LogWarning("[ChecklistListUI] ItemRow prefab has no TMP_Text.");
        }
    }
}
