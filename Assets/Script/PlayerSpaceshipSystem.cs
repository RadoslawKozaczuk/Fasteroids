using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class PlayerSpaceshipSystem : ComponentSystem
{
    public ComponentDataFromEntity<GameEngine.DeadData> Dead;

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
        Dead = GetComponentDataFromEntity<GameEngine.DeadData>();

        Entities.With(_query).ForEach((
            Entity entity, 
            ref Translation translation, 
            ref Rotation rotation, 
            ref GameEngine.SpaceshipData spaceshipData) =>
        {
            if (Dead.Exists(entity))
                return;

            EntityCommandBuffer entityCommandBuffer = _commandBufferSystem.CreateCommandBuffer();

            // this values are updated in the method below
            float3 playerPos = translation.Value;

            UpdatePositionAndRotation(ref entity, ref playerPos, ref rotation, out float3 positionChange);

            float timeToShoot = spaceshipData.TimeToFireLaser;
            timeToShoot -= Time.deltaTime;

            if (timeToShoot < 0)
            {
                CreateNewLaserBeam(entityCommandBuffer, playerPos, rotation.Value);
                timeToShoot = 0.5f;
            }

            // put it back to the entity's component
            entityCommandBuffer.SetComponent(entity, new GameEngine.SpaceshipData() { TimeToFireLaser = timeToShoot });

            CameraFollow(playerPos);

            RotateSkybox(new Vector3(-positionChange.y / 2, positionChange.x / 2));
        });
    }

    void UpdatePositionAndRotation(ref Entity entity, ref float3 position, ref Rotation rotation, out float3 positionChange)
    {
        bool movingBackwards = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            float3 forwardVector = math.mul(rotation.Value, new float3(0, 1, 0));
            positionChange = new float3(forwardVector * Time.deltaTime * GameEngine.PlayerSpeed);
            position += positionChange;
            PostUpdateCommands.SetComponent(entity, new Translation { Value = position });
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            float3 backwardVector = math.mul(rotation.Value, new float3(0, -1, 0));
            positionChange = new float3(backwardVector * Time.deltaTime * GameEngine.PlayerSpeed);
            position += positionChange;
            PostUpdateCommands.SetComponent(entity, new Translation { Value = position });
            movingBackwards = true;
        }
        else
            positionChange = float3.zero;

        // when moving backwards rotation is reversed to make it more natural
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            float rotationFactor = movingBackwards ? -GameEngine.PlayerRotationFactor : GameEngine.PlayerRotationFactor;
            rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(rotationFactor * Time.deltaTime));
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            float rotationFactor = movingBackwards ? GameEngine.PlayerRotationFactor : -GameEngine.PlayerRotationFactor;
            rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(rotationFactor * Time.deltaTime));
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
                mesh = GameEngine.Instance.QuadMesh,
                material = GameEngine.Instance.LaserBeamMaterial
            });

        entityCommandBuffer.SetComponent(
            entity,
            new Translation { Value = playerPosition + forwardVector / 2 }); // spawn it a bit at the front of player

        entityCommandBuffer.SetComponent(entity, new Scale { Value = 0.3f });

        entityCommandBuffer.SetComponent(
            entity,
            new GameEngine.MoveSpeedData
            {
                DirectionX = forwardVector.x,
                DirectionY = forwardVector.y,
                MoveSpeed = 2.5f
            });

        entityCommandBuffer.SetComponent(
            entity,
            new GameEngine.CollisionTypeData { CollisionObjectType = GameEngine.CollisionTypeEnum.Laser });

        entityCommandBuffer.SetComponent(entity, new GameEngine.TimeToDie { Time = 2f });
    }

    void CameraFollow(float3 playerPosition)
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10);
    }

    void RotateSkybox(Vector3 playerDeltaMove)
    {
        // later on also add the ability to change the sky box with game restart/level change/etc
        // If you change the skybox in playmode, you have to use the DynamicGI.UpdateEnvironment function call to update the ambient probe.
        GameEngine.Instance.SkyboxCamera.transform.Rotate(playerDeltaMove);
    }
}
