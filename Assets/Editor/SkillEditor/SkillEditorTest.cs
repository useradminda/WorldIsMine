using UnityEditor;

public class AutoDrawWindow : EditorWindow
{
    public int myInt = 10;
    public string myStr = "test";

    [MenuItem("Window/自动绘制窗口")]
    public static void Show() => GetWindow<AutoDrawWindow>();

    private SerializedObject so;

    private void OnEnable() => so = new SerializedObject(this);

    private void OnGUI()
    {
        // 核心代码：自动渲染所有字段
        so.Update();
        var prop = so.GetIterator();
        if (prop.NextVisible(true))
            do { EditorGUILayout.PropertyField(prop); }
            while (prop.NextVisible(false));
        so.ApplyModifiedProperties();
    }
}