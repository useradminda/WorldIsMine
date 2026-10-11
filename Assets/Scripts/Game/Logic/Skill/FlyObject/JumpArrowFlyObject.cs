using System.Collections.Generic;
using UnityEngine;

public class JumpArrowFlyObject : FlyObjectLogicBase
{
    private readonly List<int> resultUnitIndexList = new List<int>();
    private int targetUID = -1;
    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private bool flying;
    private Vector3 currentPosition;

    private float liveTime = 0;
   
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, flyUIndex);
        flying = false;
        if (mSearchTargetUnitLogic != null && mSearchTargetUnitLogic.IsDead == false)
        {
           targetUID = mSearchTargetUnitLogic.UId;
        }
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, oriPos, tarPos - oriPos);
        startFly();
        liveTime = flyObjectCfg.liveTime;
    }

    public override void FlyObjectUpdate(float dt)
    {
        searchReqIndex = MapCellManager.Instance.RequestSearch(mFlyObjectGob.transform.position, mSkillLogic.SkillCfg.searchRange, mAtkUnitLogic.OtherCampTypeInt);
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
        UpdateFlyPosition(dt);
        liveTime -= dt;
        if (liveTime < 0)
        {
            Die();
        }
    }

    public override void ArriveTarPos()
    {
        CreateArriveEffect(mFlyObjectGob.transform.position);
        BattleLogicDamageTools.DoDamage(mAtkUnitLogic, mSearchTargetUnitLogic, GetFinalDamage(mSearchTargetUnitLogic), targetUID, mSkillLogic.SkillCfg.dieType, mFlyObjectGob.transform.position);
        mSkillLogic.ApplyBuffs(mSearchTargetUnitLogic);
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
        else
        {
            if(randomIndex != -1)
            {
                if (randomIndex <= UnitManager.Instance.UnitList.Count - 1)
                {
                    UnitLogicBase tarUnit = UnitManager.Instance.UnitList[randomIndex];
                    if (tarUnit != null && tarUnit.IsDead == false && tarUnit.CampTypeInt != mAtkUnitLogic.CampTypeInt)
                    {
                        mSearchTargetUnitLogic = tarUnit;
                        targetUID = mSearchTargetUnitLogic.UId;
                        startFly();
                    }
                }
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
}
