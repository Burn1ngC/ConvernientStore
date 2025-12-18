using UnityEngine;
// 如果你有 UI 并且想避免 UI 吃点击，可以取消注释下面两行
// using UnityEngine.EventSystems;

public class MouseNavigator : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                 // Main Camera
    public WaypointGraph graph;        // WaypointGraph 组件
    public CameraMover mover;          // CameraMover 组件（rig 指向 CameraRig）
    public Transform rigOverride;      // 可选：直接拖 CameraRig，留空则用 mover.rig

    [Header("Start")]
    public Waypoint startWaypoint;     // 建议拖 WP_01
    public bool followWaypointRotation = false;

    [Header("Step Range")]
    public float minStep = 1.2f;
    public float maxStep = 1.8f;

    [Header("Raycast")]
    public LayerMask waypointLayer;    // 勾选 Waypoint
    public float rayDistance = 300f;

    [Header("Right Mouse Look")]
    public float lookSensitivity = 3f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    public bool lockCursorWhileLooking = false;

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Runtime")]
    public Waypoint current;

    float yaw;
    float pitch;

    Transform Rig => rigOverride != null ? rigOverride : (mover != null ? mover.rig : null);

    void Start()
    {
        if (cam == null) cam = Camera.main;

        if (graph == null || mover == null || Rig == null)
        {
            Debug.LogError("[MouseNavigator] Missing references: graph / mover / rig.");
            enabled = false;
            return;
        }

        // 确保 graph 有点位
        if (graph.all == null || graph.all.Count == 0)
        {
            graph.Build();
            if (graph.all == null || graph.all.Count == 0)
            {
                Debug.LogError("[MouseNavigator] graph.all is empty. Check Waypoints Root in WaypointGraph (must be parent of ALL WP_XX).");
                enabled = false;
                return;
            }
        }

        if (startWaypoint != null) current = startWaypoint;
        if (current == null) current = graph.all[0];

        // 开局对齐位置（可选对齐朝向）
        var targetRot = followWaypointRotation ? current.transform.rotation : Rig.rotation;
        Rig.SetPositionAndRotation(current.transform.position, targetRot);

        // 初始化右键视角的 yaw/pitch
        var e = Rig.rotation.eulerAngles;
        pitch = NormalizeAngle(e.x);
        yaw = NormalizeAngle(e.y);

        if (debugLogs)
            Debug.Log($"[MouseNavigator] Start at '{current.name}', rigPos={Rig.position}, waypoints={graph.all.Count}");
    }

    void Update()
    {
        HandleRightMouseLook();

        // 右键按住时，不处理左键移动（避免一边转一边误点）
        if (Input.GetMouseButton(1)) return;

        if (Input.GetMouseButtonDown(0))
        {
            // 如果你有 UI 想阻止点击穿透，取消注释下面这段
            /*
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            */

            TryClickMove();
        }
    }

    void HandleRightMouseLook()
    {
        if (!Input.GetMouseButton(1) || Rig == null)
        {
            if (lockCursorWhileLooking && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        if (lockCursorWhileLooking && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // “扯画面/抓画布”模式：鼠标拖动方向 = 画面滑动方向
        // 而相机旋转会让画面反向滑动，所以这里要把输入取反
        yaw -= mx * lookSensitivity;   // 关键：X 反过来
        pitch += my * lookSensitivity;   // 这个配合“往上拖 -> 看到更下方(右下)”的直觉

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        Rig.rotation = Quaternion.Euler(pitch, yaw, 0f);

    }

    void TryClickMove()
    {
        if (cam == null || current == null || mover == null) return;

        // waypointLayer 如果没勾（=0），退回 Everything，避免你又忘了导致永远点不到
        int mask = (waypointLayer.value == 0) ? Physics.DefaultRaycastLayers : waypointLayer.value;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, mask))
        {
            if (debugLogs) Debug.Log("[MouseNavigator] Raycast hit nothing.");
            return;
        }

        Waypoint wp = hit.collider.GetComponent<Waypoint>();
        if (wp == null) wp = hit.collider.GetComponentInParent<Waypoint>();

        if (wp == null)
        {
            if (debugLogs) Debug.Log($"[MouseNavigator] Hit '{hit.collider.name}' but no Waypoint component found.");
            return;
        }

        if (wp == current) return;

        float d = Vector3.Distance(current.transform.position, wp.transform.position);
        if (d < minStep || d > maxStep)
        {
            if (debugLogs) Debug.Log($"[MouseNavigator] Blocked by step range. d={d:F2}, need {minStep:F1}~{maxStep:F1}");
            return;
        }

        if (debugLogs) Debug.Log($"[MouseNavigator] Move {current.name} -> {wp.name}, d={d:F2}");

        mover.MoveTo(wp.transform);
        current = wp;
    }

    static float NormalizeAngle(float a)
    {
        // 把 0..360 转成 -180..180，避免 pitch 初始化怪异
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}
