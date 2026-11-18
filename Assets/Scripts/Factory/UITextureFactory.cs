using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI Texture 资源工厂，负责从 Resources 路径加载 Texture 并按资源地址缓存。
/// 传入路径可以包含 .png 扩展名，缓存键统一使用规范化后的资源路径。
/// </summary>
public static class UITextureFactory
{
    private static readonly Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();
    private static readonly HashSet<string> missingTextureCache = new HashSet<string>();

    /// <summary>根据 Resources 资源地址加载并缓存 Texture。</summary>
    public static Texture Load(string resourcePath)
    {
        string cacheKey = NormalizePath(resourcePath);
        if (string.IsNullOrEmpty(cacheKey))
        {
            return null;
        }

        if (textureCache.TryGetValue(cacheKey, out Texture cachedTexture))
        {
            return cachedTexture;
        }

        if (missingTextureCache.Contains(cacheKey))
        {
            return null;
        }

        Texture texture = Resources.Load<Texture>(cacheKey);
        if (texture == null)
        {
            missingTextureCache.Add(cacheKey);
            return null;
        }

        textureCache[cacheKey] = texture;
        return texture;
    }

    /// <summary>清理指定资源地址的缓存。</summary>
    public static void Remove(string resourcePath)
    {
        string cacheKey = NormalizePath(resourcePath);
        if (string.IsNullOrEmpty(cacheKey))
        {
            return;
        }

        textureCache.Remove(cacheKey);
        missingTextureCache.Remove(cacheKey);
    }

    /// <summary>清理全部 UI Texture 缓存。</summary>
    public static void Clear()
    {
        textureCache.Clear();
        missingTextureCache.Clear();
    }

    /// <summary>规范化 Resources 路径并移除图片扩展名。</summary>
    private static string NormalizePath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return string.Empty;
        }

        string path = resourcePath.Replace('\\', '/').Trim();
        if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(0, path.Length - 4);
        }
        return path;
    }
}
