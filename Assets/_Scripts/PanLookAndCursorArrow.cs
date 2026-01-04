using UnityEngine;

public class PanLookAndCursorArrow : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // Main Camera
    public Transform rig;              // CameraRig（只做水平Yaw旋转 + 平移）
    public CameraMover mover;          // 负责MoveTo平移
    public WaypointGraph graph;        // WaypointGraph
    public Waypoint startWaypoint;     // 起始点

    [Header("Arrow Visual")]
    public Transform arrowRoot;        // 空物体，用来收纳箭头（可选）
    public GameObject arrowPrefab;     // 你的 Quad prefab
    public float arrowDistance = 1.2f; // 箭头离相机多远（水平距离）
    public float arrowHeight = 0.02f;  // 箭头离地高度
    public float arrowScale = 0.6f;    // 箭头缩放

    [Header("Raycast Masks")]
    public LayerMask floorMask = ~0;   // 地面层（先 Everything 排错）
    public float rayDistance = 500f;

    [Header("Look")]
    public float lookSensitivity = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Aim / Select")]
    public float aimMaxDistanceToHit = 1.8f; // 射线落点到Waypoint的最大距离
    [Range(-1f, 1f)]
    public float aimDotThreshold = 0.25f;    // 看向程度阈值（越大越严格）

    [Header("Behavior")]
    public bool hideArrowWhileMoving = true; // 移动/冷却时隐藏箭头

    Waypoint current;
    Waypoint aimed;
    GameObject arrowInstance;

    float yaw;   // 只作用于 rig 的 Y
    float pitch; // 只作用于 cam 的 local X

    bool isDragging = false;

    void Start()
    {
        // refs
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[PanLookAndCursorArrow] cam 为空：请把 Main Camera 拖进来，或给相机打 MainCamera Tag。");
            enabled = false;
            return;
        }

        if (rig == null)
        {
            // 常见结构：rig 是 cam 的父物体
            rig = cam.transform.parent;
            if (rig == null)
            {
                Debug.LogError("[PanLookAndCursorArrow] rig 为空：请创建 CameraRig 并把 Main Camera 放到它下面，然后把 rig 拖进来。");
                enabled = false;
                return;
            }
        }

        if (mover == null) mover = FindObjectOfType<CameraMover>();
        if (graph == null) graph = FindObjectOfType<WaypointGraph>();

        // init yaw/pitch（关键：yaw来自rig，pitch来自相机本地）
        yaw = NormalizeAngle(rig.eulerAngles.y);
        pitch = NormalizeAngle(cam.transform.localEulerAngles.x);

        // start waypoint
        current = startWaypoint;

        // spawn arrow
        if (arrowPrefab != null)
        {
            if (arrowRoot == null) arrowRoot = transform; // 没有root就挂在本物体下
            arrowInstance = Instantiate(arrowPrefab, arrowRoot);
            arrowInstance.name = "NavArrow";
            arrowInstance.transform.localScale = Vector3.one * arrowScale;
            arrowInstance.SetActive(false);
        }
    }

    void Update()
    {
        // 1) 右键拖动：普通“转头”
        HandleRightDragLook();

        // 2) 找当前瞄准的 waypoint（邻居才算）
        aimed = FindAimedNeighborWaypoint();

        // 3) 箭头显示与摆放
        UpdateArrow();

        // 4) 左键点击：跳转
        if (Input.GetMouseButtonDown(0))
        {
            TryMoveToAimed();
        }
    }

    void HandleRightDragLook()
    {
        // 按住右键才转
        if (Input.GetMouseButtonDown(1)) isDragging = true;
        if (Input.GetMouseButtonUp(1)) isDragging = false;
        if (!isDragging) return;

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw -= mx * lookSensitivity;     // ✅ 反转：右拖 => 往左转
        pitch += my * lookSensitivity;   // ✅ 反转：上拖 => 往上看

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // ✅ 水平：只转rig的Y，不碰X（不会绕圈那种“轨道感”）
        rig.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ✅ 垂直：只转相机本地X（像FPS转头）
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    Waypoint FindAimedNeighborWaypoint()
    {
        if (graph == null || graph.all == null || graph.all.Count == 0) return null;
        if (current == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, floorMask))
            return null;

        // 在所有 waypoint 里找离射线落点最近的一个
        Waypoint nearest = null;
        float best = float.MaxValue;

        foreach (var wp in graph.all)
        {
            if (wp == null) continue;

            // 只允许选邻居点（避免乱跳）
            if (current.neighbors != null && !current.neighbors.Contains(wp)) continue;

            float d = Vector3.Distance(hit.point, wp.transform.position);
            if (d < best)
            {
                best = d;
                nearest = wp;
            }
        }

        if (nearest == null) return null;
        if (best > aimMaxDistanceToHit) return null;

        // 再加一个“看向程度”判断（避免侧着也触发）
        Vector3 to = (nearest.transform.position - cam.transform.position);
        to.y = 0f;
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;

        if (to.sqrMagnitude < 0.0001f) return null;
        float dot = Vector3.Dot(fwd.normalized, to.normalized);

        if (dot < aimDotThreshold) return null;

        return nearest;
    }

    void UpdateArrow()
    {
        if (arrowInstance == null) return;

        bool busy = (mover != null && mover.IsBusy);
        bool shouldShow = aimed != null && (!hideArrowWhileMoving || !busy);

        arrowInstance.SetActive(shouldShow);
        if (!shouldShow) return;

        // 箭头放在相机前方的地面上
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 pos = cam.transform.position + forward * arrowDistance;

        // 把箭头贴到地面（用射线找地面高度）
        Ray down = new Ray(pos + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(down, out RaycastHit floorHit, 10f, floorMask))
            pos.y = floorHit.point.y + arrowHeight;
        else
            pos.y = rig.position.y + arrowHeight;

        arrowInstance.transform.position = pos;

        // 箭头朝向“目标点方向”
        Vector3 dir = (aimed.transform.position - pos);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            arrowInstance.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        arrowInstance.transform.localScale = Vector3.one * arrowScale;
    }

    void TryMoveToAimed()
    {
        if (aimed == null) return;
        if (mover == null)
        {
            Debug.LogError("[PanLookAndCursorArrow] mover 为空：请把 CameraMover 拖进来。");
            return;
        }

        if (mover.IsBusy) return; // 移动/冷却中不接新移动

        mover.MoveTo(aimed.transform);
        current = aimed;
    }

    static float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}
