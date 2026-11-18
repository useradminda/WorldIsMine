using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 随机位置持续范围飞行器。
/// 适用于落点不断变化的区域攻击，每次触发时从搜索结果中选择随机单位位置作为表现位置。
/// </summary>
public class AreaRandomFlyObject : FlyObjectLogicBase
{
    private float liveTime = 0;
    private float damageClipTime = 0;
    private int triggeredDamageCount;
    private bool damagePending;
    private List<GameObject> flyObjectGobList = new List<GameObject>();

    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int uIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, uIndex);
        flyObjectGobList.Clear();
        liveTime = flyObjectCfg.liveTime;
        damageClipTime = flyObjectCfg.damClipTime;
        triggeredDamageCount = 0;
        damagePending = false;
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (CanTriggerDamage())
        {
            damageClipTime -= dt;
            if (damageClipTime < 0 && !damagePending)
            {
                reqSearchTar();
                damagePending = true;
            }
        }

        liveTime -= dt;
        if (liveTime < 0)
            die();
    }

    public override void FlyObjectLateUpdate(float dt)
    {
        if (!damagePending)
        {
            return;
        }
        setSearchTar();
        setRandomPos();
        damage();
        triggeredDamageCount++;
        damageClipTime = mFlyObjectCfg.damClipTime;
        damagePending = false;
    }

    /// <summary>
    /// 判断随机区域伤害是否还可以继续触发，负数表示不限制次数。
    /// </summary>
    private bool CanTriggerDamage()
    {
        return mFlyObjectCfg.triggerDamageCount < 0 || triggeredDamageCount < mFlyObjectCfg.triggerDamageCount;
    }

    private int searchReqIndex = -1;
    List<int> resultUnitIndexList = new List<int>();
    List<int> resultUnitUidList = new List<int>();
    private int neastIndex = -1;
    private int randomIndex = -1;
    private void reqSearchTar()
    {
        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    {
        resultUnitIndexList.Clear();
        neastIndex = -1;
        randomIndex = -1;
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
        UnitTargetSnapshot.Capture(resultUnitIndexList, resultUnitUidList);
    }

    private void damage()
    {
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                if (!UnitTargetSnapshot.TryGet(resultUnitIndexList, resultUnitUidList, i, out UnitLogicBase tarUnitLogic) || tarUnitLogic.CampTypeInt == mAtkUnitLogic.CampTypeInt)
                {
                    continue;
                }
                CreateArriveEffect(tarUnitLogic.CurPos);
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, GetFinalDamage(tarUnitLogic), resultUnitUidList[i], mSkillLogic.SkillCfg.dieType, mTarPos);
                mSkillLogic.ApplyBuffs(tarUnitLogic); 
            }
        }
        DamageEnemyWallInArea(mTarPos, mSkillLogic.SkillCfg.skillArea);
    }

    public override void ClearForBattle()
    {
        die();
    }

    private void die()
    {
        UnitFactory.RemoveFlyObjectLogic(this);
        for (int i = 0; i < flyObjectGobList.Count; i++)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, flyObjectGobList[i]);
        }
        flyObjectGobList.Clear();
    }

    private void setRandomPos()
    {
        mTarPos = getRandomPos();
        GameObject flyObject = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, mTarPos, Vector3.zero);
        flyObjectGobList.Add(flyObject);
    }

    private Vector3 getRandomPos()
    {
        if (randomIndex < 0)
            return mTarPos;
        UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[randomIndex];
        return tarUnitLogic.CurPos;
    }
}
