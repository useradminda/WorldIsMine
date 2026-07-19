using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkillEditorResManager
{
    private static Texture timeHead;
    public static Texture TimeHeadTexture
    {
        get
        {
            if (timeHead == null)
                timeHead = (EditorGUIUtility.Load("Assets/Editor/EditorResources/Timecursor.png") as Texture);
            return timeHead;
        }
    }
}
