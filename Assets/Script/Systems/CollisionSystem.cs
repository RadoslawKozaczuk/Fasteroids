using Assets.Script.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Assets.Script.Systems
{
    /// <summary>
    /// Collision system is using the quadrant spatial partitioning algorithm to reduce the number of collision checks.
    /// I chose this approach as it is the easiest one to implement in ECS syntax. 
    /// Overall results are outstanding because Boost Compiler provides so much power that any other algorithm 
    /// no matter how excellently written in any other non-ECS way would have been insanely slower in comparison to this anyway.
    /// 
    /// Glory to ECS!
    /// </summary>
    [UpdateInGroup(typeof(UpdateGroup3))]
    class CollisionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAllReadOnly<Translation, CollisionTypeData>()
                .WithNone<TimeToRespawn>()
                .ForEach((Entity entity, ref Translation translation, ref CollisionTypeData collisionType) =>
            {
                int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);

                if (CheckInQuadrant(ref entity, translation, collisionType, hashMapKey))
                    return;

                // check in neighboring quadrant if necessary
                int xFactor = hashMapKey % QuadrantSystem.QuadrantMultiplier;

                float leftBorder = xFactor * QuadrantSystem.QuadrantCellSize;
                if (translation.Value.x - leftBorder < GameEngine.AsteroidRadius)
                {
                    if (CheckInQuadrant(ref entity, translation, collisionType, hashMapKey - 1))
                        return;
                }

                float rightBorder = (xFactor + 1) * QuadrantSystem.QuadrantCellSize;
                if (translation.Value.x - leftBorder < GameEngine.AsteroidRadius)
                {
                    if (CheckInQuadrant(ref entity, translation, collisionType, hashMapKey + 1))
                        return;
                }
            });
        }

        bool CheckInQuadrant(ref Entity entity, Translation translation, CollisionTypeData collisionType, int hashMapKey)
        {
            // quadrants may be empty (or precisely speaking non existent in case when no entities are within the quadrant area)
            if(!QuadrantSystem.MultiHashMap.TryGetFirstValue(
                hashMapKey,
                out QuadrantData quadrantData,
                out NativeMultiHashMapIterator<int> nativeMultiHashMapIterator))
            {
                return false;
            }

            CollisionTypeEnum typeFirst = collisionType.CollisionObjectType;

            do
            {
                // cycling through all the values
                if (entity.Index < quadrantData.Entity.Index && // to not duplicate checks as well as to avoid checking with yourself
                    // this is fast, I tried to use my own version with cheaper sqrt approximation, 
                    // but ECS is so powerful that it is just a waste of code to be honest
                    math.distance(
                        new float2(translation.Value.x, translation.Value.y),
                        new float2(quadrantData.EntityPosition.x, quadrantData.EntityPosition.y)
                    ) < GameEngine.AsteroidRadius2)
                {
                    CollisionTypeEnum typeSecond = quadrantData.CollisionTypeEnum;

                    if (typeFirst == CollisionTypeEnum.Player)
                    {
                        if (typeSecond == CollisionTypeEnum.Asteroid)
                        {
                            // mark player as destroyed
                            PostUpdateCommands.AddComponent(entity, new DeadData());
                            GameEngine.DidPlayerDieThisFrame = true;
                            PostUpdateCommands.RemoveComponent<RenderMesh>(entity);

                            DestroyAsteroid(ref quadrantData.Entity);

                            return true;
                        }
                    }
                    else if (typeFirst == CollisionTypeEnum.Laser)
                    {
                        if (typeSecond == CollisionTypeEnum.Asteroid)
                        {
                            GameEngine.PlayerScore++;
                            PostUpdateCommands.DestroyEntity(entity);
                            DestroyAsteroid(ref quadrantData.Entity);

                            return true;
                        }
                    }
                    else if (typeFirst == CollisionTypeEnum.Asteroid)
                    {
                        DestroyAsteroid(ref entity);

                        if (typeSecond == CollisionTypeEnum.Asteroid)
                        {
                            DestroyAsteroid(ref quadrantData.Entity);
                        }
                        else if (typeSecond == CollisionTypeEnum.Laser)
                        {
                            GameEngine.PlayerScore++;
                            PostUpdateCommands.DestroyEntity(quadrantData.Entity);
                        }
                        else if (typeSecond == CollisionTypeEnum.Player)
                        {
                            // mark player as destroyed
                            PostUpdateCommands.AddComponent(quadrantData.Entity, new DeadData());
                            GameEngine.DidPlayerDieThisFrame = true;
                            PostUpdateCommands.RemoveComponent<RenderMesh>(quadrantData.Entity);
                        }

                        return true;
                    }
                }
            }
            while (QuadrantSystem.MultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));

            return false;
        }

        void DestroyAsteroid(ref Entity entity)
        {
            // for some reason this is sometimes called twice - it does not break the game as ECS filters out duplicated commands
            // but it is hard to me to even understand where this problem comes from to begin with
            PostUpdateCommands.AddComponent(entity, new TimeToRespawn() { Time = 1f });
            PostUpdateCommands.RemoveComponent<RenderMesh>(entity);
        }
    }
}