using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ZTools;

public class ProjectileJobManager : Singleton<ProjectileJobManager>, IManager
{
    // 最大箭数量
    private const int MaxProjectileCount = 6000;

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

    private Transform[] projectileTransforms;

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
        int damage
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

            float arcHeight = math.clamp( distance * 0.2f, 2f, 10f);

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
                };

            // 激活视觉
            Transform t = projectileTransforms[i];

            if (t != null)
            {
                t.position =
                    startPosition;

                if (math.lengthsq(direction) > 0.0001f)
                {
                    t.rotation =
                        Quaternion.LookRotation(
                            direction);
                }

                t.gameObject.SetActive(true);
            }

            return i;
        }

        // 没有空闲箭
        return -1;
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

            BattleLogicDamageTools.DoDamage(UnitManager.Instance.UnitList[projectile.OwnerUnitIndex], UnitManager.Instance.UnitList[projectile.TargetUnitIndex], projectile.Damage, projectile.TargetUId);
            //UnitManager.Instance.Damage(
            //    projectile.TargetUnitIndex,
            //    projectile.Damage);

            // --------------------------------
            // 隐藏视觉
            // --------------------------------

            Transform t =
                projectileTransforms[
                    projectileIndex];

            if (t != null)
            {
                t.gameObject.SetActive(false);
            }

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

            Transform t =
                projectileTransforms[
                    projectileIndex];

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

        projectileTransforms =
            new Transform[MaxProjectileCount];

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

        for (int i = 0; i < MaxProjectileCount; i++)
        {
            GameObject arrowGo = UnitViewFactory.CreateGob("FlyObject/Arrow", new Vector3(0, 10000, 0), Vector3.forward);
            setProjectileTransform(i, arrowGo.transform);
        }

        initState = true;
    }

    private void setProjectileTransform(int projectileIndex, Transform transform)
    {
        if (projectileIndex < 0 || projectileIndex >= MaxProjectileCount)
            return;
        projectileTransforms[projectileIndex] = transform;
        transform.gameObject.SetActive(false);
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
}
