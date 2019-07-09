using Assets.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    [UpdateInGroup(typeof(UpdateGroup1))]
    public class PlayerSpaceshipSystem : ComponentSystem
    {
        public ComponentDataFromEntity<DeadData> Dead;
        EntityQuery _query;

        protected override void OnCreate()
        {
            // operate only on entities that have these three components
            _query = GetEntityQuery(
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<SpaceshipData>());
        }

        protected override void OnUpdate()
        {
            Dead = GetComponentDataFromEntity<DeadData>();

            Entities.With(_query).ForEach((
                Entity entity,
                ref Translation translation,
                ref Rotation rotation,
                ref SpaceshipData spaceshipData) =>
            {
                if (Dead.Exists(entity))
                    return;

                // this values are updated in the method below
                float3 playerPos = translation.Value;

                UpdatePositionAndRotation(ref entity, ref playerPos, ref rotation, out float3 positionChange);

                // apply the changes to the game object
                GameEngine.SpaceshipInstance.transform.position = new Vector3(playerPos.x, playerPos.y, playerPos.z);
                GameEngine.SpaceshipInstance.transform.rotation = rotation.Value;

                spaceshipData.TimeToFireLaser -= Time.deltaTime;
                if (spaceshipData.TimeToFireLaser < 0)
                {
                    CreateNewLaserBeam(playerPos, rotation.Value);
                    spaceshipData.TimeToFireLaser = GameEngine.LaserFireFrequency;
                }

                CameraFollow(playerPos);

                RotateSkybox(new Vector3(-positionChange.y * 1.5f, positionChange.x * 1.5f)); // skybox must rotate pretty fast so the player can see it
            });
        }

        void UpdatePositionAndRotation(ref Entity entity, ref float3 position, ref Rotation rotation, out float3 positionChange)
        {
            bool movingBackwards = false;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                // forward vector points backwards because the spaceship is by default rotate by 180 degrees on the Z axis
                float3 forwardVector = math.mul(rotation.Value, new float3(0, -1, 0));
                positionChange = new float3(forwardVector * Time.deltaTime * GameEngine.PlayerSpeed);
                position += positionChange;
                PostUpdateCommands.SetComponent(entity, new Translation { Value = position });
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                float3 backwardVector = math.mul(rotation.Value, new float3(0, 1, 0));
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
                float rotationFactor = movingBackwards ? GameEngine.PlayerRotationFactor : -GameEngine.PlayerRotationFactor;
                rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(rotationFactor * Time.deltaTime));
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                float rotationFactor = movingBackwards ? -GameEngine.PlayerRotationFactor : GameEngine.PlayerRotationFactor;
                rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(rotationFactor * Time.deltaTime));
            }
        }

        /// <summary>
        /// Needs player rotation to calculate forward vector.
        /// </summary>
        void CreateNewLaserBeam(float3 playerPosition, quaternion playerRotation)
        {
            // forward vector points backwards because the spaceship is by default rotate by 180 degrees on the Z axis
            float3 forwardVector = math.mul(playerRotation, new float3(0, -1, 0));
            Entity entity = PostUpdateCommands.CreateEntity(GameEngine.LaserBeamArchetype);

            PostUpdateCommands.SetSharedComponent(
                entity,
                new RenderMesh
                {
                    mesh = GameEngine.Instance.QuadMesh,
                    material = GameEngine.Instance.LaserBeamMaterial
                });

            PostUpdateCommands.SetComponent(
                entity,
                new Translation { Value = playerPosition + forwardVector / 2 }); // spawn it a bit at the front of player

            PostUpdateCommands.SetComponent(entity, new Scale { Value = 0.3f });

            PostUpdateCommands.SetComponent(
                entity,
                new MoveSpeedData
                {
                    DirectionX = forwardVector.x,
                    DirectionY = forwardVector.y,
                    MoveSpeed = GameEngine.LaserSpeed
                });

            PostUpdateCommands.SetComponent(
                entity,
                new CollisionTypeData { CollisionObjectType = CollisionType.Laser });

            PostUpdateCommands.SetComponent(entity, new TimeToDieData { Time = GameEngine.LaserLiveLength });
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
}