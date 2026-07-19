using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZEditorWindow
{
    public class SkillEditorWindow : EditorWindow
    {
        // 工具栏范围
        public Rect TopToolBarRect => new Rect(0, 0, position.width, SkillWindowAreaStatic.TopToolBarAreaHeight);

        // 窗口主体范围(去除工具高度)
        public Rect MainBodyArea => new Rect(0, SkillWindowAreaStatic.TopToolBarAreaHeight, position.width, position.height - SkillWindowAreaStatic.TopToolBarAreaHeight);

        // 左侧Track范围
        public Rect WholeTrackAreaRect => new Rect(0, MainBodyArea.y, SkillWindowAreaStatic.TrackAreaWidth, MainBodyArea.height);

        // 右侧轨道track范围
        public Rect TrackContentAreaRect => new Rect(WholeTrackAreaRect.x, WholeTrackAreaRect.y + SkillWindowAreaStatic.TrackTitleAreaHight, WholeTrackAreaRect.width, WholeTrackAreaRect.height - SkillWindowAreaStatic.TrackTitleAreaHight);

        // 主时间范围
        public Rect WholeTimeAreaRect => new Rect(SkillWindowAreaStatic.TrackAreaWidth, MainBodyArea.y, MainBodyArea.width - SkillWindowAreaStatic.TrackAreaWidth, MainBodyArea.height);

        // 时间刻度线范围
        public Rect TimeMarkRect => new Rect(WholeTimeAreaRect.x, WholeTimeAreaRect.y, WholeTimeAreaRect.width, SkillWindowAreaStatic.TimeMarkAreaHeight);

        // 时间区域范围
        public Rect TimeAreaRect => new Rect(WholeTimeAreaRect.x, WholeTimeAreaRect.y + SkillWindowAreaStatic.TimeMarkAreaHeight, WholeTimeAreaRect.width, WholeTimeAreaRect.height);

        // 工具栏区域
        private SkillEditorToolBar skillEditorToolBar = new SkillEditorToolBar(null);
        public SkillEditorToolBar ToolBar => skillEditorToolBar;

        // 绘制时间区域
        private SkillEditorTimeArea skillEditorTimeArea = new SkillEditorTimeArea(null);
        public SkillEditorTimeArea TimeArea => skillEditorTimeArea;

        // 绘制track区域
        private SkillEditorTrackArea skillEditorTrackArea = new SkillEditorTrackArea(null);
        public SkillEditorTrackArea TrackArea => skillEditorTrackArea;

        public static SkillEditorWindow EditorWindow
        {
            get
            {
                if (editorWindow == null)
                {
                    OpenSkillEditorWindow();
                }
                return editorWindow;
            }
        }
        private static SkillEditorWindow editorWindow;
        /// <summary>
        /// 打开技能编辑器
        /// </summary>
        [MenuItem("Window/SkillEditor", false, 2002)]
        public static void OpenSkillEditorWindow()
        {
            editorWindow = GetWindow<SkillEditorWindow>("技能编辑");
            editorWindow.minSize = SkillWindowAreaStatic.SkillWindowMinSize;
            editorWindow.Show();      
        }

        public void OnGUI()
        {
            
            ToolBar.Draw(TopToolBarRect);
            TimeArea.Draw(WholeTimeAreaRect, TimeMarkRect, TimeAreaRect);
            TrackArea.Draw(WholeTrackAreaRect, TrackContentAreaRect);
        }
    }
}
