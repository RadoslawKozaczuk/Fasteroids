using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct QuadrantEntity : IComponentData
{
    public enum TypeEnum { Spaceship, LaserBeam, Asteroid }

    public TypeEnum Type;
}

public struct QuadrantData
{
    public Entity Entity;
    public float3 EntityPosition;
    public QuadrantEntity QuadrantEntity;
}

class QuadrantSystem : ComponentSystem
{
    const int QuadrantMultiplier = 10_000; // how many quadrants we can have per row
    const int QuadrantCellSize = 4;

    public static NativeMultiHashMap<int, QuadrantData> multiHashMap;

    public static int GetPositionHashMapKey(float3 position)
    {
        return (int)(math.floor(position.x / QuadrantCellSize) + (QuadrantMultiplier * math.floor(position.y / QuadrantCellSize)));
    }

    [BurstCompile]
    struct SetQuadrantHashMapDataJob : IJobForEachWithEntity<Translation>
    {
        public NativeMultiHashMap<int, QuadrantData>.Concurrent NativeMultiHashMap;

        public void Execute([ReadOnly] Entity entity, [ReadOnly] int index, ref Translation translation)
        {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            NativeMultiHashMap.Add(
                hashMapKey, 
                new QuadrantData { Entity = entity, EntityPosition = translation.Value });
        }
    }

    protected override void OnCreate()
    {
        multiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        multiHashMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation));

        multiHashMap.Clear(); // need to be cleared because it is persistent

        // adjust its size to match the current needs
        int entityNumber = entityQuery.CalculateLength();
        if (entityNumber > multiHashMap.Capacity)
            multiHashMap.Capacity = entityNumber;

        var setQuadrantDataHashMapJob = new SetQuadrantHashMapDataJob { NativeMultiHashMap = multiHashMap.ToConcurrent() };

        JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, entityQuery);
        jobHandle.Complete();
    }
}
