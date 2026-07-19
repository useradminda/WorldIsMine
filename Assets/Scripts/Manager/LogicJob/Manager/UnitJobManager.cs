using Nebukam.ORCA;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitJobManager : MonoBehaviour
{
    public GameObject HeroPrefab;

    private int countX = 50;
    private int countZ = 50;

    NativeArray<float3> positions;

    TransformAccessArray transformsAccessArray;

    private void Start()
    {
        Application.targetFrameRate = 120;

        positions = new NativeArray<float3>(countX * countZ, Allocator.Persistent);

        Transform[] trs = new Transform[countX * countZ];

        for (int i = 0; i < countX * countZ; i++)
        {
            float3 pos = new float3(i * -countX, 0, -countZ);
            positions[i] = pos;
            var go = Instantiate(HeroPrefab);
            go.transform.position = pos;
            trs[i] = go.transform;
        }

        transformsAccessArray = new TransformAccessArray(trs);

        RvoManager.Instance.ManagerInit();

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                Agent agent = UnitFactory.CreateAgent(new float3(-1 * x, 1, -2 * z), new float3(0, 0, 0), 2f, 10f);
                RvoManager.Instance.AddAgentCurrent(agent);
                agent.prefVelocity = new float3(0, 0, 1);
            }
        }
    }

    void Update()
    {
        RvoManager.Instance.ManagerUpdate();

        for (int i = 0; i < countX * countZ; i++)
        {
            positions[i] =
                RvoManager.Instance
                .Agents[i]
                .pos;
            Agent a = RvoManager.Instance
                .Agents[i];
            a.prefVelocity = new float3(0, 0, -10);
          //  a.velocity = new float3(0, 0, 1);
        }

        //var moveJob =
        //    new MoveJob()
        //    {
        //        positions = positions,
        //        deltaTime = Time.deltaTime 
        //    };

        //JobHandle moveHandle = moveJob.Schedule(countX * countZ, 64);

        var syncJob =
        new SyncTransformJob()
        {
            positions = positions
        };

        syncJob
            .Schedule(
                transformsAccessArray)
            .Complete(); 
    }

    private void LateUpdate()
    {
        RvoManager.Instance.ManagerLateUpdate();
    }

    void OnDestroy()
    {
        positions.Dispose();
        transformsAccessArray.Dispose();
    }
}
