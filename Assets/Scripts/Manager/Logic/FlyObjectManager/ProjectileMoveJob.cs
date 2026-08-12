using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ProjectileMoveJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<ProjectileData> projectiles;

    [ReadOnly]
    public NativeArray<int> activeProjectileIds;

    public NativeList<int>.ParallelWriter arrivedProjectileIds;

    public float deltaTime;

    public void Execute(int index)
    {
        int projectileIndex =
            activeProjectileIds[index];

        ProjectileData projectile =
            projectiles[projectileIndex];

        if (projectile.State != 1)
            return;

        // --------------------------------
        // 推进进度
        // --------------------------------

        projectile.Progress +=
            deltaTime / projectile.TotalTime;

        // --------------------------------
        // 到达目标
        // --------------------------------

        if (projectile.Progress >= 1f)
        {
            projectile.Progress = 1f;

            projectile.Position =
                projectile.TargetPosition;

            projectile.State = 2;

            projectiles[projectileIndex] =
                projectile;

            arrivedProjectileIds.AddNoResize(
                projectileIndex);

            return;
        }

        // --------------------------------
        // 当前进度
        // --------------------------------

        float t =
            projectile.Progress;

        float3 start =
            projectile.StartPosition;

        float3 target =
            projectile.TargetPosition;

        // --------------------------------
        // 起点 → 目标点
        // --------------------------------

        float3 position =
            math.lerp(
                start,
                target,
                t);

        // --------------------------------
        // 抛物线
        //
        // t = 0   → 0
        // t = 0.5 → ArcHeight
        // t = 1   → 0
        // --------------------------------

        float arc =
            4f *
            t *
            (1f - t) *
            projectile.ArcHeight;

        position.y += arc;

        projectile.Position =
            position;

        // --------------------------------
        // 计算箭头方向
        // --------------------------------

        float nextT =
            math.min(
                t + 0.001f,
                1f);

        float3 nextPosition =
            math.lerp(
                start,
                target,
                nextT);

        float nextArc =
            4f *
            nextT *
            (1f - nextT) *
            projectile.ArcHeight;

        nextPosition.y += nextArc;

        projectile.Direction =
            math.normalizesafe(nextPosition - position);

        projectiles[projectileIndex] =
            projectile;
    }
}