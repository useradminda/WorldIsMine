using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class cfg_giftConfig
{
    private static cfg_giftConfig ins;
    public static cfg_giftConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new cfg_giftConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<cfg_gift> dataList = new List<cfg_gift>();
    public List<cfg_gift> ConfigDataList => dataList;

    private void init()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "JsonData",
            "cfg_gift.json");
        string json = File.ReadAllText(path);
        dataList = JsonConvert.DeserializeObject<List<cfg_gift>>(json);
    }

    public cfg_gift SearchById(int id)
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

public class cfg_gift
{
	public int id;
	public string name;
	public string platform;
	public float cost;
	public string prefab;
	public int giftValue;
	
}
