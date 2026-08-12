
using System.Collections.Generic;
using UnityEngine;
public class ArrowFlyObject : FlyObjectLogicBase
{
    //private bool flyState = false;
    //private float curFlyTime = 0;
    //private float flyTime = 0;
    //private float height = 0;
    //private float dis = 0;

    //private Vector3 curPos;
    //private Vector3 curDir;
    //private Vector3 lastPos;

  
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, SkillLogicBase skillLogic)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, skillLogic);
        ProjectileJobManager.Instance.SpawnProjectile(atkUnitLogic.Index, targetLogicList[0].Index, oriPos, tarPos, mFlyObjectCfg.speed, 0);
    }

    public override void FlyObjectUpdate(float dt)
    {
        //initFlyObjectInfo();
        //updateFlyTime(dt);
    }

    private void initFlyObjectInfo()
    {
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
    }

    private void updateFlyTime(float dt)
    {
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
    }

    public override void TouchTarUnit()
    {
        //BattleLogicDamageTools.DoDamage(mAtkUnitLogic, mTargetLogicList, mSkillLogic);
    }
}
