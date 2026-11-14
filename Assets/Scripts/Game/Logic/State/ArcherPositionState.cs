/// <summary>弓手攻击位置状态，保持 Agent 移动直到当前位置不与攻击中的弓手重叠。</summary>
public class ArcherPositionState : StateBase
{
    private UnitLogicBase target;
    private int targetUid;

    /// <summary>创建弓手位置状态。</summary>
    public ArcherPositionState(UnitLogicBase unitLogic) : base(unitLogic)
    {
    }

    public override EStateTyep StateType => EStateTyep.ArcherPosition;

    /// <summary>进入位置搜索状态并开启 Agent。</summary>
    public override void EnterState(params object[] objects)
    {
        target = UnitLogic.SearchTarget;
        targetUid = target != null ? target.UId : -1;
        UnitLogic.TriggerMove();
        UnitLogic.Agenter.collisionEnabled = true;
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
    }

    /// <summary>保持向目标移动，当前位置合法后切换到弓手攻击状态。</summary>
    public override void UpdateState(float dt)
    {
        if (target == null || target.IsDead || target.UId != targetUid)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }

        if (!ActiveArcherPositionRegistry.TryRegisterCurrentPosition(UnitLogic, target))
        {
            UnitLogic.AgentMoveForward();
            return;
        }

        UnitLogic.StateMachine.ChangeState(EStateTyep.ArcherAttack);
    }

    /// <summary>弓手位置状态不需要延迟阶段。</summary>
    public override void LateUpdateState(float dt)
    {
    }

    /// <summary>退出位置状态时清理临时引用；成功登记的位置由弓手攻击状态释放。</summary>
    public override void ExitState()
    {
        target = null;
        targetUid = -1;
    }
}
