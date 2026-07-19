using UnityEngine;

namespace ZEditorWindow
{
    // 事件Clip
    public abstract class EventClipBase : IRectBase
    {
        // 开始帧
        private int startFrame;
        public int StartFrame => startFrame;

        // 结束帧
        private int endFrame;
        public int EndFrame => endFrame;

        public int FrameLength => EndFrame - startFrame;

       

        // 属于哪一个track子轨道
        private TrackSubItem trackSubItemParent;
        public TrackSubItem TrackSubItemParent => trackSubItemParent;


        // 事件Clip
        public EventClipBase(int startFrame, int endFrame, TrackSubItem trackSubItem)
        {
            SetTrackSubItem(trackSubItem);
            SetFrame(startFrame, endFrame);         
        }

        // 设置动画片段父级
        public void SetTrackSubItem(TrackSubItem trackSubItem)
        {
            trackSubItemParent = trackSubItem;
        }

        // 拖拽整个事件片段
        public void DragWholeEventClip(int startFrameCatch, int dragFrame)
        {
            int finalStartFrame = startFrameCatch + dragFrame;
            if (finalStartFrame == 0)
                return;
            if (finalStartFrame <= 0)
            {
                finalStartFrame = 0;           
            }
            else
            {
                if(finalStartFrame + FrameLength >= SkillWindowAreaStatic.TestFrame)
                {
                    finalStartFrame = SkillWindowAreaStatic.TestFrame - FrameLength;
                }
            }
            int frameLength = FrameLength;
            startFrame = finalStartFrame;
            endFrame = this.startFrame + frameLength;
            UpdateMyRect();
        }

        // 拖拽开始位置
        public void DragClipStartFrame(int startFrameCatch, int dragFrame) 
        {
            int finalStartFrame = startFrameCatch + dragFrame;         
            if (finalStartFrame <= 0)
            {
                finalStartFrame = 0;
            }
            if (finalStartFrame > this.endFrame - 1)
            {
                finalStartFrame = this.endFrame - 1;
            }
            this.startFrame = finalStartFrame;       
            UpdateMyRect();
        }

        // 拖拽结束位置
        public void DragClipEndFrame(int endFrameCatch, int dragFrame)
        {
            int finalEndFrame = endFrameCatch + dragFrame;
            if (finalEndFrame >= SkillWindowAreaStatic.TestFrame)
            {
                finalEndFrame = SkillWindowAreaStatic.TestFrame;
            }
            if (finalEndFrame < this.startFrame + 1)
            {
                finalEndFrame = this.startFrame + 1;
            }
            this.endFrame = finalEndFrame;
            UpdateMyRect();
        }

        public void SetFrame(int startFrame, int endFrame) 
        { 
            if (endFrame <= startFrame)
            {
                endFrame = startFrame + 1;
            }
            this.startFrame = startFrame;
            this.endFrame = endFrame;
            UpdateMyRect();
       
        }

        public void UpdateMyRect()
        {
            float posX = this.startFrame * SkillWindowAreaStatic.PixelPerFrame;
            float posY = TrackSubItemParent.MyRect.y;
            float width = (EndFrame - StartFrame) * SkillWindowAreaStatic.PixelPerFrame;
            float height = SkillWindowAreaStatic.SubTrackHeigh;
            Rect rect = new Rect(posX, posY, width, height);
            SetMyRect(rect);
            TrackSubItemParent.SortByWidth();
        }
    }
}
