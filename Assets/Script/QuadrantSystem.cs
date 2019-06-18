using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct QuadrantData
{
    public Entity Entity;
    public float3 EntityPosition;
    public GameEngine.CollisionTypeEnum CollisionTypeEnum;
}

class QuadrantSystem : ComponentSystem
{
    const int QuadrantMultiplier = 10_000; // how many quadrants we can have per row
    const int QuadrantCellSize = 4;

    public static NativeMultiHashMap<int, QuadrantData> MultiHashMap;

    public static int GetPositionHashMapKey(float3 position) 
        => (int)(math.floor(position.x / QuadrantCellSize) + (QuadrantMultiplier * math.floor(position.y / QuadrantCellSize)));

    [BurstCompile]
    struct SetQuadrantHashMapDataJob : IJobForEachWithEntity<Translation, GameEngine.CollisionTypeData>
    {
        public NativeMultiHashMap<int, QuadrantData>.Concurrent NativeMultiHashMap;

        public void Execute(
            [ReadOnly] Entity entity, 
            [ReadOnly] int index, 
            [ReadOnly] ref Translation translation,
            [ReadOnly] ref GameEngine.CollisionTypeData collisionType)
        {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            NativeMultiHashMap.Add(
                hashMapKey, 
                new QuadrantData
                {
                    Entity = entity,
                    EntityPosition = translation.Value,
                    CollisionTypeEnum = collisionType.CollisionObjectType
                });
        }
    }

    protected override void OnCreate()
    {
        MultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        MultiHashMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(
            typeof(Translation),
            typeof(GameEngine.CollisionTypeData));

        MultiHashMap.Clear(); // need to be cleared because it is persistent

        // adjust its size to match the current needs
        int entityNumber = entityQuery.CalculateLength();
        if (entityNumber > MultiHashMap.Capacity)
            MultiHashMap.Capacity = entityNumber;

        var setQuadrantDataHashMapJob = new SetQuadrantHashMapDataJob { NativeMultiHashMap = MultiHashMap.ToConcurrent() };

        JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, entityQuery);
        jobHandle.Complete();
    }
}
