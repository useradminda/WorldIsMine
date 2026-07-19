using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using ZEditorWindow;
// 绘制左侧Track
public class SkillEditorTrackArea : SkillEditorBase
{
    // 滚动条滑动信息
    private float timeAreaVScrollBarValue = 0f;
    // 滑块的大小
    private float sliderPointScale = 1;

    // track列表
    private List<TrackItem> trackList = new List<TrackItem>();
    public List<TrackItem> TrackList
    {
        get
        {
            if (trackList == null || trackList.Count == 0)
            {
                int i = 0;
                foreach (var value in Enum.GetValues(typeof(SkillEditorDefine.ETrackType)))
                {
                    SkillEditorDefine.ETrackType trackType = (SkillEditorDefine.ETrackType)value;
                    TrackItem trackItem = SkillEditorItemFactory.CreateTrackItem(trackType);
                    ++i;
                    trackList.Add(trackItem);
                }
            }
            return trackList; 
        }
    }

    public SkillEditorTrackArea(SkillEditorWindow window) : base(window) { }

    // 绘制左侧Track
    public void Draw(Rect wholeTrackAreaRect, Rect trackContentRect)
    {
        drawTrackRect(trackContentRect);
    }

    // 绘制左侧track区域信息
    private void drawTrackRect(Rect trackContentRect)
    {
        GUI.Box(trackContentRect, string.Empty);
        // 重新绘制以左上角坐标为x = 0, y = 0的范围
        GUILayout.BeginArea(trackContentRect);
        float trackRectPosY = 0;
        int subCount = 0;
        for (int i = 0; i < TrackList.Count; ++i)
        { 
            TrackItem trackItem = TrackList[i];
            trackRectPosY = i * SkillWindowAreaStatic.TrackHeigh + subCount * SkillWindowAreaStatic.SubTrackHeigh;
            Rect trackRect = new Rect(SkillWindowAreaStatic.TrackAreaWidth - SkillWindowAreaStatic.TrackWidth, trackRectPosY, SkillWindowAreaStatic.TrackWidth, SkillWindowAreaStatic.TrackHeigh);
            if (trackItem.FoldoutOpen == true)
            {
                subCount = subCount + trackItem.TrackSubItemList.Count;
            }
            GUI.Box(trackRect, string.Empty, "DD Background");

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 15;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(trackRect, trackItem.TrackName + "(" + trackItem.TrackSubItemList.Count + ")", style);

            // 折叠
            Rect foldoutRect = new Rect(trackRect.x, trackRect.y, trackRect.width - 125, trackRect.height);
            trackItem.FoldoutOpen = EditorGUI.Foldout(foldoutRect, trackItem.FoldoutOpen, "  ", true);

            // 添加子按钮
            Rect buttonRect = new Rect(trackRect.x + trackRect.width - 30, trackRect.y, 30, trackRect.height - 1);
            if (GUI.Button(buttonRect, "+", "ButtonLeft"))
            {
                TrackSubItem trackSubItem = SkillEditorItemFactory.CreateTrackSubItem(trackItem);
                trackItem.AddTrackSubItem(trackSubItem);
            }

            drawSubTrackRect(trackRect, trackItem);
        } 
        GUILayout.EndArea();
    }

    // 绘制左侧子轨道区域
    private void drawSubTrackRect(Rect trackRect, TrackItem trackItem)
    {
        if (trackItem.FoldoutOpen)
        {
            for (int i = 0; i < trackItem.TrackSubItemList.Count; i++)
            {
                TrackSubItem subItem = trackItem.TrackSubItemList[i];

                Rect subTrackRect = new Rect(trackRect.x + 25, trackRect.height + trackRect.y + i * SkillWindowAreaStatic.SubTrackHeigh, SkillWindowAreaStatic.SubTrackWidth, SkillWindowAreaStatic.SubTrackHeigh);
                GUI.Box(subTrackRect, string.Empty, "RL Background");
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;
                GUI.Label(subTrackRect, "子" + trackItem.TrackName, style);

                // 删除子按钮
                Rect buttonRect = new Rect(subTrackRect.x + subTrackRect.width - 25, subTrackRect.y, 20, subTrackRect.height - 1);
                if (GUI.Button(buttonRect, "-", "ButtonLeft"))
                {
                    trackItem.DelTrackSubItem(subItem);
                }
            }
        }
    }

}


