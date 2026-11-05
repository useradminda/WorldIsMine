using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 援军召唤技能类型。
/// 适用于战场上按红蓝双方士兵配置批量生成援军的技能，默认每次召唤 10 个单位。
/// </summary>
public class CallFriendSkill : SkillLogicBase
{
    public const int DefaultFriendCount = 10;
    private const float SpawnEffectDuration = 2f;

    public CallFriendSkill(UnitLogicBase ulb, SkillCfg skillCfg)
        : base(ulb, skillCfg)
    {
    }

    public override void OnSkillDoEffect(UnitLogicBase target, System.Collections.Generic.List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead || BattleEngine.Instance == null)
        {
            return;
        }

        if (!TryGetReinforcementCfgId(out int reinforcementCfgId))
        {
            Debug.LogError($"召唤援军技能参数无效，SkillId={SkillCfg.id}, skillParam={SkillCfg.skillParam}");
            return;
        }

        Vector3 spawnForward = UnitLogic.TargetForward;
        spawnForward.y = 0f;
        if (spawnForward.sqrMagnitude <= 0.0001f)
        {
            spawnForward = Vector3.forward;
        }

        List<Vector3> spawnPoints = CreateSpawnPoints(UnitLogic.CurPos, DefaultFriendCount, reinforcementCfgId);
        BattleEngine.Instance.CreateReinforcementUnits(
            reinforcementCfgId,
            UnitLogic.CampType,
            spawnPoints,
            spawnForward.normalized);

        PlaySpawnEffects(spawnPoints, spawnForward.normalized);
    }

    /// <summary>
    /// 计算援军围绕召唤者的圆周出生点。
    /// </summary>
    private List<Vector3> CreateSpawnPoints(Vector3 center, int count, int soldierCfgId)
    {
        List<Vector3> spawnPoints = new List<Vector3>(count);
        if (count <= 0)
        {
            return spawnPoints;
        }

        SoliderCfg soldierCfg = SoliderCfgConfig.Ins.SearchById(soldierCfgId);
        float unitRadius = soldierCfg != null ? soldierCfg.radius : 1.5f;
        float spacing = Mathf.Max(0.1f, unitRadius * 2f + BattleDefine.SpawnUnitPadding);
        float circumferenceRadius = spacing * count / (Mathf.PI * 2f);
        float radius = Mathf.Max(spacing * 1.5f, circumferenceRadius);

        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2f * i / count;
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
        if (SkillCfg == null || string.IsNullOrWhiteSpace(SkillCfg.startEffect) || spawnPoints == null)
        {
            return;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            GameObject effect = UnitViewFactory.CreateGob(SkillCfg.startEffect, spawnPoints[i], spawnForward);
            if (effect != null)
            {
                effect.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(SpawnEffectDuration, SkillCfg.startEffect);
            }
        }
    }

    private bool TryGetReinforcementCfgId(out int reinforcementCfgId)
    {
        reinforcementCfgId = 0;
        string skillParam = SkillCfg.skillParam;
        if (string.IsNullOrWhiteSpace(skillParam))
        {
            return false;
        }

        string[] values = skillParam.Split(new[] { ',', '|', ';', '/', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
        int redCfgId;
        int blueCfgId;
        if (values.Length >= 2 && int.TryParse(values[0], out redCfgId) && int.TryParse(values[1], out blueCfgId))
        {
            reinforcementCfgId = UnitLogic.CampType == ECampType.Red ? redCfgId : blueCfgId;
            return IsValidSoldierCfg(reinforcementCfgId);
        }

        string compactParam = skillParam.Trim();
        for (int splitIndex = 1; splitIndex < compactParam.Length; splitIndex++)
        {
            if (!int.TryParse(compactParam.Substring(0, splitIndex), out redCfgId)
                || !int.TryParse(compactParam.Substring(splitIndex), out blueCfgId))
            {
                continue;
            }

            if (IsValidSoldierCfg(redCfgId) && IsValidSoldierCfg(blueCfgId))
            {
                reinforcementCfgId = UnitLogic.CampType == ECampType.Red ? redCfgId : blueCfgId;
                return true;
            }
        }

        return false;
    }

    private bool IsValidSoldierCfg(int cfgId)
    {
        return cfgId > 0 && SoliderCfgConfig.Ins.SearchById(cfgId) != null;
    }
}
