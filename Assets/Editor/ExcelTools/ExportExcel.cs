using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Linq;
public class ExportExcel
{
    public static string ImportExcelPath = Application.dataPath + "/../Excels/";
    public static string ExportJsonPath = Application.streamingAssetsPath + "/JsonData/";
    public static string ExportCSPath = Application.dataPath + "/Scripts/ExcelExportCs/";
    public static string CSTempleteFilePath = Application.dataPath + "/Editor/ExcelTools/ConfigTemplete.txt";

    // 导出excel
    public static void ExportExcelData(string excelName, ExcelLocalSO so)
    {    
        initDirectory();
        if (excelName == "" || excelName.Contains("$"))
            return;
        FileInfo fileInfo = new FileInfo(ImportExcelPath + excelName);
        try
        {
            ExcelPackage package = new ExcelPackage(fileInfo);
            {
                ExcelWorksheet sheet = null;
                for (int i = 1; i <= package.Workbook.Worksheets.Count; i++)
                {
                    sheet = package.Workbook.Worksheets[i];
                    ExportSheet(sheet);
                    addToSO(excelName, sheet.Name, so);
                }
                AssetDatabase.Refresh();
            }
        }
        catch (Exception ex)
        {
        }
    }

    // 删除表格对应的数据
    public static void DelExcelData(string excelName, ExcelLocalSO so)
    {
        if (so != null)
        {
            SoInfo soinfo = so.SearchSoInfo(excelName);
            if (soinfo == null)
                return;
            List<string> sheetNameList = soinfo.SheetNameList;
            for (int i = 0; i < sheetNameList.Count; i++)
            {
                string sheetName = sheetNameList[i];
                string csFilePath = ExportCSPath + sheetName + ".cs";
                if (File.Exists(csFilePath))
                    File.Delete(csFilePath);

                string jsonFilePath = ExportJsonPath + sheetName + ".json";
                if (File.Exists(jsonFilePath))
                    File.Delete(jsonFilePath);
            }
            so.DelSoInfo(excelName);
        }
    }


    // 初始化文件夹
    private static void initDirectory()
    {
        if (!Directory.Exists(ExportJsonPath))
            Directory.CreateDirectory(ExportJsonPath);
        if (!Directory.Exists(ExportCSPath))
            Directory.CreateDirectory(ExportCSPath);
    }

    // 添加值配置文件
    private static void addToSO(string excelName, string sheetName, ExcelLocalSO so)
    {
        if (so != null)
        {
            so.AddSoInfo(excelName, sheetName);
        }
    }
  
    private static void ExportSheet(ExcelWorksheet sheet)
    {
        List<object> propNameList = new List<object>();
        List<object> propTypeList = new List<object>();
        Dictionary<int, List<object>> valueDic = new Dictionary<int, List<object>>();
        int rowCount = sheet.Dimension.Rows;
        int colCount = sheet.Dimension.Columns;
        // propName, 处理对象名称, 第一行
        for(int i = 1; i <= colCount; i++)
        {
            object propName = sheet.Cells[1, i].Value;           
            if (propName == null)
            {
                Debug.LogError(string.Format("第{0}行第{1}列为空", 1, i));
                return;
            }
            propNameList.Add(propName);
        }

        // propType，处理对象的类型, 第二行
        for(int i = 1; i <= colCount; i++)
        {
            object propType = sheet.Cells[2, i].Value;
            if (propType == null)
            {
                Debug.LogError(string.Format("第{0}行第{1}列为空", 2, i));
                return;
            }
            propTypeList.Add(propType);
        }

        /* 第三行不处理， 注释用*/

        // propValue，处理对象的数值，第四行
        for(int i = 4; i <= rowCount; i++)
        {
            List<object> valueList = new List<object>();
            for (int j = 1; j <= colCount; j++)
            {
                object propValue = sheet.Cells[i, j].Value;
                if (propValue == null)
                {
                    Debug.LogError(string.Format("第{0}行第{1}列为空", i, j));
                    return;
                }
                valueList.Add(propValue);             
            }
            valueDic.Add(valueDic.Count, valueList);
        }
        exportCs(sheet.Name, propNameList, propTypeList, valueDic);
        exportJson(sheet.Name, propNameList, propTypeList, valueDic);
    }

    // 导出CS
    private static void exportCs(string csName, List<object> propNameList, List<object> propTypeList, Dictionary<int, List<object>> valueDic)
    {
        string propFieldContent = "";
        string csContent = File.ReadAllText(CSTempleteFilePath);
        for(int i = 0; i < propNameList.Count; i++)
        {        
            string propName = propNameList[i].ToString();
            string propType = propTypeList[i].ToString();
            propFieldContent += "public " + propType + " " + propName + ";\n\t";
        }
        csContent = csContent.Replace("#ClassName#", csName);
        csContent = csContent.Replace("#propField#", propFieldContent);

        string filePath = ExportCSPath + csName + ".cs";
        if (File.Exists(filePath))
            File.Delete(filePath);
        File.WriteAllText(filePath, csContent);
    }

    // 导出json
    private static void exportJson(string jsonName, List<object> propNameList, List<object> propTypeList, Dictionary<int, List<object>> valueDic)
    {
        List<Dictionary<string, object>> jsonDataList = new List<Dictionary<string, object>>();
        for (int i = 0; i < valueDic.Count; i++)
        {
            Dictionary<string, object> jsonDataDic = new Dictionary<string, object>();
            List<object> valueList = valueDic[i];
            for(int j = 0; j < valueList.Count; j++)
            {
                string propName = propNameList[j].ToString();
                string propType = propTypeList[j].ToString();
                object propValue = valueList[j];
                string propValueStr = propValue.ToString();
                if (propType == "int")
                {
                    propValue = Convert.ToInt32(propValue);
                }
                else if(propType == "float")
                {
                    propValue = Convert.ToSingle(propValue);
                }
                else if (propType == "string")
                {
                    propValue = propValueStr;
                }
                else if (propType.Contains("string[]"))
                {               
                    List<string> propValueArray = propValueStr.Split(",").ToList();
                    propValue = propValueArray;
                }
                else if(propType.Contains("int[]"))
                {
                    List<int> propValueArray = propValueStr.Split(",").Select(s => int.Parse(s)).ToList();
                    propValue = propValueArray;
                }
                else if(propType.Contains("float[]"))
                {
                    List<float> propValueArray = propValueStr.Split(",").Select(s => Single.Parse(s)).ToList();
                    propValue = propValueArray;
                }
                jsonDataDic.Add(propName, propValue);
            }
            jsonDataList.Add(jsonDataDic);
        }

        string json = JsonConvert.SerializeObject(jsonDataList);
        string filePath = ExportJsonPath + jsonName + ".json";
        if (File.Exists(filePath))
            File.Delete(filePath);
        File.WriteAllText(filePath, json);
    }
}