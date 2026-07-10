using Nebukam;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitManager : MonoBehaviour
{
    public GameObject HeroPrefab;

    private int count = 70000;

    NativeArray<float3> positions;

    TransformAccessArray transforms;

    private void Start()
    {
        positions = new NativeArray<float3>(count, Allocator.Persistent);

        Transform[] trs = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            float3 pos = new float3(
                UnityEngine.Random.Range(-100f, 100f),
                0,
                UnityEngine.Random.Range(-100f, 100f));

            positions[i] = pos;

            var go =
                Instantiate(HeroPrefab);

            go.transform.position = pos;

            trs[i] = go.transform;
        }
    }

    void Update()
    {
        var moveJob =
            new MoveJob()
            {
                positions = positions,
                deltaTime = Time.deltaTime
            };

        JobHandle moveHandle = moveJob.Schedule(count, 64);

        var syncJob =
            new SyncTransformJob()
            {
                positions = positions
            };

        JobHandle syncHandle =
            syncJob.Schedule(
                transforms,
                moveHandle);

        syncHandle.Complete();
    }

    void OnDestroy()
    {
        positions.Dispose();
        transforms.Dispose();
    }
}
