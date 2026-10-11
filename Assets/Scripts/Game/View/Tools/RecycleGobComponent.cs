using System.Collections;
using UnityEngine;

public class RecycleGobComponent : MonoBehaviour
{
    private string prefabName = "";
    private float time = -1;
    private Coroutine recycleCoroutine;

    /// <summary>设置对象自动回收时间；非正数表示由外部管理器负责回收。</summary>
    public void SetRecycleGobTime(float time, string prefabName)
    {
        StopRecycleCoroutine();
        this.prefabName = prefabName;
        this.time = time;
        if (time > 0f)
        {
            recycleCoroutine = StartCoroutine(waitRecycle());
        }
    }

    /// <summary>对象禁用时停止旧计时，避免缓存对象被重复回收。</summary>
    private void OnDisable()
    {
        StopRecycleCoroutine();
    }

    /// <summary>等待结束后将对象放回指定预制体缓存。</summary>
    private IEnumerator waitRecycle()
    {
        yield return new WaitForSeconds(time);
        recycleCoroutine = null;
        UnitViewFactory.RemoveGob(prefabName, this.gameObject);
    }

    /// <summary>停止当前自动回收协程。</summary>
    private void StopRecycleCoroutine()
    {
        if (recycleCoroutine != null)
        {
            StopCoroutine(recycleCoroutine);
            recycleCoroutine = null;
        }
    }
}
