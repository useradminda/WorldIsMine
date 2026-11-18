using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个英雄召唤提示 UI 组件。
/// 该组件只负责显示英雄和召唤玩家信息，并通过物体重新开关触发挂载的召唤动效。
/// </summary>
public class BattleHeroCallInfoItem : MonoBehaviour
{
    [SerializeField] private Image heroIcon;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField, Range(0f, 0.95f)] private float dissolveProgress;

    private Material runtimeHeroMaterial;
    private static readonly int DissolveProgressId = Shader.PropertyToID("_DissolveProgress");

    /// <summary>当前英雄图片的火焰溶解进度，可直接在 Unity Animation 中打关键帧。</summary>
    public float DissolveProgress
    {
        get => dissolveProgress;
        set
        {
            dissolveProgress = Mathf.Clamp(value, 0f, 0.95f);
            ApplyDissolveProgress();
        }
    }

    /// <summary>初始化英雄图片材质实例，避免动画修改共享材质资产。</summary>
    private void Awake()
    {
        if (heroIcon != null && heroIcon.material != null)
        {
            runtimeHeroMaterial = new Material(heroIcon.material);
            heroIcon.material = runtimeHeroMaterial;
        }

        ApplyDissolveProgress();
    }

    /// <summary>动画系统写入序列化字段后同步 Shader 参数。</summary>
    private void OnDidApplyAnimationProperties()
    {
        ApplyDissolveProgress();
    }

    /// <summary>销毁运行时材质实例。</summary>
    private void OnDestroy()
    {
        if (runtimeHeroMaterial != null)
        {
            Destroy(runtimeHeroMaterial);
            runtimeHeroMaterial = null;
        }
    }

    /// <summary>设置英雄召唤提示内容。</summary>
    public void SetData(int heroCfgId, SoliderCfg heroCfg, BattlePlayer player)
    {
        if (heroNameText != null)
        {
            heroNameText.text = heroCfg != null ? heroCfg.name : heroCfgId.ToString();
        }

        if (playerNameText != null)
        {
            playerNameText.text = player != null ? player.Name : string.Empty;
        }

        if (heroIcon != null && heroCfg != null && !string.IsNullOrWhiteSpace(heroCfg.bigImg))
        {
            heroIcon.sprite = Resources.Load<Sprite>(NormalizeResourcePath(heroCfg.bigImg));
        }
    }

    /// <summary>规范化英雄大图的 Resources 路径并移除图片扩展名。</summary>
    private string NormalizeResourcePath(string resourcePath)
    {
        string path = resourcePath.Replace('\\', '/').Trim();
        if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(0, path.Length - 4);
        }

        return path;
    }

    /// <summary>清理上一次显示的英雄召唤内容。</summary>
    public void ClearData()
    {
        if (heroNameText != null) heroNameText.text = string.Empty;
        if (playerNameText != null) playerNameText.text = string.Empty;
        if (heroIcon != null) heroIcon.sprite = null;
    }

    /// <summary>重新打开组件以重新播放挂载在组件上的 Animator 动效。</summary>
    public void RestartDisplay()
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    /// <summary>将当前溶解进度写入英雄图片材质。</summary>
    private void ApplyDissolveProgress()
    {
        if (runtimeHeroMaterial != null && runtimeHeroMaterial.HasProperty(DissolveProgressId))
        {
            runtimeHeroMaterial.SetFloat(DissolveProgressId, Mathf.Clamp(dissolveProgress, 0f, 0.95f));
        }
    }
}
