using UnityEngine;
using UnityEditor;
using ZEditorWindow;
using Unity.VisualScripting;
/// <summary>
/// 编辑器工具栏
/// </summary>
public class SkillEditorToolBar :SkillEditorBase
{
    // 当前选中的页签索引
    private int selectedTabIndex = 0;
    // 页签名称数组
    private string[] tabNames = new string[] { "基础设置", "高级设置", "帮助" };
    public SkillEditorToolBar(SkillEditorWindow window) : base(window) { }
    private GUIStyle tabStyle;
    void OnEnable()
    {
        tabStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { textColor = Color.white },
            hover = { textColor = Color.yellow },
            active = { textColor = Color.green },
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
    }
    // 绘制工具栏
    public void Draw(Rect rect)
    {
        GUILayout.BeginArea(rect);
        // 绘制页签工具栏
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabNames);

        // 根据选中的页签索引显示对应内容
        switch (selectedTabIndex)
        {
            case 0:
                
                break;
            case 1:
                
                break;
            case 2:
               
                break;
        }


        
        GUILayout.EndArea();
    }
}
