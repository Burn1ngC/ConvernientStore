using UnityEngine;

public class SimpleLook : MonoBehaviour
{
    public float sensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public bool holdRightMouse = true;

    float yaw;
    float pitch;

    void Start()
    {
        // 用相机当前角度作为起点
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
        if (pitch > 180f) pitch -= 360f;
    }

    void Update()
    {
        if (holdRightMouse && !Input.GetMouseButton(1)) return;

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw += mx * sensitivity;
        pitch -= my * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ✅ 只改旋转，不改位置 => 绝对不会绕圈
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
