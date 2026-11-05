using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单目标投石飞行器。
/// 适用于普通抛物线石块攻击，由 ProjectileJobManager 驱动到达目标位置后结算一次伤害。
/// </summary>
public class StoneFlyObject : FlyObjectLogicBase
{
    private bool damagePending;
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, flyUIndex);
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, tarPos, Vector3.zero);
        ProjectileJobManager.Instance.SpawnProjectile(atkUnitLogic.Index, searchTargetUnit.Index, oriPos, tarPos, mFlyObjectCfg.speed, searchTargetUnit.UId, attackDamage, flyUIndex, mFlyObjectGob.transform, flyObjectCfg.flyType);
    }

    public override void FlyObjectUpdate(float dt)
    {
    }

    public override void ArriveTarPos()
    {
        if (!damagePending)
        {
            reqSearchTar();
            damagePending = true;
        }
    }

    public override void FlyObjectLateUpdate(float dt)
    {
        if (!damagePending)
        {
            return;
        }
        setSearchTar();
        CreateArriveEffect(mTarPos);
        damage();
        die();
        damagePending = false;
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
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
        UnitTargetSnapshot.Capture(resultUnitIndexList, resultUnitUidList);
    }

    private void damage()
    {
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                if (!UnitTargetSnapshot.TryGet(resultUnitIndexList, resultUnitUidList, i, out UnitLogicBase tarUnitLogic) || tarUnitLogic.CampTypeInt == mAtkUnitLogic.CampTypeInt)
                {
                    continue;
                }
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, GetFinalDamage(tarUnitLogic), resultUnitUidList[i], mSkillLogic.SkillCfg.dieType, mTarPos);
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
