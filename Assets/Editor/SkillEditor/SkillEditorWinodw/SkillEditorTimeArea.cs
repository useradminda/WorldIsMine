using UnityEngine;
using UnityEditor;
using ZEditorWindow;
using UnityEngine.UIElements;
/// <summary>
/// 时间区域
/// </summary>
public enum ELeftClikDragType
{ 
    None = 0,
    Whole,
    Left,
    Right,
}
public class SkillEditorTimeArea : SkillEditorBase
{
    // 滚动条滑动信息
    private float timeAreaHScrollBarValue = 0f;
    // 滑块的大小
    private float sliderPointScale = 1;

    // 时间刻度hashId
    private int timeMarkRectHashId = "timeMarkRect".GetHashCode();

    // 时间区域hashId
    private int timeAreaRectHashId = "timeAreaRect".GetHashCode();

    // 当前时间刻度线位置
    private float currentTimeMarkPosX = 0f;
    // 红色mark标记位置
    public float CurrentTimeMarkPosX
    {
        get
        {
            return currentTimeMarkPosX;
        }
        set
        {
            currentTimeMarkPosX = value;
            if (currentTimeMarkPosX < 0f)
                currentTimeMarkPosX = 0;
        }
    }

    // 选中的动画事件片段
    private EventClipBase selectEventClip;
    public EventClipBase SelectEventClip
    {
        set
        {
            selectEventClip = value;
        }
        get { return selectEventClip; }
    }

    private ELeftClikDragType leftClikDragType = ELeftClikDragType.None;

    public SkillEditorTimeArea(SkillEditorWindow window) : base(window) { }

    // 绘制时间区域
    public void Draw(Rect wholeTimeAreaRect, Rect timeMarkRect, Rect timeAreaRect)
    {
        // 绘制时间刻度线
        drawTimeMark(timeMarkRect);    
        listenTimeMarkEvent(timeMarkRect);

        listenTimeAreaEvent(timeAreaRect);
        drawTimeAreaLineAndBG(timeAreaRect);
        drawTimeAreaTrackClip(timeAreaRect);

        drawEventClipDragRect(timeAreaRect);

        // 最后绘制刻度红线
        drawTimeMarkPointerLine(wholeTimeAreaRect, CurrentTimeMarkPosX - timeAreaHScrollBarValue);

        if (SkillWindowAreaStatic.TestFrame * SkillWindowAreaStatic.PixelPerFrame > (timeAreaRect.width))
        {
            timeAreaHScrollBarValue = GUI.HorizontalScrollbar(new Rect(timeAreaRect.x, timeAreaRect.y + timeAreaRect.height - 40, timeAreaRect.width, 1), timeAreaHScrollBarValue, sliderPointScale, 0, SkillWindowAreaStatic.TestFrame * SkillWindowAreaStatic.PixelPerFrame - timeAreaRect.width + 5);
        }  
        else
        {
            timeAreaHScrollBarValue = 0;
        }
    }

    // 绘制刻度线
    private void drawTimeMark(Rect timeMarkRect)
    {
        // 开启一个从0,0的新区域
        GUILayout.BeginArea(timeMarkRect);
        for (int i = 0; i <= SkillWindowAreaStatic.TestFrame; i ++)
        {
            float posX = i * SkillWindowAreaStatic.PixelPerFrame - timeAreaHScrollBarValue;
            float posY = SkillWindowAreaStatic.TimeMarkAreaHeight - SkillWindowAreaStatic.TimeMarkAreaHeight * (0.1f);
            float width = 1;
            float height = SkillWindowAreaStatic.TimeMarkAreaHeight * (0.1f);
            Color color = Color.white;
            color.a = 0.5f;
            if (i % 5 == 0)
            {          
                posY = SkillWindowAreaStatic.TimeMarkAreaHeight - SkillWindowAreaStatic.TimeMarkAreaHeight * 0.45f;
                height = SkillWindowAreaStatic.TimeMarkAreaHeight * 0.45f;
                width = 1.5f;
                color.a = 0.7f;
                GUI.Label(new Rect(posX + 3, 3f, 40f, 20f), i.ToString(), new GUIStyle("AnimationTimelineTick"));
            }
            Rect lineRect = new Rect(posX, posY, width, height);
            EditorGUI.DrawRect(lineRect, color);
        }
        GUILayout.EndArea();
    }

    // 绘制时间刻度指针红线
    private void drawTimeMarkPointerLine(Rect wholeTimeAreaRect, float mouseDownPosX)
    {
        GUILayout.BeginArea(wholeTimeAreaRect);
        Color color = Color.red;
        color.a = 0.5f;
        // 绘制红头
        Rect headRect = new Rect(mouseDownPosX - 6f, -2, 12, 15);
        GUI.DrawTexture(headRect, SkillEditorResManager.TimeHeadTexture, ScaleMode.ScaleToFit, true, 0, color, 12, 15);

        // 绘制红线
        Rect lineRect = new Rect(mouseDownPosX, 0 + 10, 1, wholeTimeAreaRect.height);
        EditorGUI.DrawRect(lineRect, color);
        GUILayout.EndArea();
    }

