using UnityEngine;

public class MouseTouch : MonoBehaviour
{
    public static bool IsHoveringHighlight { get; private set; }

    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask highlightMask;   // 只勾选“可高亮物体”的Layer
    [SerializeField] private float rayDistance = 500f;

    private Outline currentOutline;
    private GameObject currentObj;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        // ✅ 兜底：不管Inspector怎么设置，开局全部关掉
        foreach (var o in FindObjectsOfType<Outline>(true))
            o.enabled = false;
    }

    void Update()
    {
        IsHoveringHighlight = false;

        if (cam == null)
        {
            Clear();
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out var hit, rayDistance, highlightMask, QueryTriggerInteraction.Ignore))
        {
            Clear();
            return;
        }

        var obj = hit.collider.gameObject;

        // 命中了可高亮层，才算 Hover
        IsHoveringHighlight = true;

        if (obj == currentObj) return;

        Clear();
        currentObj = obj;

        if (currentObj.TryGetComponent(out Outline outline))
        {
            currentOutline = outline;
            currentOutline.enabled = true;
        }
    }

    void Clear()
    {
        if (currentOutline != null) currentOutline.enabled = false;
        currentOutline = null;
        currentObj = null;
    }
}
