using UnityEngine;

public class UnitView : IView
{
    private UnitLogicBase unitLogic;

    private string prefabName;
    public string PrefabName => prefabName;

    private EStateTyep stateType;

    private ActionFlow actionFlow;
    public ActionFlow ActionFlowComponent
    {
        get
        {
            if (actionFlow == null)
            {
                actionFlow = gameObject.GetOrAddComponentInChild<ActionFlow>();
            }
            return actionFlow;
        }
    }

    private SlashComponent slashComp;
    public SlashComponent SlachComp
    {
        get
        {
            if(slashComp == null)
                slashComp = gameObject.GetOrAddComponent<SlashComponent>();
            return slashComp;
        }
    }

    private ShakeComponent shakeComp;
    public ShakeComponent ShakeComp
    {
        get
        {
            if (shakeComp == null)
            {
                GameObject shakeObject = gameObject;
                if (transform.childCount > 0)
                {
                    shakeObject = transform.GetChild(0).gameObject;
                }

                shakeComp = shakeObject.GetOrAddComponent<ShakeComponent>();
            }
            return shakeComp;
        }
    }

    private FreezeComponent freezeComp;
    public FreezeComponent FreezeComp
    {
        get
        {
            if (freezeComp == null)
                freezeComp = gameObject.GetOrAddComponent<FreezeComponent>();
            return freezeComp;
        }
    }

    public void Init(UnitLogicBase unit, string prefabName)
    {
        this.prefabName = prefabName;
        this.unitLogic = unit;
        SlachComp.ExitSlash();
    }

    public override void ViewInit()
    {

    }

    public override void ViewUpdate(float dt)
    {
        updatePos(dt);
        updateRot(dt);
    }

    public override void ViewDestroy()
    {

    }

    public override void ViewRefuse()
    {

    }

    public void BeHitSlash()
    {
        if (stateType != EStateTyep.Die)
        {
            SlachComp.SetSlash();
            ShakeComp.SetShake();
        }
    }

    public void EnterState(EStateTyep stateType, params object[] paramsInfo)
    {
        if (unitLogic != null)
        {
            if (unitLogic.StateMachine.StateDirty)
            {
                this.stateType = stateType;
                if (stateType == EStateTyep.Move)
                {
                    ActionFlowComponent.PlayAction(EActionType.run);
                }
                else if (stateType == EStateTyep.Attack)
                {
                   
                }
                else if (stateType == EStateTyep.Die)
                {
                    string dieType = paramsInfo[0].ToString();
                    enterDie(dieType, (Vector3)paramsInfo[1]);
                }
                unitLogic.StateMachine.ClearStateDirty();
            }
        }
    }

    // get dead time
    public float GetDieTime()
    {
        return ActionFlowComponent.GetAnimLen(EActionType.die);
    }

    public void PlayEffect(string prefabName, Vector3 pos, Vector3 forward, float time)
    {
        if (prefabName == "")
            return;
        GameObject go = UnitViewFactory.CreateGob(prefabName, pos, forward);
        go.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(time, prefabName);
    }

    public void SetForwardForce(Vector3 foward)
    {
        if (foward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion forceRotation = Quaternion.LookRotation(foward);
        transform.rotation = forceRotation;
        targetQ = forceRotation;
        transQ = forceRotation;
    }

    private Vector3 tarPos;
    private Vector3 transPos;
    private void updatePos(float dt)
    {
        if (stateType == EStateTyep.Die)
            return;
        if (unitLogic.Agenter.DirtyPos == true)
        {
            tarPos = unitLogic.Agenter.pos;
            unitLogic.Agenter.DirtyPos = false;
            transPos = transform.position;
        }
        if (tarPos != transPos)
        {
            //if ((tarPos - transform.position).sqrMagnitude < 0.0004f)
            //{
            //    transform.position = tarPos;
            //    transPos = transform.position;
            //}
            //else
            {
                transform.position = Vector3.Lerp(transform.position, unitLogic.Agenter.pos, dt * 10);
                transPos = transform.position;
            }
        }
    }
    private Quaternion targetQ;
    private Quaternion transQ;
    private void updateRot(float dt)
    {
        if (stateType == EStateTyep.Die)
            return;

        Vector3 targetForward = unitLogic.TargetForward;
        if (targetForward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        targetQ = Quaternion.LookRotation(targetForward);

        float rotationDot = Mathf.Abs(Quaternion.Dot(transform.rotation, targetQ));
        if (rotationDot >= 0.9995f)
        {
            transform.rotation = targetQ;
            transQ = targetQ;
            return;
        }

        float rotateFactor = Mathf.Clamp01(dt * 3f);
        transQ = Quaternion.Slerp(transform.rotation, targetQ, rotateFactor);
        transform.rotation = transQ;
    }

    private void enterDie(string dieType, Vector3 beHitPoint)
    {
        DieBaseComponent dieComp;
         if (dieType == "Bounce")
        {
            dieComp = gameObject.GetOrAddComponent<BounceDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "GroundSmash")
        {
            dieComp = gameObject.GetOrAddComponent<GroundSmashDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "Tornado")
        {
            dieComp = gameObject.GetOrAddComponent<TornadoDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if(dieType == "Explosion")
        {
            dieComp = gameObject.GetOrAddComponent<ExplosionDieComponent>();
            ((ExplosionDieComponent)dieComp).SetExplosionPoint(beHitPoint);
            dieComp.SetUnitView(this);
        }
        else if (dieType == "Normal")
        {
            dieComp = gameObject.GetOrAddComponent<NormalDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "RollDie")
        {
            dieComp = gameObject.GetOrAddComponent<RollDieComponent>();
            dieComp.SetUnitView(this);
        }
        else
        {
            dieComp = gameObject.GetOrAddComponent<NormalDieComponent>();
            dieComp.SetUnitView(this);
        }
    }
}
