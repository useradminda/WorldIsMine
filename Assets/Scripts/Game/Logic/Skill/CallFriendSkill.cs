using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 援军召唤技能类型。
/// 适用于按 skillParam 指定士兵配置和数量，并以三层同心圆分批生成援军的技能。
/// </summary>
public class CallFriendSkill : SkillLogicBase
{
    private const float SpawnEffectDuration = 2f;
    private const float SpawnWaveInterval = 0.35f;
    private readonly List<List<Vector3>> pendingSpawnWaves = new List<List<Vector3>>();
    private int pendingReinforcementCfgId;
    private int nextSpawnWaveIndex;
    private float nextSpawnWaveTime;

    public CallFriendSkill(UnitLogicBase ulb, SkillCfg skillCfg)
        : base(ulb, skillCfg)
    {
    }

    /// <summary>
    /// 执行援军召唤技能的实际效果，延迟由技能基类统一处理。
    /// </summary>
    public override void OnSkillDoEffect(UnitLogicBase target, System.Collections.Generic.List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead || BattleEngine.Instance == null)
        {
            return;
        }

        if (!TryParseReinforcementParam(out int reinforcementCfgId, out int reinforcementCount))
        {
            int skillId = SkillCfg != null ? SkillCfg.id : 0;
            string skillParam = SkillCfg != null ? SkillCfg.skillParam : string.Empty;
            Debug.LogError($"召唤援军技能参数无效，格式应为：士兵配置ID,召唤数量，SkillId={skillId}, skillParam={skillParam}");
            return;
        }

        Vector3 spawnForward = UnitLogic.TargetForward;
        spawnForward.y = 0f;
        if (spawnForward.sqrMagnitude <= 0.0001f)
        {
            spawnForward = Vector3.forward;
        }

