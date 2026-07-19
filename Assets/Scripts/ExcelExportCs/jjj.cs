using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class jjjConfig
{
    private static jjjConfig ins;
    public static jjjConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new jjjConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<jjj> dataList = new List<jjj>();
    public List<jjj> ConfigDataList => dataList;

    private void init()
    {
        string json = File.ReadAllText(Application.streamingAssetsPath + "jjj.json");
        dataList = JsonConvert.DeserializeObject<List<jjj>>(json);
    }

    public jjj SearchById(int id)
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

public class jjj
{
	public int id;
	public string wocao;
	public string[] sigaoyi;
	public float[] dsd;
	public float ac;
	
}