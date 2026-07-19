using System;
using System.IO;
using UnityEngine;

namespace ZTools
{
    /// <summary>
    /// 存储文件工具类
    /// </summary>
    public static class FileOperateEditorTool
    {
        // 编辑器存本地 保存byte数据
        public static void SaveBytesInEditor(string filePathWithEx, byte[] bytes)
        {   
            File.WriteAllBytes(Path.Combine(Application.dataPath , filePathWithEx), bytes);
        }

        // 编辑器读本地 读取byte数据
        public static byte[] ReadBytesInEditor(string filePathWithEx)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, filePathWithEx));
            return bytes;
        }
    }
}
