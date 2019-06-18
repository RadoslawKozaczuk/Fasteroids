using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using static GameEngine;

class CollisionSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem _commandBufferSystem;
    EntityArchetype _asteroidRespawn;

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _asteroidRespawn = World.Active.EntityManager.CreateArchetype(typeof(TimeToRespawn));
    }

    struct FindQuadrantSystemJob : IJobForEachWithEntity<Translation, CollisionTypeData>
    {
        // command buffer allows us to add or remove components as well as create or destroy entities
        [ReadOnly] public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        [ReadOnly] public EntityArchetype AsteroidRespawnArchetype;
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;

        [ReadOnly] NativeMultiHashMapIterator<int> _nativeMultiHashMapIterator;

        public void Execute(
            [ReadOnly] Entity entity, 
            [ReadOnly] int index, 
            [ReadOnly] ref Translation translation, 
            [ReadOnly] ref CollisionTypeData collisionType)
        {
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);

            // cycle through the entities in that hash map key
            if (QuadrantMultiHashMap.TryGetFirstValue(hashMapKey, out QuadrantData quadrantData, out _nativeMultiHashMapIterator))
            {
                CollisionTypeEnum typeFirst = collisionType.CollisionObjectType;
                do
                {
                    // cycling through all the values
                    if (entity.Index < quadrantData.Entity.Index && // to not duplicate checks
                        // this is fast, I tried to use my own version with cheaper sqrt approximation, 
                        // but ECS is so powerful that it is just a waste of code to be honest
                        math.distance(
                            new float2(translation.Value.x, translation.Value.y),
                            new float2(quadrantData.EntityPosition.x, quadrantData.EntityPosition.y)
                        ) < AsteroidRadius2)
                    {
                        CollisionTypeEnum typeSecond = quadrantData.CollisionTypeEnum;

                        if(typeFirst == CollisionTypeEnum.Player)
                        {
                            if(typeSecond == CollisionTypeEnum.Asteroid)
                            {
                                EntityCommandBuffer.AddComponent(index, entity, new DeadData());
                                // I have to somehow inform the rest of the game that player is dead
                                // for now I'll just remove the Render component to prevent player rendering
                                EntityCommandBuffer.RemoveComponent<RenderMesh>(index, entity);
                            }
                        }
                        else if(typeFirst == CollisionTypeEnum.Laser)
                        {
                            if (typeSecond == CollisionTypeEnum.Asteroid)
                            {
                                EntityCommandBuffer.DestroyEntity(index, entity);
                                Entity e1 = EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype);
                                EntityCommandBuffer.SetComponent(index, e1, new TimeToRespawn() { Time = 1f });
                                EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                                Entity e2 = EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype);
                                EntityCommandBuffer.SetComponent(index, e2, new TimeToRespawn() { Time = 1f });
                            }
                        }
                        else
                        {
                            if(typeSecond == CollisionTypeEnum.Asteroid || typeSecond == CollisionTypeEnum.Laser)
                            {
                                EntityCommandBuffer.DestroyEntity(index, entity);
                                Entity e1 = EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype);
                                EntityCommandBuffer.SetComponent(index, e1, new TimeToRespawn() { Time = 1f });
                                EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                                Entity e2 = EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype);
                                EntityCommandBuffer.SetComponent(index, e2, new TimeToRespawn() { Time = 1f });
                            }
                        }

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
            EntityCommandBuffer = _commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            AsteroidRespawnArchetype = _asteroidRespawn,
            QuadrantMultiHashMap = QuadrantSystem.MultiHashMap,
        };

        JobHandle jobHandle = findQuadrantSystemJob.Schedule(this, inputDeps);

        return jobHandle;
    }
}
