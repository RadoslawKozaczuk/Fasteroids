using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static GameEngine;

public class PlayerInputSystem : ComponentSystem
{
    EntityQuery _query;

    protected override void OnCreate()
    {
        // operate only on entities that have these three components
        _query = GetEntityQuery(
            ComponentType.ReadOnly<Spaceship>(), 
            ComponentType.ReadWrite<Rotation>(), 
            ComponentType.ReadWrite<Translation>());
    }

    protected override void OnUpdate()
    {
        Entities.With(_query).ForEach((Entity entity, ref Translation translation, ref Rotation rotation) =>
        {
            bool movingBackwards = false;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                float3 forwardVector = math.mul(rotation.Value, new float3(0, 1, 0));
                float3 newTranslation = translation.Value + forwardVector * Time.deltaTime * PlayerSpeed;
                PostUpdateCommands.SetComponent(entity, new Translation { Value = newTranslation });
            }
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                float3 forwardVector = math.mul(rotation.Value, new float3(0, -1, 0));
                float3 newTranslation = translation.Value + forwardVector * Time.deltaTime * PlayerSpeed;
                PostUpdateCommands.SetComponent(entity, new Translation { Value = newTranslation });
                movingBackwards = true;
            }

            // when moving backwards rotation is reversed to make it more natural
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                float rotationFactor = movingBackwards ? -PlayerRotationFactor : PlayerRotationFactor;
                quaternion newRotation = math.mul(rotation.Value, quaternion.RotateZ(rotationFactor * Time.deltaTime));
                PostUpdateCommands.SetComponent(entity, new Rotation { Value = newRotation });
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                float rotationFactor = movingBackwards ? PlayerRotationFactor : -PlayerRotationFactor;
                quaternion newRotation = math.mul(rotation.Value, quaternion.RotateZ(rotationFactor * Time.deltaTime));
                PostUpdateCommands.SetComponent(entity, new Rotation { Value = newRotation });
            }
        });
    }
}
