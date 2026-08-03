using AillieoUtils;
public class KDInfo : IPositionProvider
{
    private UnitLogicBase ub;
    public UnitLogicBase UB => ub;

    public AillieoUtils.Vector2 position
    {
        get
        {
            return new AillieoUtils.Vector2(UB.Agenter.pos.x, UB.Agenter.pos.z);
        }
    }

    public void SetUnit(UnitLogicBase ub)
    {
        this.ub = ub;
    }
}
