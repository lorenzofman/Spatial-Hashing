using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

// ReSharper disable ForCanBeConvertedToForeach - Native Collections do not implement IEnumerable

// ReSharper disable AccessToDisposedClosure - Disposes only happens after job is completed but Rider fails to see that

public class RoadTreeRemovalSystem : SystemBase
{
    private EntityQuery roadQuery;
    public bool Active { get; set; }

    private readonly Stopwatch removeTreesWatch = new Stopwatch();
    private EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    protected override void OnCreate()
    {
        roadQuery = GetEntityQuery(ComponentType.ReadOnly<RoadSegment>());
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (roadQuery.IsEmpty || !Active)
        {
            return;
        }
        
        Environment env = Program.Env;

        removeTreesWatch.Restart();

        EntityCommandBuffer.ParallelWriter ecb = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();

        JobHandle removePreviousTags = RemovePreviousTags(ecb);

        removePreviousTags.Complete();

        CutTrees(ecb, env);

        removeTreesWatch.Stop();

        Debug.Log($"Trees removed in: {removeTreesWatch.ElapsedMilliseconds} ms");
        InterfaceLogger.Logs.Enqueue($"Trees removed in: {removeTreesWatch.ElapsedMilliseconds} ms");
    }
    
    private void CutTrees(EntityCommandBuffer.ParallelWriter ecb, Environment env)
    {
        NativeArray<RoadSegment> roadSegments = new NativeArray<RoadSegment>(env.roadSegmentsResolution, Allocator.TempJob);
        Entities.ForEach((Entity entity, int entityInQueryIndex, in RoadSegment roadSegment) =>
        {
            roadSegments[entityInQueryIndex] = roadSegment;
            ecb.DestroyEntity(entityInQueryIndex, entity);
        }).ScheduleParallel(Dependency).Complete();
        
        Entities.ForEach((Entity entity, int entityInQueryIndex, in TreeTag tree, in Translation translation) =>
        {
            for (int i = 0; i < roadSegments.Length; i++)
            {
                if (Distance.FromPointToLineSegmentSquared(translation.Value, roadSegments[i].initial, roadSegments[i].final) <  env.roadWidth * env.roadWidth)
                {
                    ecb.AddComponent<TreeIntersectsRoadTag>(1, entity);
                }
            }
            
        }).ScheduleParallel(Dependency).Complete();
        
        roadSegments.Dispose();
    }

    private JobHandle RemovePreviousTags(EntityCommandBuffer.ParallelWriter ecb)
    {
        JobHandle removePreviousTags = Entities.WithAll<TreeTag, TreeIntersectsRoadTag>().ForEach(
            (Entity entity) =>
            {
                ecb.RemoveComponent<TreeIntersectsRoadTag>(0, entity);
                ecb.RemoveComponent<DisableRendering>(0, entity);
            }).ScheduleParallel(Dependency);
        return removePreviousTags;
    }
}