using UnityEngine;

public class SkillRemoteSkill : SkillLogicBase
{
    public SkillRemoteSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    public override void OnSkillDoEffect()
    {
        createFlyObject();
    }

    private void createFlyObject()
    {
        int flyCfgId = SkillCfg.flyObjectId;
        Vector3 oriPos = UnitLogic.CurPos;
        Vector3 tarPos = SkillSearchTarget.CurPos;
        UnitFactory.CreateFlyObjectLogic(flyCfgId, oriPos, tarPos, UnitLogic, TargetList, SkillSearchTarget, this, GetDamage());
    }

    public override UnitLogicBase GetSkillSearchTargetSingleResult()
    {
        skillSearchTarget = null;
        if (searchReqIndex < 0)
            return skillSearchTarget;

        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
        if (resultUnitIndexList.Count > 0)
        {
            skillSearchTarget = UnitManager.Instance.UnitList[randomIndex];
            if (skillSearchTarget == UnitLogic)
            {
                Debug.LogError("严重错误搜索到自己了!!");
                skillSearchTarget = null;
            }
            searchReqIndex = -1;
            return skillSearchTarget;
        }
        skillSearchTarget = null;
        searchReqIndex = -1;
        return null;
    }
}
