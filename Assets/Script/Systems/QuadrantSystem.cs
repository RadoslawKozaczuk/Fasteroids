using Assets.Script.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Script.Systems
{
    [UpdateInGroup(typeof(UpdateGroup2))]
    class QuadrantSystem : ComponentSystem
    {
        public static NativeMultiHashMap<int, QuadrantData> MultiHashMap;

        public const int QuadrantMultiplier = 10_000; // how many quadrants we can have per row
        public const int QuadrantCellSize = 4;

        EntityQuery _query;

        public static int GetPositionHashMapKey(float3 position)
            => (int)(math.floor(position.x / QuadrantCellSize) + (QuadrantMultiplier * math.floor(position.y / QuadrantCellSize)));

        [BurstCompile]
        struct SetQuadrantHashMapDataJob : IJobForEachWithEntity<Translation, CollisionTypeData>
        {
            public NativeMultiHashMap<int, QuadrantData>.Concurrent NativeMultiHashMap;

            public void Execute(
                [ReadOnly] Entity entity,
                [ReadOnly] int index,
                [ReadOnly] ref Translation translation,
                [ReadOnly] ref CollisionTypeData collisionType)
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
            var entityQueryDesc = new EntityQueryDesc
            {
                None = new ComponentType[] { typeof(DeadData), typeof(TimeToRespawn) },
                All = new ComponentType[] { ComponentType.ReadOnly<CollisionTypeData>(), ComponentType.ReadOnly<Translation>() }
            };

            _query = GetEntityQuery(entityQueryDesc);

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
            MultiHashMap.Clear(); // need to be cleared because it is persistent

            // adjust its size to match the current needs
            //int entityNumber = entityQuery.CalculateLength();
            int entityNumber = _query.CalculateLength();
            if (entityNumber > MultiHashMap.Capacity)
                MultiHashMap.Capacity = entityNumber;

            var setQuadrantDataHashMapJob = new SetQuadrantHashMapDataJob
            {
                NativeMultiHashMap = MultiHashMap.ToConcurrent()
            };

            JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, _query);
            jobHandle.Complete();
        }
    }
}