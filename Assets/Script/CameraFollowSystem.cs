using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static GameEngine;

public class CameraFollowSystem : ComponentSystem
{
    EntityQuery _query;

    protected override void OnCreate() 
        => _query = GetEntityQuery(
            ComponentType.ReadOnly<Translation>(), 
            ComponentType.ReadOnly<Spaceship>());

    protected override void OnUpdate()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Entities.With(_query).ForEach(
            (Entity entity, ref Translation translation) =>
            {
                float3 playerPosition = translation.Value;
                mainCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, -10);
            });
    }
}
