using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static GameEngine;

class AsteroidRespawnSystem : ComponentSystem
{
    EndSimulationEntityCommandBufferSystem _commandBufferSystem;
    Mesh AsteroidMesh;
    Material AsteroidMaterial;
    private EntityQuery query;

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        query = GetEntityQuery(
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<Spaceship>());
    }

    protected override void OnUpdate()
    {
        float3 playerPosition = float3.zero; // initialization

        // player will always be found, he is never destroyed even upon death he is just marked as dead
        Entities.With(query).ForEach((Entity entity, ref Translation translation)
            => playerPosition = translation.Value);

        Entities.ForEach((Entity entity, ref TimeToRespawn timeToRespawn) =>
        {
            var EntityCommandBuffer = _commandBufferSystem.CreateCommandBuffer();

            float time = timeToRespawn.Time;
            time -= Time.deltaTime;

            if (time <= 0)
            {
                EntityCommandBuffer.DestroyEntity(entity);
                Entity newAsteroid = EntityCommandBuffer.CreateEntity(AsteroidArchetype);

                EntityCommandBuffer.SetSharedComponent(
                    newAsteroid,
                    new RenderMesh
                    {
                        mesh = Instance._mesh,
                        material = Instance._asteroidMaterial
                    });

                EntityCommandBuffer.SetComponent(
                    newAsteroid,
                    new Translation { Value = new float3(FindSpawningLocation(playerPosition), 3f) });

                EntityCommandBuffer.SetComponent(newAsteroid, new Scale { Value = 0.6f });

                EntityCommandBuffer.SetComponent(
                    newAsteroid,
                    new MoveSpeed
                    {
                        DirectionX = UnityEngine.Random.Range(-1f, 1f),
                        DirectionY = UnityEngine.Random.Range(-1f, 1f),
                        Speed = UnityEngine.Random.Range(0.05f, 0.2f)
                    });
            }
            else
            {
                EntityCommandBuffer.SetComponent(entity, new TimeToRespawn() { Time = time });
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
