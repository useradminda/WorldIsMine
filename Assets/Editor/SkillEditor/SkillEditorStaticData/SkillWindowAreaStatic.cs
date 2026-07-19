using UnityEngine;
namespace ZEditorWindow
{
    // 技能编辑器静态数据
    public static class SkillWindowAreaStatic
    {
        // 编辑器最小尺寸
        public static Vector2 SkillWindowMinSize = new Vector2(1200, 400);

        // 技能编辑器窗口工具区域高度
        public static float TopToolBarAreaHeight = 50;

        // 左侧内容区域宽度
        public static float TrackAreaWidth = 180;

        // Track区域标题高度
        public static float TrackTitleAreaHight = 25;

        // 左侧TrackItem的宽度
        public static float TrackWidth => TrackAreaWidth - 25f;

        // 左侧subTrackItem的宽度
        public static float SubTrackWidth => TrackWidth - 20f;

        // 左侧TrackItem高度
        public static float TrackHeigh = 50f;

        // 左侧子SubTrackItem高度
        public static float SubTrackHeigh = 35;

        // 时间刻度区域高度
        public static float TimeMarkAreaHeight = 25;

        // 每帧多少像素
        public static float PixelPerFrame = 10f;

        // 测试用的帧数
        public static int TestFrame = 150;

        // 片段最小帧数
        public static int ClipDefaultFrame = 5;

        // 像素转到帧
        public static int PixelToFrame(float pixelValue)
        {
            return (int)(pixelValue / PixelPerFrame);
        }

    }
}
