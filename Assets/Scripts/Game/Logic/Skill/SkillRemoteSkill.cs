using UnityEngine;

public class SkillRemoteSkill : SkillLogicBase
{
    public SkillRemoteSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    public override void SkillDoEffect()
    {
        createFlyObject();
    }

    private void createFlyObject()
    {
        int flyCfgId = SkillCfg.flyObjectId;
        Vector3 oriPos = UnitLogic.CurPos;
        Vector3 tarPos = SearchTarget.CurPos;
        UnitFactory.CreateFlyObjectLogic(flyCfgId, oriPos, tarPos, UnitLogic, TargetList, SearchTarget, this, GetDamage());
    }

    public override UnitLogicBase GetSkillSearchTargetSingleResult()
    {
        searchTarget = null;
        //targetList.Clear();
        if (searchReqIndex < 0)
            return searchTarget;

        resultUnitIndexList.Clear();
        neastIndex = -1;
        randomIndex = -1;

        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
        if (resultUnitIndexList.Count > 0)
        {
            //for (int i = 0; i < resultUnitIndexList.Count; i++)
            //{
            //    int unitIndex = resultUnitIndexList[i];
            //    //targetList.Add(UnitManager.Instance.UnitList[unitIndex]);
            //}
            searchTarget = UnitManager.Instance.UnitList[randomIndex];
            if (searchTarget == UnitLogic)
            {
                Debug.LogError("严重错误搜索到自己了!!");
                searchTarget = null;
            }
            searchReqIndex = -1;
            return searchTarget;
        }
        searchTarget = null;
        searchReqIndex = -1;
        return null;
    }
}
