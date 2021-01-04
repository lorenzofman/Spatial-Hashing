using Unity.Entities;
using Unity.Rendering;

public class TreeIntersectionsSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem entityCommandBuffer;
    
    protected override void OnCreate()
    {
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>(); 
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();
        Entities.WithNone<DisableRendering>().WithAll<TreeIntersectsRoadTag>().ForEach((Entity e, int entityInQueryIndex) =>
        {
            ecb.AddComponent<DisableRendering>(entityInQueryIndex, e);
        }).ScheduleParallel(Dependency).Complete();
    }
}
