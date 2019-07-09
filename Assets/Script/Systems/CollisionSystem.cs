using Assets.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
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
                .WithNone<TimeToRespawnData>()
                .ForEach((Entity entity, ref Translation translation, ref CollisionTypeData collisionType) =>
                {
                    int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);

                    // check in this quadrant
                    if (CheckInQuadrant(ref entity, translation, collisionType, hashMapKey))
                        return;

                    // this will be used for the diagonal checks
                    bool left = false, right = false, top = false, bottom = false;

                    // check in neighboring quadrant if necessary
                    QuadrantSystem.RetrieveComponentsFromHashMapKey(hashMapKey, out int xComponent, out int yComponent);

                    float leftBorder = xComponent * QuadrantSystem.QuadrantCellSize;
                    if (Mathf.Abs(translation.Value.x - leftBorder) < GetEntityRadius(collisionType.CollisionObjectType))
                    {
                        left = true;
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent - 1 + yComponent * QuadrantSystem.QuadrantMultiplier))
                            return;
                    }

                    float rightBorder = (xComponent + 1) * QuadrantSystem.QuadrantCellSize;
                    if (Mathf.Abs(translation.Value.x - rightBorder) < GetEntityRadius(collisionType.CollisionObjectType))
                    {
                        right = true;
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent + 1 + yComponent * QuadrantSystem.QuadrantMultiplier))
                            return;
                    }

                    float topBorder = (yComponent + 1) * QuadrantSystem.QuadrantCellSize;
                    if (Mathf.Abs(translation.Value.y - topBorder) < GetEntityRadius(collisionType.CollisionObjectType))
                    {
                        top = true;
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent + (yComponent + 1) * QuadrantSystem.QuadrantMultiplier))
                            return;
                    }

                    float bottomBorder = yComponent * QuadrantSystem.QuadrantCellSize;
                    if (Mathf.Abs(translation.Value.y - bottomBorder) < GetEntityRadius(collisionType.CollisionObjectType))
                    {
                        bottom = true;
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent + (yComponent - 1) * QuadrantSystem.QuadrantMultiplier))
                            return;
                    }

                    // check diagonal possibilities
                    if (top && left)
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent - 1 + (yComponent + 1) * QuadrantSystem.QuadrantMultiplier))
                            return;

                    if (top && right)
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent + 1 + (yComponent + 1) * QuadrantSystem.QuadrantMultiplier))
                            return;

                    if (bottom && left)
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent - 1 + (yComponent - 1) * QuadrantSystem.QuadrantMultiplier))
                            return;

                    if (bottom && right)
                        if (CheckInQuadrant(ref entity, translation, collisionType, xComponent + 1 + (yComponent - 1) * QuadrantSystem.QuadrantMultiplier))
                            return;

                    // === DEBUG DRAW ===
                    // draw sphere around entities around the player's 
                    // surprisingly there is no way to draw a circle but sphere will do
                    // is DebugBuild can only be called from the main thread as well as Gizmo.DrawSpheare
                    if (Debug.isDebugBuild && GameEngine.Instance.DrawEntityCollisionBorders)
                        DebugDrawMethods.DebugDrawCircle(translation.Value.x, translation.Value.y, GetEntityRadius(collisionType.CollisionObjectType), Color.red);
                });
        }

        bool CheckInQuadrant(ref Entity entity, Translation translation, CollisionTypeData collisionType, int hashMapKey)
        {
            // quadrants may be empty (or precisely speaking non existent) in case when no entities are within the quadrant area)
            if (!QuadrantSystem.MultiHashMap.TryGetFirstValue(
                hashMapKey,
                out QuadrantData quadrantData,
                out NativeMultiHashMapIterator<int> nativeMultiHashMapIterator))
            {
                return false;
            }

            // cycling through all the values
            do
            {
                CollisionType typeFirst = collisionType.CollisionObjectType;
                CollisionType typeSecond = quadrantData.CollisionTypeEnum;

                float minimumDistance = GetEntityRadius(typeFirst) + GetEntityRadius(typeSecond);

                // to avoid duplicate checks as well as to avoid checking with yourself
                if (entity.Index < quadrantData.Entity.Index &&
                    // this is fast, I tried to use my own version with cheaper sqrt approximation, 
                    // but ECS is so powerful that it is just a waste of code to be honest
                    math.distance(
                        new float2(translation.Value.x, translation.Value.y),
                        new float2(quadrantData.EntityPosition.x, quadrantData.EntityPosition.y)
                    ) < minimumDistance)
                {
                    if (typeFirst == CollisionType.Player)
                    {
                        if (typeSecond == CollisionType.Asteroid)
                        {
                            PostUpdateCommands.DestroyEntity(entity);
                            GameEngine.DidPlayerDieThisFrame = true;
                            DestroyAsteroid(ref quadrantData.Entity);

                            return true;
                        }
                    }
                    else if (typeFirst == CollisionType.Laser)
                    {
                        if (typeSecond == CollisionType.Asteroid)
                        {
                            GameEngine.PlayerScore++;
                            PostUpdateCommands.DestroyEntity(entity);
                            DestroyAsteroid(ref quadrantData.Entity);

                            return true;
                        }
                    }
                    else if (typeFirst == CollisionType.Asteroid)
                    {
                        DestroyAsteroid(ref entity);

                        if (typeSecond == CollisionType.Asteroid)
                        {
                            DestroyAsteroid(ref quadrantData.Entity);
                        }
                        else if (typeSecond == CollisionType.Laser)
                        {
                            GameEngine.PlayerScore++;
                            PostUpdateCommands.DestroyEntity(quadrantData.Entity);
                        }
                        else if (typeSecond == CollisionType.Player)
                        {
                            GameEngine.DidPlayerDieThisFrame = true;
                            PostUpdateCommands.DestroyEntity(quadrantData.Entity);
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
            PostUpdateCommands.AddComponent(entity, new TimeToRespawnData() { Time = 1f });
            PostUpdateCommands.RemoveComponent<RenderMesh>(entity);
        }

        float GetEntityRadius(CollisionType type)
        {
            switch (type)
            {
                case CollisionType.Player:
                    return GameEngine.PlayerRadius;
                case CollisionType.Asteroid:
                    return GameEngine.AsteroidRadius;
                case CollisionType.Laser:
                    return GameEngine.LaserRadius;
                default:
                    throw new System.Exception("Impossible state");
            }
        }
    }
}