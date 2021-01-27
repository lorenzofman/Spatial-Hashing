using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

public class BezierGeneratorSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem entityCommandBuffer;
    private EntityArchetype roadSegmentArchitecture;

    protected override void OnCreate()
    {
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        roadSegmentArchitecture = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(typeof(RoadSegment));
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        EntityArchetype roadArch = roadSegmentArchitecture;
        EntityCommandBuffer.ParallelWriter ecb = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();
        Entities.ForEach((Entity entity, int entityInQueryIndex, in CubicBezier cubicBezier) =>
        {
            // Todo: IJobParallelFor
            float t = 1.0f / cubicBezier.segments;
            float3 p1 = cubicBezier.p0;
            for (int i = 1; i <= cubicBezier.segments; i++)
            {
                float3 p2 = Evaluate(cubicBezier, t * i);
                Entity e = ecb.CreateEntity(entityInQueryIndex, roadArch);
                ecb.SetComponent(entityInQueryIndex, e, new RoadSegment
                {
                    initial = p1,
                    final = p2
                });
                p1 = p2;
            }
            ecb.DestroyEntity(entityInQueryIndex, entity);
        }).ScheduleParallel(Dependency).Complete(); // Do not schedule parallel. It will operate on only one element any
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 Evaluate(CubicBezier curve, float t)
    {
        float u = 1 - t;
        float a = u * u * u;
        float b = u * u * t * 3;
        float c = u * t * t * 3;
        float d = t * t * t;

        return a * curve.p0 + b * curve.p1 + c * curve.p2 + d * curve.p3;
    }
}
