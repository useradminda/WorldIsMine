
using UnityEngine;
using System.Collections.Generic;
public class UnitViewFactory
{
    private static Dictionary<string, List<GameObject>> catchGob = new Dictionary<string, List<GameObject>>();

    public static UnitView CreateUnitView(string prefab, Vector3 initPos, Vector3 initForward, UnitLogicBase unitBase)
    {
        GameObject unitGob = null;
        if (catchGob.ContainsKey(prefab))
        {
            List<GameObject> catchGobList = catchGob[prefab];
            if (catchGobList.Count > 0)
            {
                unitGob = catchGobList[catchGobList.Count - 1];
                catchGobList.RemoveAt(catchGobList.Count - 1);
                unitGob.transform.position = initPos;
                Quaternion qua = Quaternion.LookRotation(initForward);
                unitGob.transform.rotation = qua;
            }
        }
        if (unitGob == null)
        {
            GameObject template = Resources.Load<GameObject>(prefab);
            Quaternion qua = Quaternion.LookRotation(initForward);
            unitGob = UnityEngine.Object.Instantiate(template, initPos, qua);
        }
        unitGob.name = prefab + "_" + unitBase.UId;
        UnitView unitView = unitGob.GetOrAddComponent<UnitView>();
        unitView.Init(unitBase, prefab);
        return unitView;
    }

    public static GameObject CreateGob(string prefab, Vector3 initPos, Vector3 initForward)
    {
        GameObject template = Resources.Load<GameObject>(prefab);
        Quaternion qua = Quaternion.LookRotation(initForward);
        GameObject unitGob = UnityEngine.Object.Instantiate(template, initPos, qua);
        return unitGob;
    }

    // 移除unitView
    public static void RemoveUnitView(UnitView unitView)
    {
        UnitViewManager.Instance.RemoveSwapBackUnitView(unitView);
        unitView.transform.position = new Vector3(0, 1000, 0);
        unitView.transform.gameObject.SetActive(false);
        if(!catchGob.ContainsKey(unitView.PrefabName))
        {
            catchGob.Add(unitView.PrefabName, new List<GameObject>());
        }
        catchGob[unitView.PrefabName].Add(unitView.transform.gameObject);
    }
}
