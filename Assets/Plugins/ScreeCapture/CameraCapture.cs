
using RenderHeads.Media.AVProMovieCapture;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;


// 相机录制
public class CameraCapture : MonoBehaviour
{
    // 相机拍摄组件
    public CaptureFromCamera CapFromCamera;
    private Action compeleteCallBack;

    // 相机录制
    public bool RecordFromCamera(float recordTime, string videoName, Action compeleteCallBack)
    {
        Camera mainCam = Camera.main;

        StopCameraCapture();
        CapFromCamera.SetCamera(mainCam);      

     
        string outPutPath = Application.persistentDataPath + "/tempUpLoadRes/";
        if (!Directory.Exists(outPutPath))
        {
            Directory.CreateDirectory(outPutPath);
        }
        string filePath = outPutPath + videoName;
        //if (CreationToolsUtil.FileExit(filePath))
        //{
        //    CreationToolsUtil.FileDel(filePath);
        //}
        Debug.LogError(outPutPath);
        CapFromCamera.SelectRecordingResolution(1500, 750);
        CapFromCamera.OutputTarget = OutputTarget.VideoFile;
        if (recordTime > 0)
        {
            CapFromCamera.StopMode = StopMode.SecondsEncoded;
            CapFromCamera.StopAfterSecondsElapsed = recordTime;
        }
        else
        {
            CapFromCamera.StopMode = StopMode.None;
        }
        CapFromCamera.OutputFolder = CaptureBase.OutputPath.RelativeToPeristentData;
        CapFromCamera.OutputFolderPath = outPutPath;
        CapFromCamera.VideoFileNameWithEx = videoName;
        CapFromCamera.CompletedFileWritingAction -= OnCompleted;
        CapFromCamera.CompletedFileWritingAction += OnCompleted;
        this.compeleteCallBack = compeleteCallBack;
        CapFromCamera.StartCapture();
        return true;
    }

    // 暂停录像
    public void StopCameraCapture()
    {
        if (CapFromCamera.IsCapturing())
            CapFromCamera.StopCapture();
        Camera mainCam = Camera.main;
        CapFromCamera.VideoFileNameWithEx = null;
    }

    // 保存相机画面截图
    int width = 1500;
    int height = 750;
    public byte[] SaveCameraRenderJpg(string file_name, Action<int, string> action)
    {
        try
        {
            var oldTarget = Camera.main.targetTexture;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1; // 如需更清晰可设为2/4/8（性能更高）
            Camera.main.targetTexture = rt;

            Camera.main.Render(); // 立即渲染到 rt
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            tex.Apply(false);

            var bytes = tex.EncodeToPNG();
           // Debug.LogError("CoverPNGPath : " + CreationHttpManager.Inst.SaveUpLoadTempRes(file_name, bytes));
            // 还原状态
            Camera.main.targetTexture = oldTarget;
            RenderTexture.active = null;

            DestroyImmediate(rt);
            DestroyImmediate(tex);
            action?.Invoke(0, "截图跳舞封面成功");
            return bytes;
        }
        catch
        {
            action?.Invoke(1, "截图跳舞封面不成功");
            return null;
        }
    }

    public void SaveVideoToGallery(string fileName)
    {
#if UNITY_ANDROID && UNITY_EDITOR
        string sourcePath = Path.Combine(Application.persistentDataPath, "RecordVideos", fileName);
        
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("源视频文件不存在: " + sourcePath);
            return;
        }

