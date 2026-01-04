using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaserNPC : MonoBehaviour
{
    [Header("Refs")]
    public Transform chaseTarget;
    public Transform endPoint;
    public float endDistance = 1.2f;

    [Header("Chase")]
    [Tooltip("追得有多近才停（越小越贴）")]
    public float stopDistance = 0.08f;

    [Tooltip("把目标投影到 NavMesh 的半径（目标不在NavMesh上时很关键）")]
    public float targetSampleRadius = 6f;

    [Tooltip("离NavMesh边缘保持的安全距离（避免贴边）。建议 0.2~0.6")]
    public float keepAwayFromEdge = 0.35f;

    NavMeshAgent agent;
    ChaseSpawner spawner;

    public void Init(Transform target, Transform end, ChaseSpawner owner)
    {
        chaseTarget = target;
        endPoint = end;
        spawner = owner;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.autoBraking = false;
        agent.updateRotation = false;
        agent.isStopped = false;

        agent.stoppingDistance = stopDistance;
    }

    void Update()
    {
        if (chaseTarget == null) return;
        if (!agent.isOnNavMesh) return;

        // 1) 先把目标投影到 NavMesh
        Vector3 desired = chaseTarget.position;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
            desired = hit.position;
        else
            desired.y = transform.position.y;

        // 2) 再把目标从边缘往里推一点（避免贴边）
        if (NavMesh.FindClosestEdge(desired, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            // edgeHit.normal 指向“远离边界”的方向
            desired += edgeHit.normal * keepAwayFromEdge;

            // 推完再投影一次，保证仍在NavMesh上
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit2, 2f, NavMesh.AllAreas))
                desired = hit2.position;
        }

        // 3) 每帧追（最连续）
        agent.SetDestination(desired);

        // 4) 到终点结束
        if (endPoint != null)
        {
            if (Vector3.Distance(transform.position, endPoint.position) <= endDistance)
            {
                spawner?.EndChase();
                Destroy(gameObject);
            }
        }
    }
}
