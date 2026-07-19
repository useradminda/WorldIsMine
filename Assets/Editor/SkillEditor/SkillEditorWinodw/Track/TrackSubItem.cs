using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ZEditorWindow;
public class TrackSubItem : IRectBase
{
    // 归属的TrackItem
    private TrackItem trackItemParent;  
    public TrackItem TrackItemParent => trackItemParent;
    
    private List<EventClipBase> eventList = new List<EventClipBase>();
    // 单位事件
    public List<EventClipBase> EventList => eventList;

    // track子类型
    public TrackSubItem(TrackItem trackItemParent)
    {
        this.trackItemParent = trackItemParent;
    }

    // 增加EventClip
    public void AddEventClip(int startFrame, int endFrame)
    {
        EventClipBase eventClip = SkillEditorItemFactory.CreateEventClip(startFrame, endFrame, this);
        AddEventClip(eventClip);
    }

    // 增加事件EventClip
    public void AddEventClip(EventClipBase eventClip)
    {
        eventList.Add(eventClip);
        SortByWidth();
    }

    public void RemoveEventClip(EventClipBase eventClip)
    {
        eventList.Remove(eventClip);
        SortByWidth();
    }

    public void SortByWidth()
    {
        EventList.Sort(delegate (EventClipBase a, EventClipBase b)
        {
            if (a.MyRect.width > b.MyRect.width)
                return -1;
            if (a.MyRect.width < b.MyRect.width) 
                return 1;
            return 0;
        });
    }

    public int GetLimtiStartTime(int startFrame, int endFrame)
    {
        for (int i = 0; i < eventList.Count; i++)
        {
            EventClipBase clip = eventList[i];
            if ((startFrame >= clip.StartFrame && startFrame <= clip.EndFrame) || (startFrame <= clip.StartFrame && endFrame >= clip.EndFrame))
            {
                startFrame = clip.EndFrame;
                return startFrame;
            }
        }
        return startFrame;
    }

    public int GetLimitEndTime(int startFrame, int endFrame)
    {
        for (int i = 0; i < eventList.Count; i++)
        {
            EventClipBase clip = eventList[i];
            if ((endFrame >= clip.StartFrame && endFrame <= clip.EndFrame) || (startFrame <= clip.StartFrame && endFrame >= clip.EndFrame))
            {
                endFrame = clip.StartFrame;
                return endFrame;
            }
        }
        return endFrame;
    }
}
