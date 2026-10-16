using System.Collections.Generic;
using UnityEngine;

public class GlobalArrowFlyObject : FlyObjectLogicBase
{
    public class DamageContext
    {
        private readonly HashSet<int> damagedUnitUids = new HashSet<int>();

        public bool TryMarkDamaged(int unitUid)
        {
            return damagedUnitUids.Add(unitUid);
        }
    }

    private const float ArrivedRemainTime = 2f;

    private readonly List<int> resultUnitIndexList = new List<int>();
    private readonly List<int> resultUnitUidList = new List<int>();
    private DamageContext damageContext;
    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private float remainTime;
    private bool arrived;
    private bool damagePending;

    public void SetDamageContext(DamageContext context)
    {
        damageContext = context;
    }

    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, flyUIndex);
        arrived = false;
        remainTime = 0f;
        mFlyObjectGob = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, mOriPos, mTarPos - mOriPos);
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (mFlyObjectGob == null)
        {
            return;
        }

        if (arrived)
        {
            remainTime -= dt;
            if (remainTime <= 0f)
            {
                Die();
            }
            return;
        }

        Vector3 currentPosition = mFlyObjectGob.transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, mTarPos, Mathf.Max(0.01f, mFlyObjectCfg.speed) * dt);
        Vector3 direction = nextPosition - currentPosition;
        mFlyObjectGob.transform.position = nextPosition;
        if (direction.sqrMagnitude > 0.0001f)
        {
            mFlyObjectGob.transform.rotation = Quaternion.LookRotation(direction);
        }

        if ((nextPosition - mTarPos).sqrMagnitude <= 0.0001f)
        {
            ArriveTarPos();
        }
    }

    public override void ArriveTarPos()
    {
        if (arrived)
        {
            return;
        }

        arrived = true;
        remainTime = ArrivedRemainTime;
        RequestDamageSearch();
        damagePending = true;
    }

    public override void FlyObjectLateUpdate(float dt)
    {
        if (!damagePending)
        {
            return;
        }
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
        UnitTargetSnapshot.Capture(resultUnitIndexList, resultUnitUidList);
        CreateArriveEffect(mTarPos);
        DamageTargets();
        damagePending = false;
    }

    private void RequestDamageSearch()
    {
        if (mSkillLogic == null || mAtkUnitLogic == null)
        {
            return;
        }

        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void DamageTargets()
    {
        if (mSkillLogic == null || mAtkUnitLogic == null || resultUnitIndexList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < resultUnitIndexList.Count; i++)
        {
            int unitIndex = resultUnitIndexList[i];
            if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }

            if (!UnitTargetSnapshot.TryGet(resultUnitIndexList, resultUnitUidList, i, out UnitLogicBase target) || target.CampType == mAtkUnitLogic.CampType)
            {
                continue;
            }
            if (damageContext != null && !damageContext.TryMarkDamaged(target.UId))
            {
                continue;
            }

            int damage = Mathf.Max(1, Mathf.CeilToInt(target.SoliderCfg.hp / 3f));
            BattleLogicDamageTools.DoDamage(mAtkUnitLogic, target, -damage, target.UId, mSkillLogic.SkillCfg.dieType, mTarPos);
            mSkillLogic.ApplyBuffs(target);
        }
    }

    private void Die()
    {
        if (mFlyObjectGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }

        UnitFactory.RemoveFlyObjectLogic(this);
    }
}
