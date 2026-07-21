
using UnityEngine;
public class UnitView : IView
{
    private UnitLogicBase unitLogic;
    public void Init(UnitLogicBase unit)
    {
        this.unitLogic = unit;
    }

    public override void ViewInit()
    {

    }

    public override void ViewUpdate()
    {
        updatePos();
        updateRot();
    }

    public override void ViewDestroy()
    {

    }

    public override void ViewRefuse()
    {

    }

    private void updatePos()
    {
        transform.position = Vector3.Lerp(transform.position, unitLogic.Agenter.pos, Time.deltaTime * 3);
    }

    private void updateRot()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(unitLogic.Agenter.prefVelocity), Time.deltaTime * 3);
    }
}
