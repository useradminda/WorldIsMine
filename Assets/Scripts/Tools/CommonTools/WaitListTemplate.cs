using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitListTemplate<T>
{
    // 单位列表
    public List<T> DataList => dataList;
    private List<T> dataList = new List<T>();

    // 等待添加的列表
    private List<T> waitingAddList = new List<T>();
    // 等待退出的列表
    private List<T> waitingRemoveList = new List<T>();

    public Action<T> AddCallBack;

    public int Count
    {
        get { return dataList.Count; }
    }

    public WaitListTemplate(Action<T> addCallBack)
    {
        AddCallBack = addCallBack;
    }

    // 增加一个Unit
    public T Add(T data)
    {
        waitingAddList.Add(data);
        return data;
    }

    public T Remove(T data)
    {
        waitingRemoveList.Add(data);
        return data;
    }

    // 添加等待入队列表
    public void AddWaitingList()
    {
        for (int i = 0; i < waitingAddList.Count; i++)
        {
            dataList.Add(waitingAddList[i]);
            AddCallBack?.Invoke(waitingAddList[i]);
        }
        waitingAddList.Clear();
    }
}
