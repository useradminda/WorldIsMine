using System.Collections.Generic;
using UnityEngine;

/// <summary>Follows its caster and repeatedly damages enemies around the caster.</summary>
public class AroundAreaFlyObject : FlyObjectLogicBase
{
    private float liveTime;
    private float damageClipTime;
    private int triggeredDamageCount;
    private bool damagePending;
    private bool destroyed;

    private readonly List<int> resultUnitIndexList = new List<int>();
    private readonly List<int> resultUnitUidList = new List<int>();
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private int searchReqIndex = -1;

    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg,
        Vector3 oriPos,
        Vector3 tarPos,
        UnitLogicBase atkUnitLogic,
        List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetUnit,
        SkillLogicBase skillLogic,
        int attackDamage,
        int uIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, uIndex);
        liveTime = flyObjectCfg.liveTime;
        damageClipTime = flyObjectCfg.damClipTime;
        triggeredDamageCount = 0;
        damagePending = false;
        destroyed = false;
        searchReqIndex = -1;
        resultUnitIndexList.Clear();
        resultUnitUidList.Clear();
        mTarPos = atkUnitLogic != null ? atkUnitLogic.CurPos : oriPos;
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, mTarPos, Vector3.zero);
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed)
        {
            return;
        }

        if (mAtkUnitLogic == null || mAtkUnitLogic.IsDead)
        {
            Die();
            return;
        }

        if (!CanTriggerDamage())
        {
            Die();
            return;
        }

        mTarPos = mAtkUnitLogic.CurPos;
        if (mFlyObjectGob != null)
        {
            mFlyObjectGob.transform.position = mTarPos;
        }

        if (CanTriggerDamage())
        {
            damageClipTime -= dt;
            if (damageClipTime <= 0f && !damagePending)
            {
                searchReqIndex = MapCellManager.Instance.RequestSearch(
                    mTarPos,
                    mSkillLogic.SkillCfg.skillArea,
                    mAtkUnitLogic.OtherCampTypeInt);
                damagePending = searchReqIndex >= 0;
            }
        }

        liveTime -= dt;
        if (liveTime <= 0f)
        {
            Die();
        }
    }

    public override void FlyObjectLateUpdate(float dt)
    {
        if (destroyed || !damagePending)
        {
            return;
        }

        resultUnitIndexList.Clear();
        nearestIndex = -1;
        randomIndex = -1;
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
        UnitTargetSnapshot.Capture(resultUnitIndexList, resultUnitUidList);
        Damage();

        triggeredDamageCount++;
        damageClipTime = mFlyObjectCfg.damClipTime;
        damagePending = false;
        searchReqIndex = -1;

        if (!CanTriggerDamage())
        {
            Die();
        }
    }

    private bool CanTriggerDamage()
    {
        return mFlyObjectCfg.triggerDamageCount < 0
            || triggeredDamageCount < mFlyObjectCfg.triggerDamageCount;
    }

    private void Damage()
    {
        if (mAtkUnitLogic == null || mAtkUnitLogic.IsDead)
        {
            return;
        }

        for (int i = 0; i < resultUnitIndexList.Count; i++)
        {
            if (!UnitTargetSnapshot.TryGet(resultUnitIndexList, resultUnitUidList, i, out UnitLogicBase target)
                || target == null
                || target.IsDead
                || target.CampTypeInt == mAtkUnitLogic.CampTypeInt)
            {
                continue;
            }

            BattleLogicDamageTools.DoDamage(
                mAtkUnitLogic,
                target,
                GetFinalDamage(target),
                resultUnitUidList[i],
                mSkillLogic.SkillCfg.dieType,
                mTarPos);
            mSkillLogic.ApplyBuffs(target);
        }
    }

    private void Die()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        UnitFactory.RemoveFlyObjectLogic(this);
        if (mFlyObjectGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }
    }
}
