using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController_ChaseOnly : MonoBehaviour
{
    [Header("Chase Target (set by spawner)")]
    public Transform chaseTarget;          // 追逐目标：建议是你的 CameraRig / rig
    public Transform chaseEndPoint;        // 追逐结束点：ChaseEndPoint
    public float endDistance = 1.2f;       // 离终点多近算到达

    [Header("Chase Tuning")]
    public float chaseSpeed = 7f;
    public float stoppingDistance = 0.08f;

    [Tooltip("目标不在NavMesh上时，用这个半径投影到NavMesh")]
    public float targetSampleRadius = 6f;

    [Tooltip("把目标点从边缘往里推，减少贴边。0.2~0.6 之间试")]
    public float keepAwayFromEdge = 0.35f;

    [Header("Optional: Catch player")]
    public bool killOnCatch = false;
    public float catchDistance = 0.8f;

    private NavMeshAgent agent;
    private ChaseSpawner spawner; // 用于回调 EndChase（可选）

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // 追逐更顺
        agent.autoBraking = false;
        agent.updateRotation = false;   // 你用公告板Visual面对相机，所以不让agent转
    }

    // ✅ 给 spawner 调用
    public void Init(Transform target, Transform endPoint, ChaseSpawner owner)
    {
        chaseTarget = target;
        chaseEndPoint = endPoint;
        spawner = owner;

        agent.speed = chaseSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;
    }

    void Update()
    {
        if (chaseTarget == null) return;
        if (!agent.isOnNavMesh) return;

        // 1) 目标点投影到NavMesh（否则会出现“永远差一截”）
        Vector3 desired = chaseTarget.position;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
            desired = hit.position;
        else
            desired.y = transform.position.y;

        // 2) 从边缘往里推（减少贴边）
        if (NavMesh.FindClosestEdge(desired, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            desired += edgeHit.normal * keepAwayFromEdge;
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit2, 2f, NavMesh.AllAreas))
                desired = hit2.position;
        }

        agent.SetDestination(desired);

        // 3) 到终点就结束
        if (chaseEndPoint != null)
        {
            if (Vector3.Distance(transform.position, chaseEndPoint.position) <= endDistance)
            {
                spawner?.EndChase();
                Destroy(gameObject);
                return;
            }
        }

        // 4)（可选）追上就判定死亡/结束
        if (killOnCatch && Vector3.Distance(transform.position, chaseTarget.position) <= catchDistance)
        {
            spawner?.EndChase();
            Destroy(gameObject);
        }
    }
}