    // 监听点击事件刻度事件
    private void listenTimeMarkEvent(Rect timeMarkRect)
    {
        // 开启一个从0,0的新区域
        GUILayout.BeginArea(timeMarkRect);     
        Rect contansRect = new Rect(0, 0, timeMarkRect.width, timeMarkRect.height);
        int controlId = GUIUtility.GetControlID(timeMarkRectHashId, FocusType.Passive, timeMarkRect);
        switch (Event.current.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                // 在这个区域内 点击事件从 0,0 位置开始
                if (contansRect.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    CurrentTimeMarkPosX = Event.current.mousePosition.x;
                    Event.current.Use();
                    Debug.Log("TimeMark mouseDown");
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;             
                    Debug.Log("TimeMark MouseUp");
                    Event.current.Use();
                }             
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    CurrentTimeMarkPosX = Event.current.mousePosition.x;
                    Debug.Log("TimeMark MouseDrag");
                    Event.current.Use();
                }
                break;
        }
        GUILayout.EndArea();
    }

    // 绘制时间区域的背景
    private void drawTimeAreaLineAndBG(Rect timeAreaRect)
    {
        GUILayout.BeginArea(timeAreaRect, new GUIStyle("CurveEditorBackground"));
        // 绘制竖线
        for (int i = 0; i <= SkillWindowAreaStatic.TestFrame; i++)
        {
            float posX = i * SkillWindowAreaStatic.PixelPerFrame - timeAreaHScrollBarValue;
            float posY = 0;
            float width = 1;
            float height = timeAreaRect.height;
            Color color = Color.white;
            color.a = 0.05f;
            if (i % 5 == 0)
            {
                width = 1.5f;
                color.a = 0.15f;
            }
            Rect lineRect = new Rect(posX, posY, width, height);
            EditorGUI.DrawRect(lineRect, color);
        }

        float hLinePosY = 0;
        // 绘制横线
        for (int i = 0; i < SkillEditorWindow.EditorWindow.TrackArea.TrackList.Count; i++)
        {
            TrackItem trackItem = SkillEditorWindow.EditorWindow.TrackArea.TrackList[i];
            float posX = 0;// timeAreaHScrollBarValue + sliderPointScale;
            float width = timeAreaRect.width;
            float height = 1;
            Rect parentRect = new Rect(posX, hLinePosY, width, SkillWindowAreaStatic.TrackHeigh);
            trackItem.SetMyRect(parentRect);

            hLinePosY += SkillWindowAreaStatic.TrackHeigh;      
            
            Color color = Color.white;
            color.a = 0.15f;

            Rect tracklineRect = new Rect(posX, hLinePosY, width, height);   
            EditorGUI.DrawRect(tracklineRect, color);
            if (trackItem.FoldoutOpen == true)
            {
                for (int j = 0; j < trackItem.TrackSubItemList.Count; j++)
                {
                    TrackSubItem trackSubItem = trackItem.TrackSubItemList[j];
                    Rect subRect = new Rect(posX, hLinePosY, width, SkillWindowAreaStatic.SubTrackHeigh);
                    trackSubItem.SetMyRect(subRect);
                    hLinePosY += SkillWindowAreaStatic.SubTrackHeigh;
                    Rect trackSublineRect = new Rect(posX, hLinePosY, width, height);
                    color.a = 0.5f;
                    EditorGUI.DrawRect(trackSublineRect, color);
                }           
            }
        }
        GUILayout.EndArea();
    }

    private Vector2 mouseClickDownPos = Vector2.zero;
    private int mouseClickDownStartFrame = 0;
    private int mouseClickDownEndFrame = 0;
    // 监听点击事件刻度事件
    private void listenTimeAreaEvent(Rect timeAreaRect)
    {
        // 开启一个从0,0的新区域
        GUILayout.BeginArea(timeAreaRect);
        Rect contansRect = new Rect(0, 0, timeAreaRect.width, timeAreaRect.height);
        int controlId = GUIUtility.GetControlID(timeAreaRectHashId, FocusType.Passive, timeAreaRect);
        EventType eventType = Event.current.GetTypeForControl(controlId);
        Debug.LogError(eventType);
        switch (eventType)
        {
            case EventType.MouseDown:
                // 在这个区域内 点击事件从 0,0 位置开始
                if (contansRect.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    mouseClickDownPos = Event.current.mousePosition;
                    if (Event.current.button == 0)
                    {
                        ELeftClikDragType clikType = ELeftClikDragType.None;
                        for (int i = 0; i < SkillEditorWindow.EditorWindow.TrackArea.TrackList.Count; i++)
                        {
                            TrackItem trackItem = SkillEditorWindow.EditorWindow.TrackArea.TrackList[i];
                            if (trackItem.FoldoutOpen)
                            {
                                for (int j = 0; j < trackItem.TrackSubItemList.Count; j++)
                                {
                                    TrackSubItem trackSubItem = trackItem.TrackSubItemList[j];
                                    if (trackSubItem.MyRect.Contains(Event.current.mousePosition))
                                    {
                                        for (int z = trackSubItem.EventList.Count - 1; z >= 0; z--)
                                        {
                                            EventClipBase clip = trackSubItem.EventList[z];
                                            if (SelectEventClip == null)
                                            {
                                                if (clip.MyRect.Contains(Event.current.mousePosition))
                                                {
                                                    clikType = ELeftClikDragType.Whole;
                                                    SelectEventClip = clip;
                                                    leftClikDragType = clikType;
                                                    mouseClickDownStartFrame = SelectEventClip.StartFrame;
                                                    mouseClickDownEndFrame = SelectEventClip.EndFrame;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (SelectEventClip.GetMyLeftDragRect().Contains(Event.current.mousePosition) || SelectEventClip.GetMyRightDragRect().Contains(Event.current.mousePosition))
                                                {
                                                    clikType = GetLeftClikType(Event.current.mousePosition, SelectEventClip);
                                                    leftClikDragType = clikType;
                                                    mouseClickDownStartFrame = SelectEventClip.StartFrame;
                                                    mouseClickDownEndFrame = SelectEventClip.EndFrame;
                                                    break;
                                                }
                                                if (clip.MyRect.Contains(Event.current.mousePosition))
                                                {
                                                    clikType = ELeftClikDragType.Whole;                                                   
                                                    SelectEventClip = clip;
                                                    leftClikDragType = clikType;
                                                    mouseClickDownStartFrame = SelectEventClip.StartFrame;
                                                    mouseClickDownEndFrame = SelectEventClip.EndFrame;
                                                    break;
                                                }
                                            }                                
                                        }
                                        if (clikType != ELeftClikDragType.None)
                                            break;
                                    }
                                }
                            }
                            if (clikType != ELeftClikDragType.None)
                                break;
                        }
                        if (clikType == ELeftClikDragType.None)
                        {
                            CurrentTimeMarkPosX = Event.current.mousePosition.x;
                            SelectEventClip = null;
                        }
                    }
                    Event.current.Use();
                    Debug.Log("TimeArea mouseDown");
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    bool rightClickEvent = false;
                    // 右键抬起
                    if (Event.current.button == 1)
                    {                      
                        for (int i = 0; i < SkillEditorWindow.EditorWindow.TrackArea.TrackList.Count; i++)
                        {
                            TrackItem trackItem = SkillEditorWindow.EditorWindow.TrackArea.TrackList[i];
                            if (trackItem.FoldoutOpen)
                            {
                                for (int j = 0; j < trackItem.TrackSubItemList.Count; j++)
                                {
                                    TrackSubItem trackSubItem = trackItem.TrackSubItemList[j];
                                    if (trackSubItem.MyRect.Contains(Event.current.mousePosition))
                                    {
                                        for (int z = 0; z < trackSubItem.EventList.Count; z++)
                                        {
                                            if (trackSubItem.EventList[z].MyRect.Contains(Event.current.mousePosition))
                                            {
                                                rightClickEvent = true;
                                                openRightClickPanel(Vector2.zero, trackSubItem, trackSubItem.EventList[z]);
                                                if (rightClickEvent)
                                                    break;
                                            }
                                        }
                                        if (rightClickEvent)
                                            break;
                                        openRightClickPanel(Event.current.mousePosition, trackSubItem, null);                                                                            
                                    }
                                }
                            }
                            if (rightClickEvent)
                                break;
                        }
                    }                
                    GUIUtility.hotControl = 0;
                    Debug.Log("TimeMark MouseUp");
                    Event.current.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    if (SelectEventClip == null)
                    {
                        CurrentTimeMarkPosX = Event.current.mousePosition.x;
                        Debug.Log("TimeArea MouseDrag");    
                    }
                    else
                    {
                        Vector2 dragPos = Event.current.mousePosition - mouseClickDownPos;
                        int dragFrame = SkillWindowAreaStatic.PixelToFrame(dragPos.x);
                        if (leftClikDragType == ELeftClikDragType.Whole)
                        {
                            SelectEventClip.DragWholeEventClip(mouseClickDownStartFrame, dragFrame);
                        }
                        else if(leftClikDragType == ELeftClikDragType.Left)
                        {
                            SelectEventClip.DragClipStartFrame(mouseClickDownStartFrame, dragFrame);
                        }
                        else if(leftClikDragType == ELeftClikDragType.Right)
                        {
                            SelectEventClip.DragClipEndFrame(mouseClickDownEndFrame, dragFrame);
                        }
                        Debug.Log("SelectEventClip Drag");
                    }
                    Event.current.Use();
                }
                break;
        }
        GUILayout.EndArea();
    }

    // 绘制Track的EventClip
    private void drawTimeAreaTrackClip(Rect timeAreaRect)
    {
        GUILayout.BeginArea(timeAreaRect);
        // 绘制EventClipRect
        for (int i = 0; i < SkillEditorWindow.EditorWindow.TrackArea.TrackList.Count; i++)
        {
            TrackItem trackItem = SkillEditorWindow.EditorWindow.TrackArea.TrackList[i];
            if (trackItem.FoldoutOpen == true)
            {
                for (int j = 0; j < trackItem.TrackSubItemList.Count; j++)
                {
                    TrackSubItem trackSubItem = trackItem.TrackSubItemList[j];
                    for (int z = 0; z < trackSubItem.EventList.Count; z++)
                    {
                        EventClipBase eventClip = trackSubItem.EventList[z];
                        Rect eventClipRect = new Rect(eventClip.MyRect.x + 2, eventClip.MyRect.y + 2, eventClip.MyRect.width - 1, eventClip.MyRect.height - 4);               
                        if (SelectEventClip == eventClip)
                        {
                            GUI.Box(eventClipRect, "", "flow node 0 on");
                        }
                        else
                        {
                            GUI.Box(eventClipRect, "", "flow node 0");
                        }
                    }
                }
            }
        }
        GUILayout.EndArea();
    }

    // 绘制左右拖拽的区域
    private void drawEventClipDragRect(Rect timeAreaRect)
    {
        GUILayout.BeginArea(timeAreaRect);
        if (SelectEventClip != null)
        {
            Rect leftRect = SelectEventClip.GetMyLeftDragRect();
            Rect leftDragRect = new Rect(leftRect.x + 2, leftRect.y + 2, leftRect.width - 1, leftRect.height - 4);
            if (leftClikDragType == ELeftClikDragType.Left)
                GUI.Box(leftDragRect, "", "flow node 1 on");
            else
                GUI.Box(leftDragRect, "", "flow node 1");
            Rect leftArrowRect = new Rect(leftRect.x - 1, leftRect.y + leftRect.width, leftRect.width, leftRect.height);
            //GUI.Box(leftArrowRect, "", "ArrowNavigationLeft");

            Rect rightRect = SelectEventClip.GetMyRightDragRect();
            Rect rightDragRect = new Rect(rightRect.x + 2, rightRect.y + 2, rightRect.width - 1, rightRect.height - 4);
            if (leftClikDragType == ELeftClikDragType.Right)
                GUI.Box(rightDragRect, "", "flow node 1 on");
            else
                GUI.Box(rightDragRect, "", "flow node 1");
            Rect rightArrowRect = new Rect(rightRect.x - 2, rightRect.y + rightRect.width, rightRect.width, rightRect.height);
            //GUI.Box(rightArrowRect, "", "ArrowNavigationRight");
        }
        GUILayout.EndArea();
    }


    // 打开右键的面板
    private void openRightClickPanel(Vector2 mousePos, TrackSubItem trackSubItem, EventClipBase EventClip)
    {
        GenericMenu menu = new GenericMenu();
        if (EventClip == null)
        { 
            menu.AddItem(new GUIContent("添加片段"), false, delegate () {
                int startFrame = SkillWindowAreaStatic.PixelToFrame(mousePos.x);
                int endFrame = startFrame + SkillWindowAreaStatic.ClipDefaultFrame;

                trackSubItem.AddEventClip(startFrame, endFrame);
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("添加片段"));
        }

        if (EventClip != null)
        {
            menu.AddItem(new GUIContent("删除片段"), false, delegate ()
            {
                if (SelectEventClip == EventClip)
                {
                    SelectEventClip = null;
                    leftClikDragType = ELeftClikDragType.None;
                }
                trackSubItem.RemoveEventClip(EventClip);
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("删除片段"));
        }
        menu.ShowAsContext();
    }

    // 判定clip点击区域
    private ELeftClikDragType GetLeftClikType(Vector2 mousePos, IRectBase rectData) 
    {
        if (rectData.GetMyRightDragRect().Contains(mousePos))
            return ELeftClikDragType.Right;
        if (rectData.GetMyLeftDragRect().Contains(mousePos))
            return ELeftClikDragType.Left;
        if (rectData.MyRect.Contains(mousePos))
            return ELeftClikDragType.Whole;
        return ELeftClikDragType.None;
    }

}
