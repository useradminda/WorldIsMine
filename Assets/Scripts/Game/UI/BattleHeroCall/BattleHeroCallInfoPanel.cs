using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 红蓝双方召唤英雄提示面板。
/// 两个阵营各自维护先进先出的英雄召唤队列，展示结束后自动播放本阵营的下一个提示组件。
/// </summary>
public class BattleHeroCallInfoPanel : MonoBehaviour
{
    /// <summary>英雄召唤提示单次展示时长，单位为秒。</summary>
    private const float HeroDisplayDuration = 2.5f;

    [SerializeField] private BattleHeroCallInfoItem redHeroCallItem;
    [SerializeField] private BattleHeroCallInfoItem blueHeroCallItem;

    private readonly Queue<HeroCallQueueData> redQueue = new Queue<HeroCallQueueData>();
    private readonly Queue<HeroCallQueueData> blueQueue = new Queue<HeroCallQueueData>();
    private Coroutine redDisplayCoroutine;
    private Coroutine blueDisplayCoroutine;

    /// <summary>英雄召唤提示队列中的一条数据。</summary>
    private sealed class HeroCallQueueData
    {
        public readonly int HeroCfgId;
        public readonly BattlePlayer Player;

        /// <summary>创建一条英雄召唤提示数据。</summary>
        public HeroCallQueueData(int heroCfgId, BattlePlayer player)
        {
            HeroCfgId = heroCfgId;
            Player = player;
        }
    }

    /// <summary>销毁面板时停止展示协程。</summary>
    private void OnDestroy()
    {
        StopDisplayCoroutine(ref redDisplayCoroutine);
        StopDisplayCoroutine(ref blueDisplayCoroutine);
    }

    /// <summary>将一次英雄召唤加入对应阵营的 FIFO 队列。</summary>
    public void EnqueueHero(ECampType campType, int heroCfgId, BattlePlayer player)
    {
        HeroCallQueueData queueData = new HeroCallQueueData(heroCfgId, player);
        if (campType == ECampType.Red)
        {
            redQueue.Enqueue(queueData);
            if (redDisplayCoroutine == null)
            {
                redDisplayCoroutine = StartCoroutine(DisplayQueue(ECampType.Red));
            }
            return;
        }

        blueQueue.Enqueue(queueData);
        if (blueDisplayCoroutine == null)
        {
            blueDisplayCoroutine = StartCoroutine(DisplayQueue(ECampType.Blue));
        }
    }

    /// <summary>按阵营依次展示英雄召唤提示。</summary>
    private IEnumerator DisplayQueue(ECampType campType)
    {
        Queue<HeroCallQueueData> queue = campType == ECampType.Red ? redQueue : blueQueue;
        BattleHeroCallInfoItem displayItem = campType == ECampType.Red ? redHeroCallItem : blueHeroCallItem;

        while (queue.Count > 0)
        {
            HeroCallQueueData queueData = queue.Dequeue();
            if (displayItem == null)
            {
                continue;
            }

            ShowHeroCallItem(displayItem, queueData);
            yield return new WaitForSecondsRealtime(HeroDisplayDuration);
            HideHeroCallItem(displayItem);
        }

        if (campType == ECampType.Red)
        {
            redDisplayCoroutine = null;
        }
        else
        {
            blueDisplayCoroutine = null;
        }
    }

    /// <summary>刷新并重新打开英雄提示组件，使挂载的 UI 动效重新播放。</summary>
    private void ShowHeroCallItem(BattleHeroCallInfoItem displayItem, HeroCallQueueData queueData)
    {
        SoliderCfg heroCfg = SoliderCfgConfig.Ins.SearchById(queueData.HeroCfgId);
        displayItem.gameObject.SetActive(true);
        displayItem.ClearData();
        displayItem.SetData(queueData.HeroCfgId, heroCfg, queueData.Player);
   
        displayItem.transform.SetAsLastSibling();
        displayItem.RestartDisplay();
    }

    /// <summary>关闭英雄提示组件并清理上一次显示的数据。</summary>
    private void HideHeroCallItem(BattleHeroCallInfoItem displayItem)
    {
        displayItem.gameObject.SetActive(false);
        displayItem.ClearData();
        displayItem.gameObject.SetActive(false);
    }

    /// <summary>停止指定展示协程。</summary>
    private void StopDisplayCoroutine(ref Coroutine coroutine)
    {
        if (coroutine == null)
        {
            return;
        }

        StopCoroutine(coroutine);
        coroutine = null;
    }
}
