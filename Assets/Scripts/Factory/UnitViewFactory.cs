using System;
using UnityEngine;

public class UnitViewFactory
{
    public static UnitView CreateUnitView(
        string prefab,
        Vector3 initPos,
        Vector3 initForward,
        UnitLogicBase unitBase)
    {
        GameObject template = Resources.Load<GameObject>(prefab);
       
        Quaternion rotation = initForward.sqrMagnitude > 0f
            ? Quaternion.LookRotation(initForward.normalized)
            : Quaternion.identity;
        GameObject unitGob = UnityEngine.Object.Instantiate(
            template,
            initPos,
            rotation);
        UnitView unitView = unitGob.GetOrAddComponent<UnitView>();
        unitView.Init(unitBase);
        return unitView;
    }
}