        pendingSpawnWaves.Clear();
        pendingReinforcementCfgId = reinforcementCfgId;
        pendingSpawnWaves.AddRange(CreateSpawnWaves(UnitLogic.CurPos, reinforcementCount, reinforcementCfgId));
        nextSpawnWaveIndex = 0;
        nextSpawnWaveTime = 0f;
        SpawnNextWave(spawnForward.normalized);
        if (nextSpawnWaveIndex < pendingSpawnWaves.Count)
        {
            nextSpawnWaveTime = SpawnWaveInterval;
        }
        else
        {
            ClearPendingSpawnWaves();
        }
    }

    /// <summary>
    /// 更新援军分圈召唤的间隔计时，并按顺序生成后续圈层。
    /// </summary>
    public override void SkillUpdate(float dt)
    {
        base.SkillUpdate(dt);
        if (nextSpawnWaveIndex >= pendingSpawnWaves.Count)
        {
            return;
        }

        if (UnitLogic == null || UnitLogic.IsDead)
        {
            ClearPendingSpawnWaves();
            return;
        }

        nextSpawnWaveTime -= dt * UnitLogic.LogicRatio;
        while (nextSpawnWaveTime <= 0f && nextSpawnWaveIndex < pendingSpawnWaves.Count)
        {
            SpawnNextWave(UnitLogic.TargetForward);
            nextSpawnWaveTime += SpawnWaveInterval;
        }

        if (nextSpawnWaveIndex >= pendingSpawnWaves.Count)
        {
            ClearPendingSpawnWaves();
        }
    }

    /// <summary>
    /// 生成下一圈援军，并播放这一圈的出生特效。
    /// </summary>
    private void SpawnNextWave(Vector3 spawnForward)
    {
        if (nextSpawnWaveIndex < 0 || nextSpawnWaveIndex >= pendingSpawnWaves.Count)
        {
            return;
        }

        List<Vector3> spawnPoints = pendingSpawnWaves[nextSpawnWaveIndex];
        nextSpawnWaveIndex++;
        spawnForward.y = 0f;
        if (spawnForward.sqrMagnitude <= 0.0001f)
        {
            spawnForward = Vector3.forward;
        }
        spawnForward.Normalize();

        BattleEngine.Instance.CreateReinforcementUnits(
            pendingReinforcementCfgId,
            UnitLogic.CampType,
            spawnPoints,
            spawnForward);
        PlaySpawnEffects(spawnPoints, spawnForward);
    }

    /// <summary>
    /// 清理尚未生成的援军圈层。
    /// </summary>
    private void ClearPendingSpawnWaves()
    {
        pendingSpawnWaves.Clear();
        pendingReinforcementCfgId = 0;
        nextSpawnWaveIndex = 0;
        nextSpawnWaveTime = 0f;
    }

    /// <summary>
    /// 按内圈 20%、第二圈 30%、外圈剩余数量分配援军数量。
    /// </summary>
    private List<int> CreateRingCounts(int totalCount)
    {
        List<int> ringCounts = new List<int>();
        if (totalCount <= 0)
        {
            return ringCounts;
        }

        int firstRingCount = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(totalCount * 0.2f)), totalCount);
        ringCounts.Add(firstRingCount);
        if (totalCount == firstRingCount)
        {
            return ringCounts;
        }

        int remainingCount = totalCount - firstRingCount;
        int secondRingCount = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(totalCount * 0.3f)), remainingCount);
        ringCounts.Add(secondRingCount);
        if (remainingCount == secondRingCount)
        {
            return ringCounts;
        }

        ringCounts.Add(totalCount - firstRingCount - secondRingCount);
        return ringCounts;
    }

    /// <summary>
    /// 按等间距同心圆计算三圈援军出生点。
    /// </summary>
    private List<List<Vector3>> CreateSpawnWaves(Vector3 center, int count, int soldierCfgId)
    {
        List<List<Vector3>> spawnWaves = new List<List<Vector3>>();
        SoliderCfg soldierCfg = SoliderCfgConfig.Ins.SearchById(soldierCfgId);
        float unitRadius = soldierCfg != null ? soldierCfg.radius : 1.5f;
        float pointSpacing = Mathf.Max(0.1f, unitRadius * 2f + BattleDefine.SpawnUnitPadding);
        float radialSpacing = pointSpacing;
        float minimumRadius = pointSpacing * 1.5f;
        float previousRadius = 0f;
        List<int> ringCounts = CreateRingCounts(count);

        for (int ringIndex = 0; ringIndex < ringCounts.Count; ringIndex++)
        {
            int ringCount = ringCounts[ringIndex];
            float circumferenceRadius = pointSpacing * ringCount / (Mathf.PI * 2f);
            float ringRadius = Mathf.Max(minimumRadius + ringIndex * radialSpacing, circumferenceRadius);
            if (ringIndex > 0)
            {
                ringRadius = Mathf.Max(ringRadius, previousRadius + radialSpacing);
            }

            spawnWaves.Add(CreateRingSpawnPoints(center, ringCount, ringRadius, ringIndex));
            previousRadius = ringRadius;
        }

        return spawnWaves;
    }

    /// <summary>
    /// 在单个圆环上均匀分布援军出生点，并对相邻圆环进行角度错位。
    /// </summary>
    private List<Vector3> CreateRingSpawnPoints(Vector3 center, int count, float radius, int ringIndex)
    {
        List<Vector3> spawnPoints = new List<Vector3>(count);
        if (count <= 0)
        {
            return spawnPoints;
        }

        float angleOffset = ringIndex % 2 == 0 ? 0f : Mathf.PI / count;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2f * i / count + angleOffset;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            spawnPoints.Add(center + offset);
        }

        return spawnPoints;
    }

    /// <summary>
    /// 在每个援军出生点播放技能配置中的出生特效。
    /// </summary>
    private void PlaySpawnEffects(List<Vector3> spawnPoints, Vector3 spawnForward)
    {
        if (SkillCfg == null || SkillCfg.startEffect == null || spawnPoints == null)
        {
            return;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            for (int effectIndex = 0; effectIndex < SkillCfg.startEffect.Length; effectIndex++)
            {
                string effectPath = SkillCfg.startEffect[effectIndex];
                if (string.IsNullOrWhiteSpace(effectPath))
                {
                    continue;
                }

                GameObject effect = UnitViewFactory.CreateGob(effectPath, spawnPoints[i], spawnForward);
                if (effect != null)
                {
                    effect.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(SpawnEffectDuration, effectPath);
                }
            }
        }
    }

    /// <summary>
    /// 解析 skillParam 中的士兵配置 ID 和援军数量，格式为“士兵配置ID,数量”。
    /// </summary>
    private bool TryParseReinforcementParam(out int reinforcementCfgId, out int reinforcementCount)
    {
        reinforcementCfgId = 0;
        reinforcementCount = 0;
        if (SkillCfg == null || string.IsNullOrWhiteSpace(SkillCfg.skillParam))
        {
            return false;
        }

        string[] values = SkillCfg.skillParam.Split(new[] { ',', '|', ';', '/', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 2
            || !int.TryParse(values[0], out reinforcementCfgId)
            || !int.TryParse(values[1], out reinforcementCount))
        {
            reinforcementCfgId = 0;
            reinforcementCount = 0;
            return false;
        }

        return reinforcementCount > 0 && IsValidSoldierCfg(reinforcementCfgId);
    }

    /// <summary>
    /// 判断指定士兵配置是否存在且可以用于召唤。
    /// </summary>
    private bool IsValidSoldierCfg(int cfgId)
    {
        return cfgId > 0 && SoliderCfgConfig.Ins.SearchById(cfgId) != null;
    }
}
