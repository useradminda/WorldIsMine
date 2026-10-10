
using System.Collections.Generic;
using UnityEngine;
// Area Object
public class AreaFlyObject : FlyObjectLogicBase
{
    private float liveTime = 0;
    private float damageClipTime = 0;
    private int triggeredDamageCount;

    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int uIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, uIndex);
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, tarPos, Vector3.zero);
        liveTime = flyObjectCfg.liveTime;
        damageClipTime = flyObjectCfg.damClipTime;
        triggeredDamageCount = 0;
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (CanTriggerDamage())
        {
            reqSearchTar();
            setSearchTar();

            damageClipTime -= dt;
            if (damageClipTime < 0)
            {
                damage();
                triggeredDamageCount++;
                damageClipTime = mFlyObjectCfg.damClipTime;
            }
        }

        liveTime -= dt;
        if (liveTime < 0)
            die();
    }

    /// <summary>
    /// 判断区域伤害是否还可以继续触发，负数表示不限制次数。
    /// </summary>
    private bool CanTriggerDamage()
    {
        return mFlyObjectCfg.triggerDamageCount < 0 || triggeredDamageCount < mFlyObjectCfg.triggerDamageCount;
    }

    private int searchReqIndex = -1;
    List<int> resultUnitIndexList = new List<int>();
    private int neastIndex = -1;
    private int randomIndex = -1;
    private void reqSearchTar()
    {
        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    {
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
    }

    private void damage()
    {      
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, GetFinalDamage(tarUnitLogic), tarUnitLogic.UId, mSkillLogic.SkillCfg.dieType, mTarPos);
                mSkillLogic.ApplyBuffs(tarUnitLogic);
            }
        }
    }

    private void die()
    {
        UnitFactory.RemoveFlyObjectLogic(this);
        UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
    }
}
