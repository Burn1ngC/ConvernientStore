using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Transform rig;         // CameraRig
    public float moveDuration = 0.6f;

    float baseY;
    Coroutine co;

    void Awake()
    {
        if (rig != null)
            baseY = rig.position.y;   // 只记录一次“地面高度”
    }

    public void MoveTo(Transform target)
    {
        if (target == null || rig == null) return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(MoveRoutine(target.position));
    }

    IEnumerator MoveRoutine(Vector3 targetPos)
    {
        Vector3 start = rig.position;

        // 🔒 永远锁 Y
        start.y = baseY;
        targetPos.y = baseY;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            rig.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }

        rig.position = targetPos;
    }
}
