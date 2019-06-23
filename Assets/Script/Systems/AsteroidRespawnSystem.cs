using Assets.Script.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static GameEngine;

namespace Assets.Script.Systems
{
    [UpdateInGroup(typeof(UpdateGroup4))]
    class AsteroidRespawnSystem : ComponentSystem
    {
        EntityQuery _playerQuery;
        EntityQuery _respawingQuery;

        protected override void OnCreate()
        {
            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Rotation>(),
                ComponentType.ReadOnly<SpaceshipData>());

            _respawingQuery = GetEntityQuery(ComponentType.ReadOnly<TimeToRespawn>());
        }

        protected override void OnUpdate()
        {
            float3 playerPosition = float3.zero; // initialization

            // player will always be found, he is never destroyed even upon death he is just marked as dead
            Entities.With(_playerQuery).ForEach((Entity entity, ref Translation translation)
                => playerPosition = translation.Value);

            Entities.With(_respawingQuery).ForEach((Entity entity, ref TimeToRespawn timeToRespawn) =>
            {
                timeToRespawn.Time -= Time.deltaTime;

                if (timeToRespawn.Time <= 0)
                {
                    PostUpdateCommands.RemoveComponent(entity, typeof(TimeToRespawn));
     
                    float3 spawnLocation = new float3(FindSpawningLocation(playerPosition), 3f);

                    PostUpdateCommands.AddSharedComponent(
                        entity,
                        new RenderMesh
                        {
                            mesh = Instance.AsteroidMesh,
                            material = Instance.AsteroidMaterial
                        });

                    PostUpdateCommands.SetComponent(entity, new Translation { Value = spawnLocation });
                    PostUpdateCommands.SetComponent(entity, new Scale { Value = AsteroidScale });

                    PostUpdateCommands.SetComponent(
                        entity,
                        new MoveSpeedData
                        {
                            DirectionX = UnityEngine.Random.Range(-1f, 1f),
                            DirectionY = UnityEngine.Random.Range(-1f, 1f),
                            MoveSpeed = UnityEngine.Random.Range(0.05f, 0.2f),
                            RotationSpeed = new float3(
                                    UnityEngine.Random.Range(0f, 1f),
                                    UnityEngine.Random.Range(0f, 1f),
                                    UnityEngine.Random.Range(0f, 1f))
                        });

                    PostUpdateCommands.SetComponent(entity, new Rotation { Value = UnityEngine.Random.rotation });
                }
            });
        }

        float2 FindSpawningLocation(float3 playerPosition)
        {
            float2 spawnLocation;

            // it is not the most mathematically correct solution
            // as the asteroids dispersion will not be even (those that normally would spawn inside the frustum 
            // will spawn right next to the frustum's edge instead)
            spawnLocation.x = UnityEngine.Random.Range(0, GridDimensionFloat) + WorldOffetValue;
            if (spawnLocation.x > playerPosition.x)
            {
                // tried to spawn on the right side of the player
                if (spawnLocation.x - playerPosition.x < FrustumSizeX)
                    spawnLocation.x += FrustumSizeX;
            }
            else
            {
                // left side
                if (playerPosition.x - spawnLocation.x < FrustumSizeX)
                    spawnLocation.x -= FrustumSizeX;
            }

            spawnLocation.y = UnityEngine.Random.Range(0, GridDimensionFloat) + WorldOffetValue;
            if (spawnLocation.y > playerPosition.y)
            {
                // tried to spawn above the player
                if (spawnLocation.y - playerPosition.y < FrustumSizeY)
                    spawnLocation.y += FrustumSizeY;
            }
            else
            {
                // below
                if (playerPosition.y - spawnLocation.y < FrustumSizeY)
                    spawnLocation.y -= FrustumSizeY;
            }

            return spawnLocation;
        }
    }
}