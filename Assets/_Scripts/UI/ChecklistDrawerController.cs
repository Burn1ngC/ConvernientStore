using System.Collections;
using UnityEngine;
using TMPro;

public class ChecklistDrawerController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform drawer;          // ChecklistDrawer
    public RectTransform arrowGraphic;    // 可选：箭头图片
    public TMP_Text arrowText;            // 你现在用的 TMP 文字箭头

    [Header("Positions (anchored X)")]
    public float openX = 0f;
    public float closedX = -280f;         // 露出按钮宽度：320-40=280（按你按钮宽改）

    [Header("Animation")]
    public float duration = 0.18f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Start State")]
    public bool startOpen = false;

    bool isOpen;
    Coroutine co;

    void Awake()
    {
        if (drawer == null) drawer = (RectTransform)transform;
    }

    void Start()
    {
        isOpen = startOpen;
        SetInstant(isOpen);
        UpdateArrowVisual();
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (drawer == null) return;

        isOpen = open;
        UpdateArrowVisual();

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(AnimateTo(isOpen ? openX : closedX));
    }

    void SetInstant(bool open)
    {
        var p = drawer.anchoredPosition;
        p.x = open ? openX : closedX;
        drawer.anchoredPosition = p;
    }

    IEnumerator AnimateTo(float targetX)
    {
        float startX = drawer.anchoredPosition.x;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float k = ease.Evaluate(Mathf.Clamp01(t));

            var p = drawer.anchoredPosition;
            p.x = Mathf.Lerp(startX, targetX, k);
            drawer.anchoredPosition = p;

            yield return null;
        }

        var end = drawer.anchoredPosition;
        end.x = targetX;
        drawer.anchoredPosition = end;

        co = null;
    }

    void UpdateArrowVisual()
    {
        // 打开时应该显示“<”表示往左收回；关闭时显示“>”表示往右展开
        if (arrowText != null)
            arrowText.text = isOpen ? "<" : ">";

        // 如果你用的是图片箭头，就在这里旋转（可选）
        if (arrowGraphic != null)
        {
            arrowGraphic.localRotation = Quaternion.Euler(0, 0, isOpen ? 180f : 0f);
        }
    }
}
