using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用远程技能类型。
/// 适用于需要通过 flyObjectId 发射箭、石块或其他飞行器的技能；自身目标技能则直接施加 Buff。
/// </summary>
public class SkillRemoteSkill : SkillLogicBase
{
    public SkillRemoteSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    /// <summary>
    /// 判断远程技能是否允许对目标释放；普通远程攻击可以攻击城门，非普攻远程技能不能主动锁定城门。
    /// </summary>
    public override bool CanUseWithTarget(UnitLogicBase target)
    {
        return target != null && (BNormalSkill || target.UnitType != EUnitType.Wall);
    }

    /// <summary>
    /// 执行远程技能的实际效果，延迟由技能基类统一处理。
    /// </summary>
    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (SkillCfg.needTarget == 2)
        {
            // 自身目标技能不创建飞行物，直接给施法者添加Buff。
            ApplyBuffs(UnitLogic);
            return;
        }

        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(SkillCfg.flyObjectId);
        bool isAreaFlyObject = flyObjectCfg != null
            && (flyObjectCfg.flyType == "area"
                || flyObjectCfg.flyType == "areaRandom"
                || flyObjectCfg.flyType == "blackHall"
                || flyObjectCfg.flyType == "translateArea");
        bool isLineFlyObject = flyObjectCfg != null && flyObjectCfg.flyType == "line";
        if (target == null || (!isAreaFlyObject && !isLineFlyObject && target.IsDead)
            || (!BNormalSkill && target.UnitType == EUnitType.Wall))
        {
            return;
        }
        int flyCfgId = SkillCfg.flyObjectId;
        List<UnitLogicBase> targetLogicList = BuildTargetLogicList(resultUnitIndexList, target);
        Vector3 oriPos = UnitLogic.CurPos;
        Vector3 tarPos = GetFlyTargetPosition(flyCfgId, target, targetLogicList);
        Vector3 attackDirection = tarPos - oriPos;
        attackDirection.y = 0f;
        if (attackDirection.sqrMagnitude > 0.0001f && UnitLogic.UnitView != null)
        {
            // 远程飞行技能释放时同步角色朝向到第一发目标方向。
            UnitLogic.UnitView.SetForwardForce(attackDirection.normalized);
        }
        FlyObjectFactory.CreateFlyObjectLogic(flyCfgId, oriPos, tarPos, UnitLogic, targetLogicList, target, this, GetAttackDamageSnapshot());
    }

    /// <summary>
    /// 将技能范围搜索得到的单位下标转换为本次飞行器使用的有效目标对象列表。
    /// </summary>
    private List<UnitLogicBase> BuildTargetLogicList(List<int> resultUnitIndexList, UnitLogicBase primaryTarget)
    {
        List<UnitLogicBase> targetLogicList = new List<UnitLogicBase>();
        if (resultUnitIndexList != null)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
                {
                    continue;
                }

                UnitLogicBase target = UnitManager.Instance.UnitList[unitIndex];
                if (target == null || target.IsDead || target.CampTypeInt == UnitLogic.CampTypeInt)
                {
                    continue;
                }

                targetLogicList.Add(target);
            }
        }

        if (primaryTarget != null && !primaryTarget.IsDead
            && primaryTarget.CampTypeInt != UnitLogic.CampTypeInt
            && !targetLogicList.Contains(primaryTarget))
        {
            targetLogicList.Add(primaryTarget);
        }

        return targetLogicList;
    }

    /// <summary>
    /// 为范围型飞行器计算所有有效目标的中心点，普通单目标飞行器仍使用主目标位置。
    /// </summary>
    private Vector3 GetFlyTargetPosition(int flyCfgId, UnitLogicBase primaryTarget, List<UnitLogicBase> targetLogicList)
    {
        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyCfgId);
        bool isAreaFlyObject = flyObjectCfg != null
            && (flyObjectCfg.flyType == "area"
                || flyObjectCfg.flyType == "areaRandom"
                || flyObjectCfg.flyType == "blackHall"
                || flyObjectCfg.flyType == "translateArea");
        if (!isAreaFlyObject || targetLogicList == null || targetLogicList.Count == 0)
        {
            return primaryTarget.GetClosestPoint(UnitLogic.CurPos);
        }

        Vector3 center = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < targetLogicList.Count; i++)
        {
            UnitLogicBase target = targetLogicList[i];
            if (target == null || target.IsDead || target.CampTypeInt == UnitLogic.CampTypeInt)
            {
                continue;
            }

            center += target.CurPos;
            validCount++;
        }

        return validCount > 0
            ? center / validCount
            : (primaryTarget != null ? primaryTarget.CurPos : UnitLogic.CurPos);
    }
}
