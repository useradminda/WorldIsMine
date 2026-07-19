using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using System.IO;
using OfficeOpenXml.DataValidation;

// 缓存已经导入的excel表信息
[System.Serializable]
public class ExcelLocalSO : ScriptableObject
{
    public List<SoInfo> ExcelCatchInfoList = new List<SoInfo>();

    public void AddSoInfo(string excelName, string sheetName)
    {
        SoInfo soInfo = null;
        for (int i = 0; i < ExcelCatchInfoList.Count; i++)
        {
            if (excelName == ExcelCatchInfoList[i].ExcelName)
            {
                soInfo = ExcelCatchInfoList[i];
                break;
            }
        }
        if (soInfo == null)
        {
            soInfo = new SoInfo() { ExcelName = excelName };
            ExcelCatchInfoList.Add(soInfo);
        }
        if (!soInfo.SheetNameList.Contains(sheetName))
            soInfo.SheetNameList.Add(sheetName);
    }

    public void DelSoInfo(string excelName)
    {
        SoInfo soInfo = null;
        for (int i = 0; i < ExcelCatchInfoList.Count; i++)
        {
            if (excelName == ExcelCatchInfoList[i].ExcelName)
            {
                soInfo = ExcelCatchInfoList[i];
                break;
            }
        }
        if (soInfo != null)
            ExcelCatchInfoList.Remove(soInfo);
    }

    public SoInfo SearchSoInfo(string excelName)
    {
        SoInfo soInfo = null;
        for (int i = 0; i < ExcelCatchInfoList.Count; i++)
        {
            if (excelName == ExcelCatchInfoList[i].ExcelName)
            {
                soInfo = ExcelCatchInfoList[i];
                break;
            }
        }
        return soInfo;

    }
}

[System.Serializable]
public class SoInfo
{
    public string ExcelName;
    public List<string> SheetNameList = new List<string>();
}


public class ExcelLocalSoTool
{
    [MenuItem("导出/创建Excel配置SO")]
    public static void CreateExcelSo()
    {
        if (!File.Exists("Assets/Editor/ExcelTools/ExcelSetting.asset"))
        {
            ExcelLocalSO so = ScriptableObject.CreateInstance<ExcelLocalSO>();
            AssetDatabase.CreateAsset(so, "Assets/Editor/ExcelTools/ExcelSetting.asset");// Application.dataPath + "/Editor/ExcelTools/ExcelSetting.asset");
                                                                                         //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
        }
    }
}
