using System.Collections;
using UnityEngine;

public class RecycleGobComponent : MonoBehaviour
{
    private string prefabName = "";
    private float time = -1;
    private Coroutine recycleCoroutine;
    private bool isRecycling;

    /// <summary>设置对象自动回收时间；非正数表示由外部管理器负责回收。</summary>
    public void SetRecycleGobTime(float time, string prefabName)
    {
        StopRecycleCoroutine();
        isRecycling = false;
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
        // 父级释放者被回收时，子特效的协程会被 Unity 自动停止；这里补一次回收，避免特效重新启用后永久显示。
        if (gameObject.activeSelf && !string.IsNullOrWhiteSpace(prefabName))
        {
            RecycleNow(false);
        }
    }

    /// <summary>等待结束后将对象放回指定预制体缓存。</summary>
    private IEnumerator waitRecycle()
    {
        yield return new WaitForSeconds(time);
        recycleCoroutine = null;
        RecycleNow(true);
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

    /// <summary>按当前调用场景决定是否解除父级，再将特效对象放回对象池。</summary>
    private void RecycleNow(bool detachParent)
    {
        if (isRecycling || string.IsNullOrWhiteSpace(prefabName))
        {
            return;
        }

        isRecycling = true;
        if (detachParent)
        {
            transform.SetParent(null, true);
        }
        UnitViewFactory.RemoveGob(prefabName, gameObject);
        isRecycling = false;
    }
}
