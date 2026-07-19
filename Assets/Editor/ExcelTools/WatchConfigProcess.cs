
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class WatchConfigProcess
{
    static FileSystemWatcher watcher;
    static FileSystemEventHandler fileChangedCb;
    static RenamedEventHandler fileRenameCb;

    static ExcelLocalSO so;
    static bool isInitialized = false;

    private static FileSystemEventArgs curChangeEventArgs;

    [InitializeOnLoadMethod]
    private static void Init()
    {
        if (isInitialized) return;
        EditorApplication.update += OnUpdate;
        watcher = new FileSystemWatcher(ExportExcel.ImportExcelPath, "*.xlsx");
        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.CreationTime;
        watcher.EnableRaisingEvents = true;
        fileChangedCb = new FileSystemEventHandler(OnDataTableChanged);
        fileRenameCb = new RenamedEventHandler(OnDataTableChanged);
        watcher.Changed += fileChangedCb;
        watcher.Deleted += fileChangedCb;
        watcher.Renamed += fileRenameCb;
        watcher.Created += fileChangedCb;
        isInitialized = true;
        catchExcelSo();
    }

    private static void OnUpdate()
    {
        if (curChangeEventArgs != null)
        {
            //var fileName = Path.GetFileNameWithoutExtension(curChangeEventArgs.Name);
            //if (ArrayUtility.Contains(PreloadProcedure.dataTables, fileName))
            //{
            //    MyGameTools.RefreshAllDataTable(new string[] { fileName });
            //    Debug.LogFormat("Auto Refresh DataTable:{0}", curChangeEventArgs.FullPath);
            //}
            //else if (ArrayUtility.Contains(PreloadProcedure.configs, fileName))
            //{
            //    MyGameTools.RefreshAllConfig(PreloadProcedure.configs);
            //    Debug.LogFormat("Auto Refresh Config:{0}", curChangeEventArgs.FullPath);
            //}
            if (curChangeEventArgs.ChangeType == WatcherChangeTypes.Deleted)
            {
                ExportExcel.DelExcelData(curChangeEventArgs.Name, so);
            }
            else
            {
                ExportExcel.ExportExcelData(curChangeEventArgs.Name, so);
            }
            if (so)
            {
                UnityEditor.EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssetIfDirty(so);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            curChangeEventArgs = null;
        }
    }

    private static void OnDataTableChanged(object sender, FileSystemEventArgs e)
    {
        curChangeEventArgs = e;
       
    }

    // 创建并缓存一个配置信息文件
    private static void catchExcelSo()
    {  
        if (!File.Exists("Assets/Editor/ExcelTools/ExcelSetting.asset"))
        {
            ExcelLocalSoTool.CreateExcelSo();          
        }
        if (so == null)
            so = AssetDatabase.LoadAssetAtPath<ExcelLocalSO>("Assets/Editor/ExcelTools/ExcelSetting.asset");
    }

}