        if (GetAndroidSDKVersion() >= 29) // Android 10+
        {
            StartCoroutine(SaveViaMediaStoreCoroutine(sourcePath, fileName));
        }
        else // Android 9及以下
        {
            StartCoroutine(SaveViaLegacyMethodCoroutine(sourcePath, fileName));
        }
#endif
    }

    private IEnumerator SaveViaMediaStoreCoroutine(string sourcePath, string fileName)
    {
        yield return new WaitForEndOfFrame();

        try
        {
            using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
            using (AndroidJavaObject unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // 创建ContentValues - 使用正确的列名常量
                AndroidJavaObject contentValues = new AndroidJavaObject("android.content.ContentValues");

                // 获取MediaColumns类以访问列名常量
                using (AndroidJavaClass mediaColumns = new AndroidJavaClass("android.provider.MediaStore$MediaColumns"))
                {
                    // 使用正确的列名常量
                    string displayName = mediaColumns.GetStatic<string>("DISPLAY_NAME");
                    string mimeType = mediaColumns.GetStatic<string>("MIME_TYPE");
                    string relativePath = GetAndroidSDKVersion() >= 29 ?
                        mediaColumns.GetStatic<string>("RELATIVE_PATH") : "_data";

                    contentValues.Call("put", displayName, fileName);
                    contentValues.Call("put", mimeType, "video/mp4");

                    if (GetAndroidSDKVersion() >= 29)
                    {
                        contentValues.Call("put", relativePath, "Movies");
                    }
                    else
                    {
                        // 使用兼容方法获取公共目录
                        string publicDir = GetLegacyMoviesDirectory();
                        string destinationPath = Path.Combine(publicDir, fileName);
                        contentValues.Call("put", relativePath, destinationPath);
                    }
                }

                // 获取ContentResolver
                AndroidJavaObject contentResolver = unityActivity.Call<AndroidJavaObject>("getContentResolver");

                // 获取MediaStore URI
                AndroidJavaObject collectionUri;
                using (AndroidJavaClass mediaStore = new AndroidJavaClass("android.provider.MediaStore$Video$Media"))
                {
                    collectionUri = mediaStore.CallStatic<AndroidJavaObject>("getContentUri", "external");
                }

                // 插入新记录
                AndroidJavaObject publicUri = contentResolver.Call<AndroidJavaObject>("insert", collectionUri, contentValues);

                if (publicUri != null)
                {
                    // 分块读取和写入文件
                    const int bufferSize = 8192; // 8KB
                    byte[] buffer = new byte[bufferSize];

                    using (FileStream fs = File.OpenRead(sourcePath))
                    using (AndroidJavaObject outputStream = contentResolver.Call<AndroidJavaObject>("openOutputStream", publicUri))
                    {
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
                        {
                            // 创建正确大小的sbyte数组
                            sbyte[] chunk = new sbyte[bytesRead];
                            for (int i = 0; i < bytesRead; i++)
                            {
                                chunk[i] = (sbyte)buffer[i];
                            }

                            outputStream.Call("write", chunk);
                        }

                        outputStream.Call("flush");
                        outputStream.Call("close");
                    }

                    Debug.Log($"视频已保存到相册: {publicUri.Call<string>("toString")}");

                    // 通知媒体扫描器刷新
                    ScanFile(unityActivity, sourcePath);
                }
                else
                {
                    Debug.LogError("无法创建MediaStore条目");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MediaStore保存失败: {e.Message}\n{e.StackTrace}");
        }
    }

    private IEnumerator SaveViaLegacyMethodCoroutine(string sourcePath, string fileName)
    {
        yield return new WaitForEndOfFrame();

        try
        {
            // 使用兼容方法获取公共目录
            string publicDir = GetLegacyMoviesDirectory();
            string destinationPath = Path.Combine(publicDir, fileName);

            // 确保目录存在
            if (!Directory.Exists(publicDir))
            {
                Directory.CreateDirectory(publicDir);
            }

            // 复制文件
            File.Copy(sourcePath, destinationPath, true);
            Debug.Log("视频已保存到: " + destinationPath);

            // 通知媒体扫描器
            using (AndroidJavaObject unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                ScanFile(unityActivity, destinationPath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("传统保存失败: " + e.Message);
        }
    }

    // 兼容方法获取公共视频目录
    private string GetLegacyMoviesDirectory()
    {
        try
        {
            // 使用Environment的getExternalStoragePublicDirectory方法
            using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
            {
                // 获取DIRECTORY_MOVIES常量
                string dirMovies = environment.GetStatic<string>("DIRECTORY_MOVIES");

                // 调用getExternalStoragePublicDirectory方法
                AndroidJavaObject moviesDir = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", dirMovies);

                if (moviesDir != null)
                {
                    return moviesDir.Call<string>("getAbsolutePath");
                }
            }
        }
        catch
        {
            // 忽略异常，继续尝试其他方法
        }

        // 备选方案：使用Application.persistentDataPath
        try
        {
            string basePath = Application.persistentDataPath;
            // 获取Movies目录
            return Path.Combine(basePath, "..", "Movies");
        }
        catch
        {
            // 最终备选方案：硬编码路径
            return "/sdcard/Movies";
        }
    }

    private void ScanFile(AndroidJavaObject context, string filePath)
    {
        try
        {
            using (AndroidJavaClass mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection"))
            {
                // 正确调用静态方法
                object[] parameters = new object[] { context, new string[] { filePath }, new string[] { "video/mp4" }, null };
                mediaScanner.CallStatic("scanFile", parameters);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("媒体扫描失败: " + e.Message);
        }
    }

    IEnumerator RequestSaveVideo(string fileName)
    {
        if (GetAndroidSDKVersion() >= 30) // Android 11+
        {
            SaveVideoToGallery(fileName);
            yield break;
        }

        // 检查是否已有权限
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            // 请求权限
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);

            // 等待用户响应
            float timeout = 30f; // 5秒超时
            float elapsed = 0f;

            while (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Debug.LogError("存储权限被拒绝");
                yield break;
            }
        }

        // 有权限后保存视频
        SaveVideoToGallery(fileName);
    }

    private int GetAndroidSDKVersion()
    {
        throw new NotImplementedException();
    }

    void OnCompleted(FileWritingHandler handler)
    {
#if (UNITY_ANDROID || UNITY_IOS) && UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (handler.Status == FileWritingHandler.CompletionStatus.Completed)
        {
            string[] paths = handler.Path.Split("/");
            string fileName = paths[paths.Length-1];
            // 检查文件是否存在
            if (!System.IO.File.Exists(handler.Path))
            {
                Debug.LogError("源视频文件不存在: " + handler.Path);
                return;
            }

            StartCoroutine(RequestSaveVideo(fileName));
        }
#endif
        this.compeleteCallBack?.Invoke();
        Debug.LogError("录制视频完成");
    }

    private void OnApplicationQuit()
    {
        if (CapFromCamera.IsCapturing())
            CapFromCamera.StopCapture(true, true);
    }

}
