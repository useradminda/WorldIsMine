using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 渲染组件
public class RenderComponent : MonoBehaviour
{
    private bool singleRender = false;
    private void Awake()
    {
        if (RenderArray == null || RenderArray.Length <= 1)
        {
            singleRender = true;
        }
        else
        {
            singleRender = false;
        }
    }
   

    private MaterialPropertyBlock propertyBlock;
    public MaterialPropertyBlock PropertyBlock
    {
        get
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            return propertyBlock;
        }
    }
    private Renderer render;
    public Renderer Render
    {
        get
        {
            if(render == null)
               render = GetComponentInChildren<Renderer>();
            return render;
        }
    }

    private Renderer[] renderArray;
    public Renderer[] RenderArray
    {
        get
        {
            if (renderArray == null)
                renderArray = GetComponentsInChildren<Renderer>();        
            return renderArray;
        }
    }

    public void SetPropertyBlockFloat(string _propName, float _propValue)
    {
        if (singleRender)
        {
            PropertyBlock.SetFloat(_propName, _propValue);
            Render.SetPropertyBlock(PropertyBlock);
        }
        else
        {
            for(int i = 0; i < RenderArray.Length; i++)
            {
                PropertyBlock.SetFloat(_propName, _propValue);
                RenderArray[i].SetPropertyBlock(PropertyBlock);
            }
        }
    }

    public void SetPropertyBlockColor(string _propName, Color _color)
    {
        if (singleRender)
        {
            PropertyBlock.SetColor(_propName, _color);
            Render.SetPropertyBlock(PropertyBlock);
        }
        else
        {
            for (int i = 0; i < RenderArray.Length; i++)
            {
                PropertyBlock.SetColor(_propName, _color);
                RenderArray[i].SetPropertyBlock(PropertyBlock);
            }
        }
    }

    public void SetPropertyBlockVector(string _propName, Vector4 _vector)
    {
        if (singleRender)
        {
            PropertyBlock.SetVector(_propName, _vector);
            Render.SetPropertyBlock(PropertyBlock);
        }
        else
        {
            for (int i = 0; i < RenderArray.Length; i++)
            {
                PropertyBlock.SetVector(_propName, _vector);
                RenderArray[i].SetPropertyBlock(PropertyBlock);
            }
        }
    }

    // 设置材质
    public void SetMat(Material _mat)
    {
        if (singleRender)
        {

            Render.sharedMaterial = _mat;
        }
        else
        {
            for (int i = 0; i < RenderArray.Length; i++)
            {
                if(RenderArray[i].sharedMaterial.name == "ShadowMat" || RenderArray[i].sharedMaterial.name == "Boom")
                { 
                    continue; 
                }
                RenderArray[i].sharedMaterial = _mat;
            }
        }
    }
}
