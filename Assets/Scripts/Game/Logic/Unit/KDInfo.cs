using AillieoUtils;
public class KDInfo : IPositionProvider
{
    private UnitBase ub;
    public UnitBase UB => ub;

    public AillieoUtils.Vector2 position
    {
        get
        {
            return new AillieoUtils.Vector2(UB.Agenter.pos.x, UB.Agenter.pos.y);
        }
    }

    public void SetUnit(UnitBase ub)
    {
        this.ub = ub;
    }
}
