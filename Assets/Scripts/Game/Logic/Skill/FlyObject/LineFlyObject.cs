
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
// Area Object
public class LineFlyObject : FlyObjectLogicBase
{
    private float liveTime = 0;
    private float damageClipTime = 0;
    private int triggeredDamageCount;
    private Vector3 moveForward = Vector3.zero;
    private Vector3 curPos = Vector3.zero;
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int uIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, uIndex);
        curPos = oriPos;
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, curPos, Vector3.zero);
        moveForward = (tarPos - oriPos).normalized;
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

        updateMoveForward(dt);

        liveTime -= dt;
        if (liveTime < 0)
            die();
    }

    /// <summary>
    /// 判断直线范围伤害是否还可以继续触发，负数表示不限制次数。
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
        searchReqIndex = MapCellManager.Instance.RequestSearch(curPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    {
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
    }

    private void updateMoveForward(float dt)
    {
        curPos = curPos + moveForward * dt * mFlyObjectCfg.speed;
        mFlyObjectGob.transform.position = Vector3.Lerp(mFlyObjectGob.transform.position, curPos, dt * 3);
    }

    private void damage()
    {      
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, GetFinalDamage(tarUnitLogic), tarUnitLogic.UId, mSkillLogic.SkillCfg.dieType, curPos);
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
