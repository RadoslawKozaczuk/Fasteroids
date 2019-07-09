using Assets.Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    [UpdateInGroup(typeof(UpdateGroup1))]
    class MoveSystem : JobComponentSystem
    {
        // IJobForEach does not expose entity and job index handle but here they are not needed anyway
        [BurstCompile]
        struct MoveJob : IJobForEach<MoveSpeedData, Translation, Rotation>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute(
                [ReadOnly] ref MoveSpeedData moveSpeed,
                ref Translation translation,
                ref Rotation rotation)
            {
                translation.Value.x += moveSpeed.DirectionX * moveSpeed.MoveSpeed * DeltaTime;
                translation.Value.y += moveSpeed.DirectionY * moveSpeed.MoveSpeed * DeltaTime;

                // this for sure can be done in a more elegant way
                quaternion newRotation = math.mul(rotation.Value, quaternion.RotateX(moveSpeed.RotationSpeed.x * DeltaTime));
                newRotation = math.mul(newRotation, quaternion.RotateY(moveSpeed.RotationSpeed.y * DeltaTime));
                newRotation = math.mul(newRotation, quaternion.RotateZ(moveSpeed.RotationSpeed.z * DeltaTime));
                rotation.Value = newRotation;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            MoveJob renderJob = new MoveJob { DeltaTime = Time.deltaTime };
            return renderJob.Schedule(this, inputDeps);
        }
    }
}
