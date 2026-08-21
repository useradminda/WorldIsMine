using System;
using System.Collections.Generic;
using Unity.Collections;

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

    public T this[int index]
    {
        get => dataList[index];
    }

    public WaitListTemplate(Action<T> addCallBack)
    {
        AddCallBack = addCallBack;
    }

    //// 增加一个Unit
    //public T Add(T data)
    //{
    //    waitingAddList.Add(data);
    //    return data;
    //}

    public T AddImmediately(T data)
    {
        dataList.Add(data);
        AddCallBack?.Invoke(data);
        return data;
    }

    //public T Remove(T data)
    //{
    //    waitingRemoveList.Add(data);
    //    return data;
    //}

    public void RemoveSwapBack(T data)
    {
        waitingRemoveList.RemoveSwapBack(data);
    }

    public void AddWaitingList()
    {
        for (int i = 0; i < waitingAddList.Count; i++)
        {
            dataList.Add(waitingAddList[i]);
            AddCallBack?.Invoke(waitingAddList[i]);
        }
        waitingAddList.Clear();
    }

    public void RemoveImmediately(T data)
    {
        dataList.Remove(data);
    }
}
