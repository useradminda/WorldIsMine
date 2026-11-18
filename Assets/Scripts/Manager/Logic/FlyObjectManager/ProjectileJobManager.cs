using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ZTools;

public class ProjectileJobManager : Singleton<ProjectileJobManager>, IManager
{
    // 箭类投射物固定经过的世界高度，保证近距离射击也能看到高抛轨迹。
    private const float ArrowApexY = 20f;
    // 当起点和目标都高于弧顶配置时，仍保留最小抛物线高度。
    private const float ArrowMinimumArcHeight = 2f;

    // 最大箭数量
    private const int MaxProjectileCount = 10000;

    // 状态
    private const byte StateFree = 0;
    private const byte StateFlying = 1;

    // ----------------------------------------
    // Projectile 数据
    // ----------------------------------------

    private NativeArray<ProjectileData> projectiles;

    // 当前正在飞行的箭
    private NativeList<int> activeProjectileIds;

    // Job产生的到达箭列表
    private NativeList<int> arrivedProjectileIds;

    private Dictionary<int, Transform> projectileTransformsDic = new Dictionary<int, Transform>();

    private bool initState;


    // ========================================
    // 发射
    // ========================================

    public int SpawnProjectile(
        int ownerUnitIndex,
        int targetUnitIndex,
        float3 startPosition,
        float3 targetPosition,
        float speed,
        int tarUid,
        int damage,
        int flyUIndex,
        Transform flyTrans,
        string flyType
        )
    {
        if (!initState)
            return -1;

        // 找空闲槽位
        for (int i = 0;
             i < MaxProjectileCount;
             i++)
        {
            if (projectiles[i].State != StateFree)
                continue;

            float3 offset = targetPosition - startPosition;

            float3 direction = math.normalizesafe(offset);

            int activeIndex = activeProjectileIds.Length;

            float distance = math.distance(startPosition, targetPosition);

            float totalTime = distance / speed;


            float arcHeight = 1;

            if (flyType == "arrow")
            {
               // 旧逻辑：弧高跟随水平距离变化，近距离时弧顶过低，容易被单位或城墙遮挡。
               // arcHeight = math.clamp(distance * 0.2f, 2f, 10f);
               arcHeight = CalculateArrowArcHeight(startPosition, targetPosition);
            }
            else if(flyType == "stone")
            {
                arcHeight = math.clamp(distance * 0.3f, 4f, 15f);
            }

            activeProjectileIds.Add(i);

            projectiles[i] =
                new ProjectileData
                {
                    Index = i,

                    OwnerUnitIndex = ownerUnitIndex,

                    TargetUnitIndex = targetUnitIndex,

                    StartPosition = startPosition,

                    TargetPosition = targetPosition,

                    Position = startPosition,

                    Direction = math.normalizesafe(offset),

                    Speed = speed,

                    TotalTime = totalTime,

                    ArcHeight = arcHeight,

                    Progress = 0f,

                    Damage = damage,

                    State = StateFlying,

                    ActiveListIndex = activeIndex,

                    TargetUId = tarUid,

                    FlyUIndex = flyUIndex,
                };

            projectileTransformsDic.Add(flyUIndex, flyTrans);
            flyTrans.position = targetPosition;
            if (math.lengthsq(direction) > 0.0001f)
            {
                flyTrans.rotation = Quaternion.LookRotation(direction);
            }      
            return i;
        }

        // 没有空闲箭
        return -1;
    }

    /// <summary>计算弓箭抛物线弧高，使轨迹中点固定经过指定世界高度。</summary>
    private static float CalculateArrowArcHeight(float3 startPosition, float3 targetPosition)
    {
        float middleLinearY = math.lerp(startPosition.y, targetPosition.y, 0.5f);
        return math.max(ArrowMinimumArcHeight, ArrowApexY - middleLinearY);
    }

    // ========================================
    // Manager Update
    // ========================================

    public void ManagerUpdate(float dt)
    {
        if (!initState)
            return;

        if (activeProjectileIds.Length <= 0)
            return;

        // 清空上一次的到达结果
        arrivedProjectileIds.Clear();

        // ------------------------------------
        // Burst
        // ------------------------------------

        JobHandle handle =
            new ProjectileMoveJob
            {
                projectiles =
            projectiles,

                activeProjectileIds =
            activeProjectileIds.AsArray(),

                arrivedProjectileIds =
            arrivedProjectileIds.AsParallelWriter(),

                deltaTime =
            dt
            }
            .Schedule(
                activeProjectileIds.Length,
                64);

        handle.Complete();

        // ------------------------------------
        // 处理到达
        // ------------------------------------

        ProcessArrivedProjectiles();

        // ------------------------------------
        // 更新视觉
        // ------------------------------------

        ApplyTransforms();
    }

    // ========================================
    // 处理到达
    // ========================================

