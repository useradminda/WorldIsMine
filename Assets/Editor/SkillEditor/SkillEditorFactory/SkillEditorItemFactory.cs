
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using static SkillEditorDefine;

namespace ZEditorWindow
{
    public static class SkillEditorItemFactory
    {
        // 创建TrackItem
        public static TrackItem CreateTrackItem(ETrackType trackType)
        {    
            TrackItem trackItem = new TrackItem(trackType);
            return trackItem;
        }

        // 创建子TrackItem
        public static TrackSubItem CreateTrackSubItem(TrackItem parent)
        {
            TrackSubItem trackSubItem = new TrackSubItem(parent);
            return trackSubItem;
        }

        // 创建事件Clip
        public static EventClipBase CreateEventClip(int startFrame, int endFrame, TrackSubItem trackSubItem)
        {
            EventClipBase eventClip = null;
            if (trackSubItem.TrackItemParent.TrackType == ETrackType.AnimationProcess)
            {
                eventClip = new MotionEventClip(startFrame, endFrame, trackSubItem);
            }
            if (eventClip == null)
            {
                eventClip = new MotionEventClip(startFrame, endFrame, trackSubItem);
            }
            return eventClip;
        }
    }
}
