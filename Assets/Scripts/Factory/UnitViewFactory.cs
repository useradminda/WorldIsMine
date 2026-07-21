using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitViewFactory
{
    public static UnitView CreateUnitView(string prefab, Vector3 initPos, Vector3 initForward, UnitLogicBase unitBase)
    {
        GameObject unitGob = Resources.Load<GameObject>(prefab);
        unitGob.transform.position = initPos;
        unitGob.transform.rotation = Quaternion.Euler(initForward);
        UnitView unitView = unitGob.GetOrAddComponent<UnitView>();
        unitView.Init(unitBase);
        return unitView;
    }
}
