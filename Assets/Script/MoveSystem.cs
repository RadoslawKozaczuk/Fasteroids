using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;
using static GameEngine;

namespace Assets.Script
{
    [BurstCompile]
    class MoveSystem : JobComponentSystem
    {
        struct MoveJob : IJobForEachWithEntity<Translation, MoveSpeed>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] Entity entity, [ReadOnly] int index, ref Translation translation, [ReadOnly] ref MoveSpeed moveSpeed)
            {
                translation.Value.x += moveSpeed.DirectionX * moveSpeed.Speed * DeltaTime;
                translation.Value.y += moveSpeed.DirectionY * moveSpeed.Speed * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            MoveJob renderJob = new MoveJob { DeltaTime = Time.deltaTime };
            JobHandle jobHandle = renderJob.Schedule(this, inputDeps);

            return jobHandle;
        }
    }
}