    private void ProcessArrivedProjectiles()
    {
        for (int i = 0;
             i < arrivedProjectileIds.Length;
             i++)
        {
            int projectileIndex =
                arrivedProjectileIds[i];


            ProjectileData projectile =
                projectiles[projectileIndex];

            // --------------------------------
            // 主线程
            // 这里可以调用 Unity / UnitManager
            // --------------------------------
            FlyObjectLogicBase flyLogic = FlyObjectManager.Instance.SearchByFlyUIndex(projectile.FlyUIndex);
            if (flyLogic != null)
                flyLogic.ArriveTarPos();

            // --------------------------------
            // 从活跃列表移除
            // --------------------------------

            RemoveActiveProjectile(
                projectileIndex);

            // --------------------------------
            // 变成空闲
            // --------------------------------

            projectile.State =
                StateFree;

            projectiles[projectileIndex] =
                projectile;

            projectileTransformsDic[projectile.FlyUIndex] = null;
            projectileTransformsDic.Remove(projectile.FlyUIndex);
        }
    }

    // ========================================
    // 从 Active List 删除
    // ========================================

    private void RemoveActiveProjectile(
        int projectileIndex)
    {
        ProjectileData projectile =
        projectiles[projectileIndex];

        int removeIndex =
            projectile.ActiveListIndex;

        int lastIndex =
            activeProjectileIds.Length - 1;

        if (removeIndex != lastIndex)
        {
            int lastProjectileIndex =
                activeProjectileIds[lastIndex];

            // RemoveAtSwapBack 会把 lastProjectileIndex
            // 自动移动到 removeIndex

            activeProjectileIds.RemoveAtSwapBack(
                removeIndex);

            // 更新被移动过来的箭
            ProjectileData lastProjectile =
                projectiles[lastProjectileIndex];

            lastProjectile.ActiveListIndex =
                removeIndex;

            projectiles[lastProjectileIndex] =
                lastProjectile;
        }
        else
        {
            activeProjectileIds.RemoveAtSwapBack(
                removeIndex);
        }
    }

    // ========================================
    // 更新视觉
    // ========================================

    private void ApplyTransforms()
    {
        for (int i = 0;
             i < activeProjectileIds.Length;
             i++)
        {
            int projectileIndex =
                activeProjectileIds[i];

            ProjectileData projectile =
                projectiles[projectileIndex];

            Transform t = projectileTransformsDic[projectile.FlyUIndex];

            if (t == null)
                continue;

            t.position =
                projectile.Position;

            if (math.lengthsq(
                    projectile.Direction) >
                0.0001f)
            {
                t.rotation =
                    Quaternion.LookRotation(
                        projectile.Direction);
            }
        }
    }

    public ProjectileData GetProjectile(
        int projectileIndex)
    {
        return projectiles[projectileIndex];
    }

    // ========================================
    // 生命周期
    // ========================================

    public void ManagerInit()
    {
        init();
    }

    public void ManagerLateUpdate(
        float dt)
    {
    }

    public void ManagerRefuse()
    {
    }

    public void ManagerDestroy()
    {
        dispose();
    }

    /// <summary>
    /// 清空所有尚未结束的投射物和对应的表现变换。
    /// </summary>
    public void ClearAll()
    {
        if (!initState)
        {
            return;
        }

        for (int i = 0; i < activeProjectileIds.Length; i++)
        {
            int projectileIndex = activeProjectileIds[i];
            ProjectileData projectile = projectiles[projectileIndex];
            projectile.State = StateFree;
            projectiles[projectileIndex] = projectile;
        }

        activeProjectileIds.Clear();
        arrivedProjectileIds.Clear();
        projectileTransformsDic.Clear();
    }

    private void init()
    {
        projectiles = new NativeArray<ProjectileData>(
                MaxProjectileCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);

        activeProjectileIds = new NativeList<int>(
                MaxProjectileCount,
                Allocator.Persistent);

        arrivedProjectileIds = new NativeList<int>(
                MaxProjectileCount,
                Allocator.Persistent);

        // 初始化所有箭
        for (int i = 0;
             i < MaxProjectileCount;
             i++)
        {
            projectiles[i] =
                new ProjectileData
                {
                    Index = i,
                    State = StateFree
                };
        }

        initState = true;
    }

    private void dispose()
    {
        if (projectiles.IsCreated)
            projectiles.Dispose();

        if (activeProjectileIds.IsCreated)
            activeProjectileIds.Dispose();

        if (arrivedProjectileIds.IsCreated)
            arrivedProjectileIds.Dispose();

        initState = false;
    }
}

public struct ProjectileData
{
  
    public int Index;

    public int OwnerUnitIndex;

    public int TargetUnitIndex;

    public int TargetUId;

    public float3 Position;

    public float3 StartPosition;

    public float3 TargetPosition;

    public float3 Direction;

    public float Speed;

    public int Damage;

    public byte State;

    public float ArcHeight;

    public float Progress;

    public int ActiveListIndex;

    public float TotalTime;

    public int FlyUIndex;
}
