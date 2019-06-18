using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class PlayerSpaceshipSystem : ComponentSystem
{
    EndSimulationEntityCommandBufferSystem _commandBufferSystem;
    EntityQuery _query;

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        // operate only on entities that have these three components
        _query = GetEntityQuery(
            ComponentType.ReadWrite<Translation>(), 
            ComponentType.ReadWrite<Rotation>(),
            ComponentType.ReadWrite<GameEngine.SpaceshipData>());
    }

    protected override void OnUpdate()
    {
        Entities.With(_query).ForEach((
            Entity entity, 
            ref Translation translation, 
            ref Rotation rotation, 
            ref GameEngine.SpaceshipData ssData) =>
        {
            EntityCommandBuffer entityCommandBuffer = _commandBufferSystem.CreateCommandBuffer();

            // this values are updated in the method below
            float3 playerPos = translation.Value;
            quaternion playerRot = rotation.Value;

            UpdatePositionAndRotation(ref entity, ref playerPos, ref playerRot);

            float timeToShoot = ssData.TimeToFireLaser;
            timeToShoot -= Time.deltaTime;

            if (timeToShoot < 0)
            {
                CreateNewLaserBeam(entityCommandBuffer, playerPos, playerRot);
                timeToShoot = 0.5f;
            }

            // put it back to the entity's component
            entityCommandBuffer.SetComponent(entity, new GameEngine.SpaceshipData() { TimeToFireLaser = timeToShoot });
        });
    }

    void UpdatePositionAndRotation(ref Entity entity, ref float3 position, ref quaternion rotation)
    {
        bool movingBackwards = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            float3 forwardVector = math.mul(rotation, new float3(0, 1, 0));
            position += forwardVector * Time.deltaTime * GameEngine.PlayerSpeed;
            PostUpdateCommands.SetComponent(entity, new Translation { Value = position });
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            float3 backwardVector = math.mul(rotation, new float3(0, -1, 0));
            position += backwardVector * Time.deltaTime * GameEngine.PlayerSpeed;
            PostUpdateCommands.SetComponent(entity, new Translation { Value = position });
            movingBackwards = true;
        }

        // when moving backwards rotation is reversed to make it more natural
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            float rotationFactor = movingBackwards ? -GameEngine.PlayerRotationFactor : GameEngine.PlayerRotationFactor;
            rotation = math.mul(rotation, quaternion.RotateZ(rotationFactor * Time.deltaTime));
            PostUpdateCommands.SetComponent(entity, new Rotation { Value = rotation });
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            float rotationFactor = movingBackwards ? GameEngine.PlayerRotationFactor : -GameEngine.PlayerRotationFactor;
            rotation = math.mul(rotation, quaternion.RotateZ(rotationFactor * Time.deltaTime));
            PostUpdateCommands.SetComponent(entity, new Rotation { Value = rotation });
        }
    }

    /// <summary>
    /// Needs player rotation to calculate forward vector.
    /// </summary>
    void CreateNewLaserBeam(EntityCommandBuffer entityCommandBuffer, float3 playerPosition, quaternion playerRotation)
    {
        float3 forwardVector = math.mul(playerRotation, new float3(0, 1, 0));
        Entity entity = entityCommandBuffer.CreateEntity(GameEngine.LaserBeamArchetype);

        entityCommandBuffer.SetSharedComponent(
            entity,
            new RenderMesh
            {
                mesh = GameEngine.Instance.Mesh,
                material = GameEngine.Instance.LaserBeamMaterial
            });

        entityCommandBuffer.SetComponent(
            entity,
            new Translation { Value = playerPosition + forwardVector / 2 }); // spawn it a bit at the front of player

        entityCommandBuffer.SetComponent(entity, new Scale { Value = 0.3f });

        entityCommandBuffer.SetComponent(
            entity,
            new GameEngine.MoveSpeed
            {
                DirectionX = forwardVector.x,
                DirectionY = forwardVector.y,
                Speed = 1.25f
            });

        entityCommandBuffer.SetComponent(
            entity,
            new GameEngine.CollisionTypeData { CollisionObjectType = GameEngine.CollisionTypeEnum.Laser });
    }
}
