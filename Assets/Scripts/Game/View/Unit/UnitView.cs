using UnityEngine;

public class UnitView : IView
{
    private UnitLogicBase unitLogic;

    private string prefabName;
    public string PrefabName => prefabName;

    private GameObject aroundEffectObject;

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

    private ParalysisComponent paralysisComp;
    public ParalysisComponent ParalysisComp
    {
        get
        {
            if (paralysisComp == null)
            {
                paralysisComp = gameObject.GetOrAddComponent<ParalysisComponent>();
            }
            return paralysisComp;
        }
    }

    public void Init(UnitLogicBase unit, string prefabName)
    {
        this.prefabName = prefabName;
        this.unitLogic = unit;
        FootSmokeManager.Instance.StopForUnit(this);
        SlachComp.ExitSlash();

        // 城门是静态单位：只保留渲染和受击闪白，不创建/播放任何 Action 或死亡表现。
        if (unit != null && unit.UnitType == EUnitType.Wall)
        {
            stateType = EStateTyep.Idle;
            return;
        }

        ActionFlowComponent.SetAnimationSpeed(1f);
        SetAttackBuffColor(false);
        SetDissolveAmount(0f);
        InitAroundEffect(unit.SoliderCfg.aroundEffect);
    }

    public void SetAttackBuffColor(bool enabled)
    {
        RenderComponent renderComponent = gameObject.GetOrAddComponentInChild<RenderComponent>();
        renderComponent.SetPropertyBlockColor("_BuffColor", enabled ? new Color(1f, 0.68f, 0.05f, 1f) : Color.white);
        renderComponent.SetPropertyBlockFloat("_BuffColorStrength", enabled ? 0.85f : 0f);
    }

    public void SetDissolveAmount(float amount)
    {
        RenderComponent renderComponent = gameObject.GetOrAddComponentInChild<RenderComponent>();
        renderComponent.SetPropertyBlockFloat("_DissolveAmount", Mathf.Clamp01(amount));
    }

    public override void ViewInit()
    {

    }

    public override void ViewUpdate(float dt)
    {
        if (unitLogic != null && unitLogic.UnitType == EUnitType.Wall)
            return;
        updatePos(dt);
        updateRot(dt);
    }

    public override void ViewDestroy()
    {

    }

    public override void ViewRefuse()
    {
        FootSmokeManager.Instance.StopForUnit(this);
    }

    public void BeHitSlash()
    {
        BattleClashEffectManager.Instance.ReportHit(transform.position, unitLogic != null ? unitLogic.Prop.Radius : 0f);

        if (stateType != EStateTyep.Die)
        {
            if (unitLogic.UnitType == EUnitType.Wall)
            {
                SlachComp.SetSlash(Color.white, 0.1f);
                ShakeComp.SetShake(0.2f);
            }
            else
            {
                SlachComp.SetSlash(Color.white);
                ShakeComp.SetShake();
            }
           
        }
    }

    public void EnterState(EStateTyep stateType, params object[] paramsInfo)
    {
        if (unitLogic != null)
        {
            if (unitLogic.StateMachine.StateDirty)
            {
                this.stateType = stateType;
                if (aroundEffectObject != null)
                {
                    aroundEffectObject.SetActive(stateType == EStateTyep.Move);
                }
                if (stateType == EStateTyep.Move)
                {
                    ActionFlowComponent.PlayAction(EActionType.run);
                    FootSmokeManager.Instance.StartForSoldier(this, unitLogic);
                }
                else if (stateType == EStateTyep.Attack)
                {
                    FootSmokeManager.Instance.StopForUnit(this);
                }
                else if (stateType == EStateTyep.Skill)
                {
                    FootSmokeManager.Instance.StopForUnit(this);
                }
                else if (stateType == EStateTyep.Die)
                {
                    FootSmokeManager.Instance.StopForUnit(this);
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

    public void NotifyDeathViewFinished()
    {
        if (unitLogic != null)
        {
            unitLogic.RecycleAfterDeathView();
        }
    }

    public void PlayEffect(string prefabName, Vector3 pos, Vector3 forward, float time)
    {
        if (prefabName == "")
            return;
        GameObject go = UnitViewFactory.CreateGob(prefabName, pos, forward);
        go.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(time, prefabName);
    }

    public void PlayAttachEffect(string prefabName, float time)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        GameObject go = UnitViewFactory.CreateGob(prefabName, transform.position, transform.forward);
        if (go == null)
        {
            return;
        }

        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
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
        if (stateType == EStateTyep.Die || stateType == EStateTyep.Attack)
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
        if (dieType == "Tornado")
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
        else if (dieType == "Dissolve")
        {
            dieComp = gameObject.GetOrAddComponent<DissolveDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "Normal")
        {
            dieComp = gameObject.GetOrAddComponent<NormalDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "PushDie")
        {
            dieComp = gameObject.GetOrAddComponent<PushDieComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "PushUp")
        {
            dieComp = gameObject.GetOrAddComponent<PushUpComponent>();
            dieComp.SetUnitView(this);
        }
        else if (dieType == "PushCameraSide")
        {
            dieComp = gameObject.GetOrAddComponent<PushCameraSideComponent>();
            dieComp.SetUnitView(this);
        }
        else
        {
            dieComp = gameObject.GetOrAddComponent<NormalDieComponent>();
            dieComp.SetUnitView(this);
        }
    }


    /// <summary>创建或重新启用角色周身特效，模型缓存复用时不会重复创建。</summary>
    private void InitAroundEffect(string effectPath)
    {
        if (string.IsNullOrEmpty(effectPath))
        {
            if (aroundEffectObject != null)
            {
                aroundEffectObject.SetActive(false);
            }
            return;
        }

        if (aroundEffectObject != null)
        {
            aroundEffectObject.SetActive(false);
            return;
        }

        GameObject effectTemplate = Resources.Load<GameObject>(effectPath);
        if (effectTemplate == null)
        {
            Debug.LogError($"周身特效不存在，路径={effectPath}");
            return;
        }

        aroundEffectObject = Instantiate(effectTemplate, transform, false);
        aroundEffectObject.transform.localPosition = Vector3.zero;
        aroundEffectObject.transform.localRotation = Quaternion.identity;
        aroundEffectObject.SetActive(false);
    }

}
