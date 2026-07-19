using UnityEngine;
using ZEditorWindow;
public abstract class IRectBase
{
    private Rect myRect;

    public Rect MyRect => myRect;

    // 左边拖拽区域
    private Rect LeftDragRect;
    // 右边拖拽区域
    private Rect RightDragRect;


    public void SetMyRect(Rect rect)
    {
        myRect = rect;
        LeftDragRect = new Rect(rect.x, rect.y, SkillWindowAreaStatic.PixelPerFrame, rect.height);
        RightDragRect = new Rect(rect.x + rect.width - SkillWindowAreaStatic.PixelPerFrame, rect.y, SkillWindowAreaStatic.PixelPerFrame, rect.height);
    }

    // 获取左侧拖拽区域
    public Rect GetMyLeftDragRect()
    {
        return LeftDragRect;
    }

    public Rect GetMyRightDragRect()
    {
        return RightDragRect;
    }

   
}
