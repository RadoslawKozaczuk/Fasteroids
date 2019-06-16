using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static GameEngine;

class CollisionSystemV2 : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate() => commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    [BurstCompile]
    struct FindQuadrantSystemJob : IJobForEachWithEntity<Translation>
    {
        // command buffer allows us to add or remove components as well as create or destroy entities
        [ReadOnly] public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;

        [ReadOnly] NativeMultiHashMapIterator<int> _nativeMultiHashMapIterator;

        public void Execute([ReadOnly] Entity entity, [ReadOnly] int index, ref Translation translation)
        {
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);

            // cycle through the entities in that hash map key
            if (QuadrantMultiHashMap.TryGetFirstValue(hashMapKey, out QuadrantData quadrantData, out _nativeMultiHashMapIterator))
            {
                do
                {
                    // cycling through all the values
                    if (entity != quadrantData.Entity && 
                        // this is fast, I tried to use my own version with cheaper sqrt approximation, 
                        // but ECS is so powerful that it is just a waste of code to be honest
                        math.distance(
                            new float2(translation.Value.x, translation.Value.y),
                            new float2(quadrantData.EntityPosition.x, quadrantData.EntityPosition.y)
                        ) < AsteroidRadius2)
                    {
                        // collision between asteroids - destroy both
                        EntityCommandBuffer.DestroyEntity(index, entity);
                        EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);



                        break;
                    }
                }
                while (QuadrantMultiHashMap.TryGetNextValue(out quadrantData, ref _nativeMultiHashMapIterator));
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var findQuadrantSystemJob = new FindQuadrantSystemJob
        {
            EntityCommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            QuadrantMultiHashMap = QuadrantSystem.multiHashMap
        };

        JobHandle jobHandle = findQuadrantSystemJob.Schedule(this, inputDeps);

        return jobHandle;
    }
}
