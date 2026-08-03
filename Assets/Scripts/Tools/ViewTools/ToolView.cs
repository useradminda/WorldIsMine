using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public static class ToolView
{
    public static Vector2 ScreenPointToLocalPoint(RectTransform _rectTransform, Vector2 screenPoint, Camera camera)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPoint, camera, out localPoint))
        {
            return localPoint;
        }
        return Vector2.zero;
    }

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T t = go.GetComponent<T>();
        if (t == null)
        {
            t = go.AddComponent<T>();
        }
        return t;
    }

    public static T GetOrAddComponent<T>(this Transform go) where T : Component
    {
        T t = go.GetComponent<T>();
        if (t == null)
        {
            t = go.gameObject.AddComponent<T>();
        }
        return t;
    }

    public static T GetOrAddComponentInChild<T>(this GameObject go) where T : Component
    {
        T t = go.transform.GetComponentInChildren<T>();
        if (t == null)
        {
            t = go.GetOrAddComponent<T>();
        }
        return t;
    }
}
