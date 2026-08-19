
using System.Collections.Generic;
using UnityEngine;
public class ArrowFlyObject : FlyObjectLogicBase
{ 
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int damage)
    {
        oriPos = oriPos + new Vector3(0, 0.5f, 0);
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, damage);
        ProjectileJobManager.Instance.SpawnProjectile(atkUnitLogic.Index, searchTargetUnit.Index, oriPos, tarPos, mFlyObjectCfg.speed, searchTargetUnit.UId, damage);
    }

    //public override void FlyObjectUpdate(float dt)
    //{
    //    initFlyObjectInfo();
    //    updateFlyTime(dt);
    //}

    //private void initFlyObjectInfo()
    //{
        //if (flyState == false)
        //{
        //    curFlyTime = 0;
        //    dis = Vector3.Distance(new Vector3(mOriPos.x, 0, mOriPos.z), new Vector3(mTarPos.x, 0, mTarPos.z));
        //    dis = Mathf.Max(dis, 0.05f);
        //    flyTime = dis / mFlyObjectCfg.speed;
        //    // 防止距离太近
        //    height = Mathf.Sqrt(dis) * 1.5f;

        //    lastPos = mOriPos;
        //    flyState = true;
        //}
    //}

    //private void updateFlyTime(float dt)
    //{
        //if (!flyState)
        //    return;
        //curFlyTime += dt;
        //float t = Mathf.Clamp01(curFlyTime / flyTime);

        //// 水平移动（包含Y插值）
        //curPos = Vector3.Lerp(new Vector3(mOriPos.x, 0, mOriPos.z), new Vector3(mTarPos.x, 0, mTarPos.z), t);
        //// 抛物线
        //curPos.y += Mathf.Sin(t * Mathf.PI) * height;
        //curDir = curPos - lastPos;
        //lastPos = curPos;

        //if (curFlyTime > flyTime)
        //{
        //    flyState = false;
        //    TouchTarUnit();
        //}
    //}

    //public override void TouchTarUnit()
    //{
    //    BattleLogicDamageTools.DoDamage(mAtkUnitLogic, mTargetLogicList, mSkillLogic);
    //}
}
