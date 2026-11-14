
public class DieState : StateBase
{
    private float dieActionTime = 0;
    public override EStateTyep StateType { get { return EStateTyep.Die; } }

    public DieState(UnitLogicBase ulb) : base(ulb)
    {
      
    }

    public override void EnterState(params object[] objects)
    {
        // 在死亡清理 Buff 前保存冰冻状态，供死亡表现选择碎裂效果。
        bool wasFrozen = UnitLogic.BuffLogicMachine.HasBuffType(BuffDefine.EBuffType.Freeze);
        UnitLogic.TriggerDie();
        // 旧调用：清理 Buff 后直接把原始死亡参数传给表现层，无法保留冰冻状态。
        // UnitLogic.UnitView.EnterState(EStateTyep.Die, objects);
        object[] dieObjects = new object[objects.Length + 1];
        objects.CopyTo(dieObjects, 0);
        dieObjects[objects.Length] = wasFrozen;
        UnitLogic.UnitView.EnterState(EStateTyep.Die, dieObjects);
    }

    public override void UpdateState(float dt)
    {
       
    }

    public override void ExitState()
    {

    }

    public override void LateUpdateState(float dt)
    {
       
    }
}
