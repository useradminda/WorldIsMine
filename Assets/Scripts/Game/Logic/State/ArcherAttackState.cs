/// <summary>弓手攻击状态包装器，负责碰撞生命周期并复用通用攻击逻辑。</summary>
public class ArcherAttackState : StateBase
{
    private readonly AttackState commonAttackState;
    private UnitLogicBase target;

    /// <summary>创建弓手攻击状态。</summary>
    public ArcherAttackState(UnitLogicBase unitLogic) : base(unitLogic)
    {
        commonAttackState = new AttackState(unitLogic);
    }

    public override EStateTyep StateType => EStateTyep.ArcherAttack;

    /// <summary>进入弓手攻击状态并关闭弓手碰撞。</summary>
    public override void EnterState(params object[] objects)
    {
        target = UnitLogic.SearchTarget;
        UnitLogic.TriggerMoveStop();
        UnitLogic.Agenter.collisionEnabled = false;
        commonAttackState.EnterState(objects);
    }

    /// <summary>转发通用攻击更新。</summary>
    public override void UpdateState(float dt)
    {
        commonAttackState.UpdateState(dt);
    }

    /// <summary>转发通用攻击延迟更新。</summary>
    public override void LateUpdateState(float dt)
    {
        commonAttackState.LateUpdateState(dt);
    }

    /// <summary>退出弓手攻击状态并恢复 Agent 碰撞。</summary>
    public override void ExitState()
    {
        commonAttackState.ExitState();
        UnitLogic.Agenter.collisionEnabled = true;
        ActiveArcherPositionRegistry.Release(UnitLogic, target);
        target = null;
    }
}
