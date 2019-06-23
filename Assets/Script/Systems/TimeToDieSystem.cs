using Assets.Script.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Script.Systems
{
    /// <summary>
    /// Updates all TimeToDie data components of all entities in the World.
    /// Additionally, destroys all entities that have their TimeToDie value lower or equal zero.
    /// </summary>
    [UpdateInGroup(typeof(UpdateGroup1))]
    class TimeToDieSystem : JobComponentSystem
    {
        EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        EntityQuery _query;

        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var entityQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(TimeToDie) }
            };

            _query = GetEntityQuery(entityQueryDesc);

            base.OnCreate();
        }

        [BurstCompile]
        struct TimeToDieJob : IJobForEachWithEntity<TimeToDie>
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
            var job = new TimeToDieJob
            {
                DeltaTime = Time.deltaTime,
                EntityCommandBuffer = _commandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };

            JobHandle jobHandle = job.Schedule(_query, inputDeps);
            jobHandle.Complete();
            return jobHandle;
        }
    }
}
