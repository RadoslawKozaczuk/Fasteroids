using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts
{
    [BurstCompile]
    class AgentRenderSystemWithJobs : JobComponentSystem
    {
        struct RenderJob : IJobForEachWithEntity<Translation>
        {
            [ReadOnly] public NativeArray<float3> VisiblePositions;

            public void Execute([ReadOnly] Entity entity, [ReadOnly] int index, ref Translation c0)
            {
                c0.Value = VisiblePositions[index];
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            RenderJob renderJob = new RenderJob { VisiblePositions = GameEngine.Positions };
            JobHandle jobHandle = renderJob.Schedule(this, inputDeps);

            return jobHandle;
        }
    }
}
