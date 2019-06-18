using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// Collision system is using the quadrant spatial partitioning algorithm to reduce the number of collision checks.
/// I chose this approach as it is the easiest one to implement in ECS syntax. 
/// Overall results are outstanding because Boost Compiler provides so much power that any other algorithm 
/// no matter how excellently written in any other non-ECS way would have been insanely slower in comparison to this anyway.
/// 
/// Glory to ECS!
/// </summary>
class CollisionSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem _commandBufferSystem;
    EntityArchetype _asteroidRespawn;

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _asteroidRespawn = World.Active.EntityManager.CreateArchetype(typeof(GameEngine.TimeToRespawn));
    }

    struct FindQuadrantSystemJob : IJobForEachWithEntity<Translation, GameEngine.CollisionTypeData>
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
            [ReadOnly] ref GameEngine.CollisionTypeData collisionType)
        {
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);

            // cycle through the entities in that hash map key
            if (QuadrantMultiHashMap.TryGetFirstValue(hashMapKey, out QuadrantData quadrantData, out _nativeMultiHashMapIterator))
            {
                GameEngine.CollisionTypeEnum typeFirst = collisionType.CollisionObjectType;
                do
                {
                    // cycling through all the values
                    if (entity.Index < quadrantData.Entity.Index && // to not duplicate checks
                        // this is fast, I tried to use my own version with cheaper sqrt approximation, 
                        // but ECS is so powerful that it is just a waste of code to be honest
                        math.distance(
                            new float2(translation.Value.x, translation.Value.y),
                            new float2(quadrantData.EntityPosition.x, quadrantData.EntityPosition.y)
                        ) < GameEngine.AsteroidRadius2)
                    {
                        GameEngine.CollisionTypeEnum typeSecond = quadrantData.CollisionTypeEnum;

                        if(typeFirst == GameEngine.CollisionTypeEnum.Player)
                        {
                            if(typeSecond == GameEngine.CollisionTypeEnum.Asteroid)
                            {
                                // "destroy" player
                                EntityCommandBuffer.AddComponent(index, entity, new GameEngine.DeadData());
                                GameEngine.DidPlayerDieThisFrame = true;
                                EntityCommandBuffer.RemoveComponent<RenderMesh>(index, entity);

                                // destroy asteroid
                                EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                                EntityCommandBuffer.SetComponent(
                                    index, 
                                    EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype), 
                                    new GameEngine.TimeToRespawn() { Time = 1f });
                            }
                        }
                        else if(typeFirst == GameEngine.CollisionTypeEnum.Laser)
                        {
                            if (typeSecond == GameEngine.CollisionTypeEnum.Asteroid)
                            {
                                EntityCommandBuffer.DestroyEntity(index, entity);
                                EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                                EntityCommandBuffer.SetComponent(
                                    index, 
                                    EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype), 
                                    new GameEngine.TimeToRespawn() { Time = 1f });
                            }
                        }
                        else if(typeFirst == GameEngine.CollisionTypeEnum.Asteroid)
                        {
                            EntityCommandBuffer.DestroyEntity(index, entity);
                            EntityCommandBuffer.SetComponent(
                                index,
                                EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype), 
                                new GameEngine.TimeToRespawn() { Time = 1f });

                            if (typeSecond == GameEngine.CollisionTypeEnum.Asteroid)
                            {
                                EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                                EntityCommandBuffer.SetComponent(
                                    index, 
                                    EntityCommandBuffer.CreateEntity(index, AsteroidRespawnArchetype), 
                                    new GameEngine.TimeToRespawn() { Time = 1f });
                            }
                            else if(typeSecond == GameEngine.CollisionTypeEnum.Laser)
                            {
                                EntityCommandBuffer.DestroyEntity(index, quadrantData.Entity);
                            }
                            else if(typeSecond == GameEngine.CollisionTypeEnum.Player)
                            {
                                // "destroy" player
                                EntityCommandBuffer.AddComponent(index, quadrantData.Entity, new GameEngine.DeadData());
                                GameEngine.DidPlayerDieThisFrame = true;
                                EntityCommandBuffer.RemoveComponent<RenderMesh>(index, quadrantData.Entity);
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
