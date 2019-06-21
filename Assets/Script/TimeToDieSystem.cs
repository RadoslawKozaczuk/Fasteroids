using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using static GameEngine;

namespace Assets.Script
{
    /// <summary>
    /// Updates all TimeToDie data components of all entities in the World.
    /// Additionally, destroys all entities that have their TimeToDie value lower or equal zero.
    /// </summary>
    [BurstCompile]
    class TimeToDieSystem : JobComponentSystem
    {
        EndSimulationEntityCommandBufferSystem _commandBufferSystem;

        protected override void OnCreate() => _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        struct KillJob : IJobForEachWithEntity<TimeToDie>
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent EntityCommandBuffer;
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] Entity entity, [ReadOnly] int index, ref TimeToDie timeToDie)
            {
                timeToDie.Time -= DeltaTime;

                if (timeToDie.Time <= 0)
                    EntityCommandBuffer.DestroyEntity(index, entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var renderJob = new KillJob
            {
                DeltaTime = Time.deltaTime,
                EntityCommandBuffer = _commandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };

            JobHandle jobHandle = renderJob.Schedule(this, inputDeps);
            jobHandle.Complete(); // because killing happen is done in concurrent manner we have to wait until the job is done
            return jobHandle;
        }
    }
}
