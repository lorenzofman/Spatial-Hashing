using Unity.Entities;
using Unity.Rendering;

public class DisableRenderingSystem : SystemBase
{
    private bool render = true;
    private bool update;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;
    
    public void ToggleRendering()
    {
        render = !render;
        update = true;
    }
    
    protected override void OnCreate()
    {
        endSimulationEntityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }
    
    protected override void OnUpdate()
    {
        if (!update)
        {
            return;
        }

        update = false;
        EntityCommandBuffer.ParallelWriter ecb = endSimulationEntityCommandBuffer.CreateCommandBuffer().AsParallelWriter();
        
        if (render)
        {
            Entities.WithNone<TreeIntersectsRoadTag>().ForEach(
                (Entity e, int entityInQueryIndex, in TreeTag _) =>
                {
                    ecb.RemoveComponent<DisableRendering>(entityInQueryIndex, e);
                }).ScheduleParallel(Dependency).Complete();
        }
        else
        {
            Entities.WithNone<DisableRendering>().ForEach((Entity e, int entityInQueryIndex, in TreeTag _) =>
            {
                ecb.AddComponent<DisableRendering>(entityInQueryIndex, e);
            }).ScheduleParallel(Dependency).Complete();
        }
    }
}