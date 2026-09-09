
using UnityEngine;
using System.Collections.Generic;
public class UnitViewFactory
{
    private static Dictionary<string, List<GameObject>> catchGob = new Dictionary<string, List<GameObject>>();

    public static UnitView CreateUnitView(string prefab, Vector3 initPos, Vector3 initForward, UnitLogicBase unitBase)
    {
        GameObject unitGob = CreateGob(prefab, initPos, initForward);
        unitGob.name = prefab + "_" + unitBase.UId;
        UnitView unitView = unitGob.transform.GetChild(0).GetOrAddComponent<UnitView>();
        unitView.Init(unitBase, prefab);
        return unitView;
    }

    public static GameObject CreateGob(string prefab, Vector3 initPos, Vector3 initForward)
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
                Quaternion qua1 = Quaternion.LookRotation(initForward);
                unitGob.transform.rotation = qua1;
                unitGob.gameObject.SetActive(true);
                return unitGob;
            }
        }   
        GameObject template = Resources.Load<GameObject>(prefab);
        Quaternion qua = Quaternion.LookRotation(initForward);
        unitGob = UnityEngine.Object.Instantiate(template, initPos, qua);
        return unitGob;
    }

    public static void RemoveGob(string prefabName, GameObject gob)
    {
        //  gob.transform.position = new Vector3(0, 1000, 0);
        gob.transform.gameObject.SetActive(false);
        if (!catchGob.ContainsKey(prefabName))
        {
            catchGob.Add(prefabName, new List<GameObject>());
        }
        catchGob[prefabName].Add(gob);
    }

    // 移除unitView
    public static void RemoveUnitView(UnitView unitView)
    {
        UnitViewManager.Instance.RemoveSwapBackUnitView(unitView);
        RemoveGob(unitView.PrefabName, unitView.gameObject);
    }
}
