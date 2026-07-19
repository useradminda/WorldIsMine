using UnityEngine;
using static SkillEditorDefine;
using System.Collections.Generic;
/// <summary>
/// TrackItem
/// </summary>
public class TrackItem : IRectBase
{
    // 轨道类型
    private ETrackType trackType;
    public ETrackType TrackType => trackType;

    // 打开状态
    public bool FoldoutOpen = true;

    // 子轨道
    private List<TrackSubItem> trackSubItemList = new List<TrackSubItem>();
    public List<TrackSubItem> TrackSubItemList => trackSubItemList;

    // Track颜色
    public Color Color
    {
        get
        {
            Color color = Color.white;
            if (TrackType == ETrackType.AnimationProcess)
            {
                color = Color.green;
            }
            if (TrackType == ETrackType.EffectProcess)
            {
                color = Color.red;
            }
            color.a = 0.2f;
            return color;
        }
    }

    // Track名称
    public string TrackName
    {
        get
        {
            string name = "未命名";
            if (TrackType == ETrackType.AnimationProcess)
            {
                name = "动作";
            }
            if (TrackType == ETrackType.EffectProcess)
            {
                name = "特效";
            }
            return name;
        }
    }

    public TrackItem(ETrackType trackType)
    {
        this.trackType = trackType;
    }

    // 添加子轨道
    public void AddTrackSubItem(TrackSubItem trackSubItem)
    {
        trackSubItemList.Add(trackSubItem);
    }

    // 删除子轨道
    public void DelTrackSubItem(int index)
    {
        trackSubItemList.RemoveAt(index);
    }

    // 删除子轨道
    public void DelTrackSubItem(TrackSubItem trackSubIte)
    {
        trackSubItemList.Remove(trackSubIte);
    }
}
