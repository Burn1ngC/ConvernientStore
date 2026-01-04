using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [Tooltip("不填也行，会自动用 Main Camera")]
    public Camera cam;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // ✅ 正确：从自己指向相机
        Vector3 lookDir = cam.transform.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(lookDir);
    }
}
