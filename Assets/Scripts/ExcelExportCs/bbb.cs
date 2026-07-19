using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class bbbConfig
{
    private static bbbConfig ins;
    public static bbbConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new bbbConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<bbb> dataList = new List<bbb>();
    public List<bbb> ConfigDataList => dataList;

    private void init()
    {
        string json = File.ReadAllText(Application.streamingAssetsPath + "bbb.json");
        dataList = JsonConvert.DeserializeObject<List<bbb>>(json);
    }

    public bbb SearchById(int id)
    {
        for(int i = 0; i < ConfigDataList.Count; i++)
        {
            if (ConfigDataList[i].id == id)
            {
                return ConfigDataList[i];
            }
        }
        return null;
    }
}

public class bbb
{
	public int id;
	public string wocao2;
	public string[] sigaoyi2;
	public float dsd2;
	public float ac2;
	
}