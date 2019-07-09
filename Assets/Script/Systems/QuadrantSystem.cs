using Assets.Script.Components;
using Assets.Script.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    [UpdateInGroup(typeof(UpdateGroup2))]
    class QuadrantSystem : ComponentSystem
    {
        public static NativeMultiHashMap<int, QuadrantData> MultiHashMap;

        public const int QuadrantMultiplier = 10_000; // y coordinates are stored in millions while x are stored in units
        public const int QuadrantCellSize = 2;

        EntityQuery _query;

        public static int GetPositionHashMapKey(float3 position)
            => (int)(math.floor(position.x / QuadrantCellSize) + (QuadrantMultiplier * math.floor(position.y / QuadrantCellSize)));

        public static void RetrieveComponentsFromHashMapKey(int hashMapKey, out int xComponent, out int yComponent)
        {
            xComponent = hashMapKey % QuadrantMultiplier;
            yComponent = (hashMapKey - xComponent) / QuadrantMultiplier;
        }

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
                None = new ComponentType[] { typeof(DeadData), typeof(TimeToRespawnData) },
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

        static int GetEntityCountInHashMap(NativeMultiHashMap<int, QuadrantData> hashMap, int hashKey)
        {
            int count = 0;
            if (hashMap.TryGetFirstValue(hashKey, out _, out NativeMultiHashMapIterator<int> nativeMultiHashMapIterator))
            {
                do
                {
                    count++;
                } while (hashMap.TryGetNextValue(out _, ref nativeMultiHashMapIterator));
            }

            return count;
        }

        protected override void OnUpdate()
        {
            MultiHashMap.Clear(); // need to be cleared because it is persistent

            // adjust its size to match the current needs
            int entityNumber = _query.CalculateLength();
            if (entityNumber > MultiHashMap.Capacity)
                MultiHashMap.Capacity = entityNumber;

            var setQuadrantDataHashMapJob = new SetQuadrantHashMapDataJob
            {
                NativeMultiHashMap = MultiHashMap.ToConcurrent()
            };

            JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, _query);
            jobHandle.Complete();

            // === DEBUG DRAW ===
            // draw quadrants around the player's 
            if (Debug.isDebugBuild && GameEngine.Instance.DrawCollisionQuadrants)
            {
                float posX = GameEngine.SpaceshipInstance.transform.position.x;
                float posY = GameEngine.SpaceshipInstance.transform.position.y;
                for (int i = -1; i <= 1; i++)
                    for (int y = -1; y <= 1; y++)
                        DebugDrawMethods.DebugDrawQuadrant(posX + i * QuadrantCellSize, posY + y * QuadrantCellSize, Color.yellow);

                Debug.Log(GetEntityCountInHashMap(MultiHashMap, GetPositionHashMapKey(Utils.GetMouseWorldPosition())));
            }
        }
    }
}