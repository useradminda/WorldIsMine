using UnityEngine;
using UnityEngine.UIElements;

public class UnitView : IView
{
    private UnitLogicBase unitLogic;

    private string prefabName;
    public string PrefabName => prefabName;

    private GameObject aroundEffectObject;

    private EStateTyep stateType;

    private ActionFlow actionFlow;
    private BuffColorBlinkComponent buffColorBlinkComponent;
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

    private AttackStopComponent attackStopComp;
    public AttackStopComponent AttackStopComp
    {
        get
        {
            if (attackStopComp == null)
            {
                attackStopComp = gameObject.GetOrAddComponent<AttackStopComponent>();
            }
            return attackStopComp;
        }
    }

    public void Init(UnitLogicBase unit, string prefabName)
    {
        this.prefabName = prefabName;
        this.unitLogic = unit;
        if (attackStopComp != null)
        {
            attackStopComp.Cancel();
        }
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

    /// <summary>
    /// 开启或停止攻击 Buff 的金色闪烁表现，并在停止时清理材质属性块。
    /// </summary>
    public void SetAttackBuffColor(bool enabled)
    {
        if (enabled)
        {
            if (buffColorBlinkComponent == null)
            {
                buffColorBlinkComponent = gameObject.GetOrAddComponentInChild<BuffColorBlinkComponent>();
            }

            buffColorBlinkComponent.StartBlink();
            return;
        }

        if (buffColorBlinkComponent != null)
        {
            buffColorBlinkComponent.StopBlink();
            return;
        }

        RenderComponent renderComponent = gameObject.GetOrAddComponentInChild<RenderComponent>();
        renderComponent.SetPropertyBlockColor("_BuffColor", Color.white);
        renderComponent.SetPropertyBlockFloat("_BuffColorStrength", 0f);
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

    /// <summary>
    /// 清理单位 View 的临时表现组件，避免缓存复用时继续执行上一单位的死亡逻辑。
    /// </summary>
    public void ResetForRecycle()
    {
        ViewRefuse();
        AttackStopComp.Cancel();
        DieBaseComponent dieComponent = gameObject.GetComponent<DieBaseComponent>();
        if (dieComponent != null)
        {
            dieComponent.ClearForRecycle();
        }
    }

    public void BeHitSlash()
    {
        BeHitSlash(transform.position);
    }

    /// <summary>
    /// 播放受击闪白、抖动和交战随机特效，支持使用实际受击点。
    /// </summary>
    public void BeHitSlash(Vector3 hitPoint)
    {
        Vector3 effectPoint = hitPoint.sqrMagnitude > 0.0001f ? hitPoint : transform.position;
        BattleClashEffectManager.Instance.ReportHit(effectPoint, unitLogic != null ? unitLogic.Prop.Radius : 0f);

        if (stateType != EStateTyep.Die)
        {
            if (unitLogic.UnitType == EUnitType.Wall)
            {
                SlachComp.SetSlash(Color.white, 0.1f, 0.3f);
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
                    bool wasFrozen = paramsInfo.Length > 2
                        && paramsInfo[2] is bool
                        && (bool)paramsInfo[2];
                    // 旧调用：只传入死亡类型和受击点，无法知道死亡前是否处于冰冻状态。
                    // enterDie(dieType, (Vector3)paramsInfo[1]);
                    enterDie(dieType, (Vector3)paramsInfo[1], wasFrozen);
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

    public void PlayEffect(string prefabName, Vector3 pos, float time)
    {
        if (prefabName == "")
            return;
        GameObject go = UnitViewFactory.CreateGob(prefabName, pos, transform.forward);
        go.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(time, prefabName);
    }

  
    public void PlayAttachEffect(string prefabName, float time, Vector3 localPosition)
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
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(time, prefabName);
    }

    public void PlayNoAttachEffect(string prefabName, float time)
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

        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation.normalized;
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

    /// <summary>
    /// 立即同步角色模型位置，供传送等不需要位置插值的逻辑使用。
    /// </summary>
    public void SnapToPosition(Vector3 position)
    {
        transform.position = position;
        tarPos = position;
        transPos = position;
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

    /// <summary>
    /// 根据死亡类型创建死亡表现；拥有冰冻 Buff 的角色优先使用碎裂死亡效果。
    /// </summary>
    private void enterDie(string dieType, Vector3 beHitPoint, bool wasFrozen = false)
    {
        DieBaseComponent dieComp;
        // 旧逻辑：进入死亡表现后再查询 Buff；此时 Buff 可能已经被 TriggerDie 清理。
        // bool isFrozen = unitLogic != null
        //     && unitLogic.BuffLogicMachine.HasBuffType(BuffDefine.EBuffType.Freeze);
        bool isFrozen = wasFrozen || (unitLogic != null
            && unitLogic.BuffLogicMachine.HasBuffType(BuffDefine.EBuffType.Freeze));
        if (isFrozen || dieType == "Split")
        {
            dieComp = gameObject.GetOrAddComponent<SplitDieComponent>();
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
        else if (dieType == "TranslateDie")
        {
            dieComp = gameObject.GetOrAddComponent<TranslateDieComponent>();
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

