using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorTools
{
    [MenuItem("导出/重新导出所有表格")]
    public static void Ex()
    {
        ExportExcel.ExportExcelData("", null);
    }
}
