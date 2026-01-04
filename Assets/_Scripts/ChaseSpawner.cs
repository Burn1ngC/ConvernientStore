using UnityEngine;
using UnityEngine.AI;

public class ChaseSpawner : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("追逐目标。建议留空，让它自动找 CameraMover.rig；找不到再用 Main Camera。")]
    public Transform chaseTarget;

    [Tooltip("追逐 NPC Prefab（根物体上要有 NavMeshAgent + EnemyController_ChaseOnly）")]
    public GameObject chaserPrefab;

    [Tooltip("固定刷新点（场景中的 ChaseSpawnPoint）")]
    public Transform fixedSpawnPoint;

    [Tooltip("追逐结束点（场景中的 ChaseEndPoint）")]
    public Transform chaseEndPoint;

    [Header("NavMesh Spawn")]
    [Tooltip("在刷新点附近找 NavMesh 的半径（建议 4~12）")]
    public float sampleRadius = 8f;

    [Tooltip("限制吸附偏移：如果 SamplePosition 找到的点离刷新点太远，就不生成（避免刷到终点附近）")]
    public float maxSnapDistance = 3f;

    [Tooltip("生成后把位置从 NavMesh 边缘往里推一点，减少贴边生成（0.2~0.8）")]
    public float spawnPushFromEdge = 0.5f;

    [Header("Run Mode")]
    [Tooltip("只允许触发一次追逐")]
    public bool spawnOnlyOnce = true;

    private bool spawnedOnce = false;
    private GameObject aliveNpc;

    void Awake()
    {
        AutoPickChaseTargetIfNull();
    }

    void AutoPickChaseTargetIfNull()
    {
        if (chaseTarget != null) return;

        // 1) 优先追 rig（真正移动的点）
        var mover = FindObjectOfType<CameraMover>();
        if (mover != null && mover.rig != null)
        {
            chaseTarget = mover.rig;
            return;
        }

        // 2) 兜底：追主相机
        if (Camera.main != null)
        {
            chaseTarget = Camera.main.transform;
        }
    }

    /// <summary>
    /// 给 UI 调用：只触发一次追逐
    /// </summary>
    public void TriggerChaseOnce()
    {
        if (spawnOnlyOnce && spawnedOnce) return;
        spawnedOnce = true;

        StartChase();
    }

    /// <summary>
    /// 开始追逐（也可用于调试手动调用）
    /// </summary>
    public void StartChase()
    {
        AutoPickChaseTargetIfNull();

        if (chaseTarget == null)
        {
            Debug.LogError("[ChaseSpawner] chaseTarget 为空：请拖 Main Camera 或 CameraMover.rig。");
            return;
        }

        if (chaserPrefab == null)
        {
            Debug.LogError("[ChaseSpawner] chaserPrefab 为空：请拖你的追逐NPC prefab。");
            return;
        }

        if (fixedSpawnPoint == null)
        {
            Debug.LogError("[ChaseSpawner] fixedSpawnPoint 为空：请创建 ChaseSpawnPoint 并拖进来。");
            return;
        }

        // 清理旧NPC（防重复）
        if (aliveNpc != null) Destroy(aliveNpc);

        // 1) 固定刷新点
        Vector3 raw = fixedSpawnPoint.position;

        // 2) 在刷新点附近找 NavMesh（从上方一点更稳）
        Vector3 query = raw + Vector3.up * 2f;

        // ✅ 关键：先声明 hit，避免作用域报错
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(query, out hit, sampleRadius, NavMesh.AllAreas))
        {
            Debug.LogError("[ChaseSpawner] 刷新点附近找不到 NavMesh：请确认 Bake 了 NavMesh，并把 SpawnPoint 放到蓝色可走区域上。");
            return;
        }

        float snapDist = Vector3.Distance(hit.position, raw);
        if (snapDist > maxSnapDistance)
        {
            Debug.LogError($"[ChaseSpawner] NavMesh 吸附点离 SpawnPoint 太远({snapDist:F2}m)。请把 SpawnPoint 放到可走区域或减小 sampleRadius。");
            return;
        }

        Vector3 spawnPos = hit.position;

        // 3) 生成位置从边缘往里推一点，减少贴边生成
        if (spawnPushFromEdge > 0f)
        {
            NavMeshHit edgeHit;
            if (NavMesh.FindClosestEdge(spawnPos, out edgeHit, NavMesh.AllAreas))
            {
                Vector3 pushed = spawnPos + edgeHit.normal * spawnPushFromEdge;

                NavMeshHit hit2;
                if (NavMesh.SamplePosition(pushed, out hit2, 2f, NavMesh.AllAreas))
                {
                    spawnPos = hit2.position;
                }
            }
        }

        // 4) 实例化
        aliveNpc = Instantiate(chaserPrefab, spawnPos, Quaternion.identity);

        // 5) 强制放上 NavMesh（更稳）
        var agent = aliveNpc.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(spawnPos);
        }
        else
        {
            Debug.LogError("[ChaseSpawner] prefab 上缺少 NavMeshAgent！");
        }

        // 6) 初始化追逐脚本（配套 EnemyController_ChaseOnly）
        var ctrl = aliveNpc.GetComponent<EnemyController_ChaseOnly>();
        if (ctrl != null)
        {
            ctrl.Init(chaseTarget, chaseEndPoint, this);
        }
        else
        {
            Debug.LogError("[ChaseSpawner] prefab 上没有 EnemyController_ChaseOnly 脚本！");
        }
    }

    public void EndChase()
    {
        Debug.Log("[ChaseSpawner] Chase ended.");
        // 你后面要接剧情/音效/解锁按钮等就写这里
    }

    // 可选：重置追逐（用于重开）
    public void ResetChase()
    {
        spawnedOnce = false;

        if (aliveNpc != null) Destroy(aliveNpc);
        aliveNpc = null;
    }
}
