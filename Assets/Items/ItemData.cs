using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "Item_")]
public class ItemData : ScriptableObject
{
    [Header("ID / Name")]
    public string id;
    public string displayName;

    // ✅ 统一给代码用的名字
    public string DisplayName => displayName;

    [Header("UI")]
    public Sprite icon;
    [TextArea(3, 10)]
    public string ingredientsText;

    [Header("Gameplay")]
    public bool isDecoy;
    public float rarityWeight = 1f;

    [Header("Optional")]
    public string[] tags;
    public int areaId; // 货架区域/分区(可选)
}
