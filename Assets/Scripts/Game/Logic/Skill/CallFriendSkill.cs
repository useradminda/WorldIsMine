using System;
using UnityEngine;

/// <summary>Summons a small reinforcement group for the owner's camp.</summary>
public class CallFriendSkill : SkillLogicBase
{
    public const int DefaultFriendCount = 20;

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

        BattleEngine.Instance.CreateReinforcementUnits(
            reinforcementCfgId,
            UnitLogic.CampType,
            DefaultFriendCount,
            UnitLogic.CurPos,
            spawnForward.normalized);
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
