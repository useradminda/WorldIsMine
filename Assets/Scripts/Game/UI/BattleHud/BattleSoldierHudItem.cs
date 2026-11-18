using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>一组士兵共用的头顶 HUD，显示该组士兵总血量和归属玩家名称。</summary>
public class BattleSoldierHudItem : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool faceCamera = true;

    private readonly List<UnitLogicBase> soldierLogics = new List<UnitLogicBase>();
    private float refreshInterval;
    private float refreshTimer;

    /// <summary>绑定一组士兵并初始化总血量显示。</summary>
    public void Bind(IList<UnitLogicBase> logics, BattlePlayer player, float positionRefreshInterval)
    {
        soldierLogics.Clear();
        for (int i = 0; i < logics.Count; i++)
        {
            if (logics[i] != null) soldierLogics.Add(logics[i]);
        }

        refreshInterval = Mathf.Max(0f, positionRefreshInterval);
        refreshTimer = 0f;
        if (playerNameText != null) playerNameText.text = player != null ? player.Name : string.Empty;
        RefreshHp();
        RefreshPosition();
        gameObject.SetActive(true);
    }

    /// <summary>根据组内存活士兵重新计算总血量。</summary>
    public void RefreshHp()
    {
        int currentHp = 0;
        int maxHp = 0;
        for (int i = soldierLogics.Count - 1; i >= 0; i--)
        {
            UnitLogicBase logic = soldierLogics[i];
            if (logic == null || logic.IsDead)
            {
                soldierLogics.RemoveAt(i);
                continue;
            }

            currentHp += Mathf.Max(0, logic.Prop.Hp);
            maxHp += Mathf.Max(0, logic.Prop.MaxHp);
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = Mathf.Max(1, maxHp);
            hpSlider.value = Mathf.Clamp(currentHp, 0, Mathf.Max(1, maxHp));
        }
    }

    /// <summary>从当前士兵组中移除指定士兵并刷新总血量。</summary>
    public void RemoveSoldier(UnitLogicBase soldierLogic)
    {
        soldierLogics.Remove(soldierLogic);
        RefreshHp();
    }

    /// <summary>按配置频率刷新士兵组 HUD 的平均位置。</summary>
    public void RefreshPosition(float deltaTime)
    {
        if (!gameObject.activeSelf) return;
        refreshTimer -= deltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = refreshInterval;
        RefreshPosition();
    }

    /// <summary>将 HUD 放置到所有存活士兵模型位置的平均点。</summary>
    private void RefreshPosition()
    {
        Vector3 position = Vector3.zero;
        int count = 0;
        for (int i = soldierLogics.Count - 1; i >= 0; i--)
        {
            UnitLogicBase logic = soldierLogics[i];
            if (logic == null || logic.IsDead || logic.UnitView == null)
            {
                continue;
            }

            position += logic.UnitView.transform.position;
            count++;
        }

        if (count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = position / count + worldOffset;
        Camera targetCamera = Camera.main;
        if (faceCamera && targetCamera != null) transform.forward = targetCamera.transform.forward;
    }

    /// <summary>清理士兵组绑定并隐藏 HUD。</summary>
    public void Unbind()
    {
        soldierLogics.Clear();
        gameObject.SetActive(false);
    }
}
