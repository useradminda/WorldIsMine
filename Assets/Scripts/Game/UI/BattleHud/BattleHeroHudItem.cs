using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>单个英雄头顶 HUD，显示血量、经验、星级、玩家头像、等级和玩家名称。</summary>
public class BattleHeroHudItem : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TextMeshProUGUI starText;
    [SerializeField] private RawImage playerAvatar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private bool faceCamera = true;

    private UnitLogicBase heroLogic;
    private float refreshInterval;
    private float refreshTimer;

    /// <summary>绑定英雄逻辑并设置 HUD 基础数据。</summary>
    public void Bind(UnitLogicBase logic, int level, int experience, int maxExperience, int stars, float positionRefreshInterval)
    {
        heroLogic = logic;
        refreshInterval = Mathf.Max(0f, positionRefreshInterval);
        refreshTimer = 0f;
        if (logic != null)
        {
            SetHp(logic.Prop.Hp, logic.Prop.MaxHp);
            SetPlayer(logic.PlayerInfo);
        }

        SetExperience(level, experience, maxExperience, stars);
        RefreshPosition(true);
        gameObject.SetActive(true);
    }

    /// <summary>设置英雄血条当前值和最大值。</summary>
    public void SetHp(int currentHp, int maxHp)
    {
        if (hpSlider == null) return;
        hpSlider.minValue = 0f;
        hpSlider.maxValue = Mathf.Max(1, maxHp);
        hpSlider.value = Mathf.Clamp(currentHp, 0, Mathf.Max(1, maxHp));
    }

    /// <summary>设置英雄等级、经验和星级显示。</summary>
    public void SetExperience(int level, int experience, int maxExperience, int stars)
    {
        if (levelText != null) levelText.text = level.ToString();
        if (starText != null) starText.text = stars.ToString();
        if (experienceSlider != null)
        {
            experienceSlider.minValue = 0f;
            experienceSlider.maxValue = Mathf.Max(1, maxExperience);
            experienceSlider.value = Mathf.Clamp(experience, 0, Mathf.Max(1, maxExperience));
        }
    }

    /// <summary>设置归属玩家名称和玩家头像。</summary>
    public void SetPlayer(BattlePlayer player)
    {
        if (playerNameText != null) playerNameText.text = player != null ? player.Name : string.Empty;
        if (playerAvatar != null)
        {
            playerAvatar.texture = player == null ? null : UITextureFactory.Load(player.AvatarAddress);
        }
    }

    /// <summary>同步英雄当前血量。</summary>
    public void RefreshHp()
    {
        if (heroLogic != null) SetHp(heroLogic.Prop.Hp, heroLogic.Prop.MaxHp);
    }

    /// <summary>按配置频率刷新英雄 HUD 的世界位置。</summary>
    public void RefreshPosition(float deltaTime)
    {
        if (!gameObject.activeSelf || heroLogic == null || heroLogic.UnitView == null) return;
        refreshTimer -= deltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = refreshInterval;
        RefreshPosition(true);
    }

    /// <summary>立即将 HUD 放置到英雄模型上方并朝向相机。</summary>
    private void RefreshPosition(bool immediate)
    {
        if (heroLogic == null || heroLogic.UnitView == null) return;
        transform.position = heroLogic.UnitView.transform.position + worldOffset;
        Camera targetCamera = Camera.main;
        if (faceCamera && targetCamera != null) transform.forward = targetCamera.transform.forward;
    }

    /// <summary>解除英雄绑定并隐藏 HUD。</summary>
    public void Unbind()
    {
        heroLogic = null;
        gameObject.SetActive(false);
    }
}
