using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using ZTools;

/// <summary>从 Resources 加载并缓存图集和 Sprite 的管理器。</summary>
public class SpriteLoadManager : Singleton<SpriteLoadManager>
{
    private readonly Dictionary<string, SpriteAtlas> atlasCache = new Dictionary<string, SpriteAtlas>();
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    /// <summary>构造精灵加载管理器。</summary>
    public SpriteLoadManager()
    {
    }

    /// <summary>从 Resources 下的 SpriteAtlas 获取精灵。</summary>
    public Sprite LoadFromAtlas(string atlasPath, string spriteName)
    {
        string cacheKey = atlasPath + "/" + spriteName;
        Sprite sprite;
        if (spriteCache.TryGetValue(cacheKey, out sprite))
        {
            return sprite;
        }

        SpriteAtlas atlas = LoadAtlas(atlasPath);
        if (atlas == null)
        {
            return null;
        }

        sprite = atlas.GetSprite(spriteName);
        if (sprite != null)
        {
            spriteCache[cacheKey] = sprite;
        }

        return sprite;
    }

    /// <summary>从 Resources 下直接获取精灵资源。</summary>
    public Sprite LoadSprite(string spritePath)
    {
        Sprite sprite;
        if (spriteCache.TryGetValue(spritePath, out sprite))
        {
            return sprite;
        }

        sprite = Resources.Load<Sprite>(spritePath);
        if (sprite != null)
        {
            spriteCache[spritePath] = sprite;
        }

        return sprite;
    }

    /// <summary>清理精灵缓存。</summary>
    public void ClearCache()
    {
        spriteCache.Clear();
        atlasCache.Clear();
    }

    /// <summary>加载并缓存图集。</summary>
    private SpriteAtlas LoadAtlas(string atlasPath)
    {
        SpriteAtlas atlas;
        if (atlasCache.TryGetValue(atlasPath, out atlas))
        {
            return atlas;
        }

        atlas = Resources.Load<SpriteAtlas>(atlasPath);
        if (atlas != null)
        {
            atlasCache[atlasPath] = atlas;
        }

        return atlas;
    }
}
