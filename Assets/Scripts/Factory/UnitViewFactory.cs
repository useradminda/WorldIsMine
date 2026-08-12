
using UnityEngine;

public class UnitViewFactory
{
    public static UnitView CreateUnitView(string prefab, Vector3 initPos, Vector3 initForward, UnitLogicBase unitBase)
    {
        GameObject template = Resources.Load<GameObject>(prefab);
        Quaternion qua = Quaternion.LookRotation(initForward);
        GameObject unitGob = UnityEngine.Object.Instantiate(template, initPos, qua);
        UnitView unitView = unitGob.GetOrAddComponent<UnitView>();
        unitView.Init(unitBase);
        return unitView;
    }

    public static GameObject CreateGob(string prefab, Vector3 initPos, Vector3 initForward)
    {
        GameObject template = Resources.Load<GameObject>(prefab);
        Quaternion qua = Quaternion.LookRotation(initForward);
        GameObject unitGob = UnityEngine.Object.Instantiate(template, initPos, qua);
        return unitGob;
    }
}
