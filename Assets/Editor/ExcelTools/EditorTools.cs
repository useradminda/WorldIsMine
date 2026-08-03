using UnityEditor;

public class EditorTools
{
    [MenuItem("导出/重新导出所有表格")]
    public static void Ex()
    {
        ExportExcel.ExportExcelData("", null);
    }

    [MenuItem("导出/导出服务器配置")]
    public static void ExportServerConfig()
    {
        ServerConfigExporter.ExportAll();
        EditorUtility.RevealInFinder(ServerConfigExporter.OutputRootPath);
    }
}
