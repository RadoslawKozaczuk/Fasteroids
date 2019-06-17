using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using static GameEngine;

class CollisionSystemV2 : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem _commandBufferSystem;
    EntityArchetype _asteroidRespawn;

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _asteroidRespawn = World.Active.EntityManager.CreateArchetype(typeof(TimeToRespawn));
    }

    struct FindQuadrantSystemJob : IJobForEachWithEntity<Translation>
    {
        // command buffer allows us to add or remove components as well as create or destroy entities
        [ReadOnly] public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        [ReadOnly] public EntityArchetype AsteroidRespawnArchetype;
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;

        // player's tag
        [ReadOnly] public ComponentDataFromEntity<Spaceship> Spaceship;

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
                    if (entity.Index < quadrantData.Entity.Index && // to not duplicate checks
                        // this is fast, I tried to use my own version with cheaper sqrt approximation, 
                        // but ECS is so powerful that it is just a waste of code to be honest
                        math.distance(
                            new float2(translation.Value.x, translation.Value.y),
                            new float2(quadrantData.EntityPosition.x, quadrantData.EntityPosition.y)
                        ) < AsteroidRadius2)
                    {
                        // collision between asteroids - destroy both (except player's spaceship)
                        if (Spaceship.Exists(entity))
                        {
                            EntityCommandBuffer.AddComponent(index, entity, new DeadData());
                            // I have to somehow inform the rest of the game that player is dead
                            // for now I'll just remove the Render component to prevent player rendering
                            EntityCommandBuffer.RemoveComponent<RenderMesh>(index, entity);
                        }
                        else
                        {
                            EntityCommandBuffer.DestroyEntity(index, entity);
                            Entity e1 = EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype);
                            EntityCommandBuffer.SetComponent(index, e1, new TimeToRespawn() { Time = 1f });
                        }

                        if (Spaceship.Exists(quadrantData.Entity))
                        {
                            EntityCommandBuffer.AddComponent(index, quadrantData.Entity, new DeadData());
                            EntityCommandBuffer.RemoveComponent<RenderMesh>(index, quadrantData.Entity);
                        }
                        else
                        {
                            EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                            Entity e2 = EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype);
                            EntityCommandBuffer.SetComponent(index, e2, new TimeToRespawn() { Time = 1f });
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
            Spaceship = GetComponentDataFromEntity<Spaceship>()
        };

        JobHandle jobHandle = findQuadrantSystemJob.Schedule(this, inputDeps);

        return jobHandle;
    }
}
