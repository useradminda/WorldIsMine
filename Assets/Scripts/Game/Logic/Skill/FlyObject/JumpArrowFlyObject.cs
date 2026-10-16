using System.Collections.Generic;
using UnityEngine;

public class JumpArrowFlyObject : FlyObjectLogicBase
{
    private readonly List<int> resultUnitIndexList = new List<int>();
    private readonly HashSet<int> attackedTargetUidSet = new HashSet<int>();
    private int targetUID = -1;
    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private bool flying;
    private bool searchPending;
    private Vector3 currentPosition;
    private int triggeredDamageCount;

    private float liveTime = 0;
   
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, flyUIndex);
        flying = false;
        targetUID = -1;
        triggeredDamageCount = 0;
        attackedTargetUidSet.Clear();
        resultUnitIndexList.Clear();
        searchReqIndex = -1;
        nearestIndex = -1;
        randomIndex = -1;
        if (mSearchTargetUnitLogic != null && mSearchTargetUnitLogic.IsDead == false)
        {
           targetUID = mSearchTargetUnitLogic.UId;
        }
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, oriPos, tarPos - oriPos);
        startFly();
        liveTime = flyObjectCfg.liveTime;
        searchPending = false;
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (!flying && !searchPending)
        {
            searchReqIndex = MapCellManager.Instance.RequestSearch(mFlyObjectGob.transform.position, mSkillLogic.SkillCfg.searchRange, mAtkUnitLogic.OtherCampTypeInt);
            searchPending = true;
        }
        UpdateFlyPosition(dt);
        liveTime -= dt;
        if (liveTime < 0)
        {
            Die();
        }
    }

    public override void FlyObjectLateUpdate(float dt)
    {
        if (!searchPending)
        {
            return;
        }
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
        searchReqIndex = -1;
        searchPending = false;
        if (flying)
        {
            return;
        }
        UnitLogicBase target = SelectNextTarget();
        if (target != null)
        {
            mSearchTargetUnitLogic = target;
            targetUID = target.UId;
            startFly();
        }
    }

    public override void ArriveTarPos()
    {
        CreateArriveEffect(mFlyObjectGob.transform.position);
        BattleLogicDamageTools.DoDamage(mAtkUnitLogic, mSearchTargetUnitLogic, GetFinalDamage(mSearchTargetUnitLogic), targetUID, mSkillLogic.SkillCfg.dieType, mFlyObjectGob.transform.position);
        mSkillLogic.ApplyBuffs(mSearchTargetUnitLogic);
        attackedTargetUidSet.Add(targetUID);
        triggeredDamageCount++;
        if (mFlyObjectCfg.triggerDamageCount >= 0 && triggeredDamageCount >= mFlyObjectCfg.triggerDamageCount)
        {
            Die();
        }
    }

    private void startFly()
    {
        mOriPos = mFlyObjectGob.transform.position;   
        currentPosition = mOriPos;
        flying = true;
    }

    private void UpdateFlyPosition(float dt)
    {
        if (flying)
        {
            if (mSearchTargetUnitLogic == null || mSearchTargetUnitLogic.IsDead == true || targetUID != mSearchTargetUnitLogic.UId)
            {
                flying = false;
                return;
            }
            float speed = Mathf.Max(0.01f, mFlyObjectCfg.speed);
            currentPosition = Vector3.MoveTowards(currentPosition, mSearchTargetUnitLogic.CurPos, speed * dt);
            mFlyObjectGob.transform.position = currentPosition;
            Vector3 direction = mSearchTargetUnitLogic.CurPos - currentPosition;
            if (direction.sqrMagnitude > 0.0001f)
            {
                mFlyObjectGob.transform.rotation = Quaternion.LookRotation(direction);
            }
            if ((currentPosition - mSearchTargetUnitLogic.CurPos).sqrMagnitude < (0.15f * 0.15f))
            {
                flying = false;
                ArriveTarPos();
            }
        }
    }

    private void Die()
    {
        flying = false;
        if (mFlyObjectGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }
        UnitFactory.RemoveFlyObjectLogic(this);
    }

    private UnitLogicBase SelectNextTarget()
    {
        if (resultUnitIndexList.Count == 0)
        {
            return null;
        }
        int startIndex = randomIndex >= 0 ? resultUnitIndexList.IndexOf(randomIndex) : Random.Range(0, resultUnitIndexList.Count);
        if (startIndex < 0)
        {
            startIndex = 0;
        }
        for (int i = 0; i < resultUnitIndexList.Count; i++)
        {
            int resultIndex = resultUnitIndexList[(startIndex + i) % resultUnitIndexList.Count];
            if (resultIndex < 0 || resultIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }
            UnitLogicBase target = UnitManager.Instance.UnitList[resultIndex];
            if (target != null && !target.IsDead && target.CampTypeInt != mAtkUnitLogic.CampTypeInt && !attackedTargetUidSet.Contains(target.UId))
            {
                return target;
            }
        }
        return null;
    }
}
