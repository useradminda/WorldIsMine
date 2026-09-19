using UnityEngine;
using UnityEngine.UI;

/// <summary>单个英雄头顶的信息组件，跟随头部 Transform。</summary>
public class UnitWorldNameItem : BasePanel
{
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text heroNameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text starText;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool faceCamera = true;

    public Transform FollowTarget { get; private set; }

    /// <summary>绑定英雄头部 Transform。</summary>
    public void Bind(Transform target)
    {
        FollowTarget = target;
    }

    /// <summary>解除英雄头部绑定。</summary>
    public void Unbind()
    {
        FollowTarget = null;
    }

    /// <summary>刷新玩家、英雄、等级和星级信息。</summary>
    public void SetData(string playerName, string heroName, int level, int stars)
    {
        SetText(playerNameText, playerName);
        SetText(heroNameText, heroName);
        SetText(levelText, level.ToString());
        SetText(starText, stars.ToString());
        Open();
    }

    private void LateUpdate()
    {
        if (FollowTarget == null)
        {
            return;
        }

        transform.position = FollowTarget.position + headOffset;
        Camera targetCamera = Camera.main;
        if (targetCamera != null && faceCamera)
        {
            transform.forward = targetCamera.transform.forward;
        }
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
