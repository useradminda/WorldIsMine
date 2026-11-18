using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 红蓝双方英雄技能释放提示面板。
/// 每个阵营独立维护 FIFO 队列，由面板统一控制展示时长、动效重播和节点渲染顺序。
/// </summary>
public class BattleHeroOperateSkillPanel : MonoBehaviour
{
    /// <summary>单条英雄技能提示的展示时长，单位为秒。</summary>
    private const float SkillDisplayDuration = 1.5f;

    [SerializeField] private UseSkillItem redSkillItem;
    [SerializeField] private UseSkillItem blueSkillItem;

    private readonly Queue<HeroSkillQueueData> redQueue = new Queue<HeroSkillQueueData>();
    private readonly Queue<HeroSkillQueueData> blueQueue = new Queue<HeroSkillQueueData>();
    private Coroutine redDisplayCoroutine;
    private Coroutine blueDisplayCoroutine;

    /// <summary>英雄技能释放队列中的一条数据。</summary>
    private sealed class HeroSkillQueueData
    {
        public readonly int HeroCfgId;
        public readonly BattlePlayer Player;
        public readonly string SkillName;
        public readonly Sprite HeroIcon;

        /// <summary>创建一条英雄技能提示数据。</summary>
        public HeroSkillQueueData(int heroCfgId, BattlePlayer player, string skillName, Sprite heroIcon)
        {
            HeroCfgId = heroCfgId;
            Player = player;
            SkillName = skillName ?? string.Empty;
            HeroIcon = heroIcon;
        }
    }

    /// <summary>将一次英雄技能释放加入对应阵营的 FIFO 队列。</summary>
    public void EnqueueSkill(ECampType campType, int heroCfgId, BattlePlayer player, string skillName, Sprite heroIcon = null)
    {
        HeroSkillQueueData queueData = new HeroSkillQueueData(heroCfgId, player, skillName, heroIcon);
        if (campType == ECampType.Red)
        {
            redQueue.Enqueue(queueData);
            if (redDisplayCoroutine == null) redDisplayCoroutine = StartCoroutine(DisplayQueue(ECampType.Red));
            return;
        }

        blueQueue.Enqueue(queueData);
        if (blueDisplayCoroutine == null) blueDisplayCoroutine = StartCoroutine(DisplayQueue(ECampType.Blue));
    }

    /// <summary>按阵营依次展示英雄技能提示。</summary>
    private IEnumerator DisplayQueue(ECampType campType)
    {
        Queue<HeroSkillQueueData> queue = campType == ECampType.Red ? redQueue : blueQueue;
        UseSkillItem displayItem = campType == ECampType.Red ? redSkillItem : blueSkillItem;

        while (queue.Count > 0)
        {
            HeroSkillQueueData queueData = queue.Dequeue();
            if (displayItem == null) continue;

            ShowSkillItem(displayItem, queueData);
            yield return new WaitForSeconds(SkillDisplayDuration);
            HideSkillItem(displayItem);
        }

        if (campType == ECampType.Red) redDisplayCoroutine = null;
        else blueDisplayCoroutine = null;
    }

    /// <summary>刷新技能提示内容并重新打开组件，使挂载的 Animator 动效重新播放。</summary>
    private void ShowSkillItem(UseSkillItem displayItem, HeroSkillQueueData queueData)
    {
        SoliderCfg heroCfg = SoliderCfgConfig.Ins.SearchById(queueData.HeroCfgId);
        string heroName = heroCfg != null ? heroCfg.name : queueData.HeroCfgId.ToString();
        string playerName = queueData.Player != null ? queueData.Player.Name : string.Empty;
        displayItem.ClearData();
        displayItem.SetData(heroCfg, heroName, playerName, queueData.SkillName);
        displayItem.transform.SetAsLastSibling();
        displayItem.gameObject.SetActive(false);
        displayItem.gameObject.SetActive(true);
    }

    /// <summary>关闭技能提示组件并清理显示数据。</summary>
    private void HideSkillItem(UseSkillItem displayItem)
    {
        displayItem.ClearData();
        displayItem.gameObject.SetActive(false);
    }

    /// <summary>销毁面板时停止未完成的红蓝展示协程。</summary>
    private void OnDestroy()
    {
        if (redDisplayCoroutine != null) StopCoroutine(redDisplayCoroutine);
        if (blueDisplayCoroutine != null) StopCoroutine(blueDisplayCoroutine);
        redDisplayCoroutine = null;
        blueDisplayCoroutine = null;
    }
}
