using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>红蓝双方使用物品召唤士兵时的提示面板，按阵营维护先进先出队列。</summary>
public class BattleSoldierCallPanel : MonoBehaviour
{
    /// <summary>单条士兵召唤提示的展示时长，单位为秒。</summary>
    [SerializeField] private float soldierDisplayDuration = 1f;
    [SerializeField] private BattleSoldierCallInfoItem redSoldierCallItem;
    [SerializeField] private BattleSoldierCallInfoItem blueSoldierCallItem;

    private readonly Queue<SoldierCallQueueData> redQueue = new Queue<SoldierCallQueueData>();
    private readonly Queue<SoldierCallQueueData> blueQueue = new Queue<SoldierCallQueueData>();
    private Coroutine redDisplayCoroutine;
    private Coroutine blueDisplayCoroutine;

    /// <summary>一条士兵召唤提示队列数据。</summary>
    private sealed class SoldierCallQueueData
    {
        public readonly int SoldierCfgId;
        public readonly int Count;
        public readonly BattlePlayer Player;
        public readonly string IconPath;

        /// <summary>创建一条士兵召唤提示数据。</summary>
        public SoldierCallQueueData(int soldierCfgId, int count, BattlePlayer player, string iconPath)
        {
            SoldierCfgId = soldierCfgId;
            Count = count;
            Player = player;
            IconPath = iconPath;
        }
    }

    /// <summary>销毁面板时停止红蓝双方的展示协程。</summary>
    private void OnDestroy()
    {
        StopDisplayCoroutine(ref redDisplayCoroutine);
        StopDisplayCoroutine(ref blueDisplayCoroutine);
    }

    /// <summary>将一次士兵召唤提示加入指定阵营的先进先出队列。</summary>
    public void EnqueueSoldier(ECampType campType, int soldierCfgId, int count, BattlePlayer player, string iconPath)
    {
        SoldierCallQueueData data = new SoldierCallQueueData(soldierCfgId, count, player, iconPath);
        if (campType == ECampType.Red)
        {
            redQueue.Enqueue(data);
            if (redDisplayCoroutine == null) redDisplayCoroutine = StartCoroutine(DisplayQueue(ECampType.Red));
            return;
        }

        blueQueue.Enqueue(data);
        if (blueDisplayCoroutine == null) blueDisplayCoroutine = StartCoroutine(DisplayQueue(ECampType.Blue));
    }

    /// <summary>按阵营依次显示士兵召唤提示，并处理下一条队列数据。</summary>
    private IEnumerator DisplayQueue(ECampType campType)
    {
        Queue<SoldierCallQueueData> queue = campType == ECampType.Red ? redQueue : blueQueue;
        BattleSoldierCallInfoItem item = campType == ECampType.Red ? redSoldierCallItem : blueSoldierCallItem;
        while (queue.Count > 0)
        {
            SoldierCallQueueData data = queue.Dequeue();
            if (item == null) continue;
            ShowItem(item, data);
            yield return new WaitForSeconds(Mathf.Max(0f, soldierDisplayDuration));
            HideItem(item);
        }

        if (campType == ECampType.Red) redDisplayCoroutine = null;
        else blueDisplayCoroutine = null;
    }

    /// <summary>设置当前提示内容、加载头像并重新播放挂载的 UI 动效。</summary>
    private void ShowItem(BattleSoldierCallInfoItem item, SoldierCallQueueData data)
    {
        SoliderCfg cfg = SoliderCfgConfig.Ins.SearchById(data.SoldierCfgId);
        string iconPath = data.IconPath;
        if (string.IsNullOrWhiteSpace(iconPath) && cfg != null) iconPath = cfg.headImg;
        item.gameObject.SetActive(true);
        item.ClearData();
        item.SetData(data.SoldierCfgId, cfg, data.Count, data.Player, iconPath);
        item.transform.SetAsLastSibling();
        item.RestartDisplay();
    }

    /// <summary>关闭当前提示并清理显示数据。</summary>
    private void HideItem(BattleSoldierCallInfoItem item)
    {
        item.ClearData();
        item.gameObject.SetActive(false);
    }

    /// <summary>停止指定展示协程并清空引用。</summary>
    private void StopDisplayCoroutine(ref Coroutine coroutine)
    {
        if (coroutine == null) return;
        StopCoroutine(coroutine);
        coroutine = null;
    }
}
