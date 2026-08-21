using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneFlyObject : FlyObjectLogicBase
{
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int damage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, damage, flyUIndex);
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, tarPos, Vector3.zero);
        ProjectileJobManager.Instance.SpawnProjectile(atkUnitLogic.Index, searchTargetUnit.Index, oriPos, tarPos, mFlyObjectCfg.speed, searchTargetUnit.UId, damage, flyUIndex, mFlyObjectGob.transform, flyObjectCfg.flyType);
    }

    public override void FlyObjectUpdate(float dt)
    {
        reqSearchTar();
        setSearchTar();   
    }

    public override void ArriveTarPos()
    {
        damage();
        die();
    }

    private int searchReqIndex = -1;
    List<int> resultUnitIndexList = new List<int>();
    private int neastIndex = -1;
    private void reqSearchTar()
    {
        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    {
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex);
    }

    private void damage()
    {
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, mDamage, tarUnitLogic.UId);
            }
        }
    }

    private void die()
    {
        UnitFactory.RemoveFlyObjectLogic(this);
        UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
    }
}
