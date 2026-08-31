using System.Collections;
using UnityEngine;

public class RecycleGobComponent : MonoBehaviour
{
    private string prefabName = "";
    private float time = -1;
    public void SetRecycleGobTime(float time, string prefabName)
    {
        this.prefabName = prefabName;
        if (time <= 0)
        {
            this.time = time;
        }
        StartCoroutine(waitRecycle());
    }

    IEnumerator waitRecycle()
    {
        yield return new WaitForSeconds(time);
        UnitViewFactory.RemoveGob(prefabName, this.gameObject);
    }
}
