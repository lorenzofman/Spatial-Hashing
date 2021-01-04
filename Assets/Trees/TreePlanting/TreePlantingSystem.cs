using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[GenerateAuthoringComponent]
public class TreePlantingSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb = endSimulationEntityCommandBuffer.CreateCommandBuffer().AsParallelWriter();
        Random random = new Random();
        random.InitState();

        Entities.ForEach((Entity entity, int entityInQueryIndex, in TreePlantingData planting) =>
        {
            UnityEngine.Debug.Log($"Planting {TreePlantingInformation.TreeCount} trees");
            for (int i = 0; i < TreePlantingInformation.TreeCount; i++)
            {
                float3 p = RandomPositionInBoundaries(TreePlantingInformation.TerrainBoundaries, ref random);
                PlantTree(ecb, entityInQueryIndex, p, planting.treeModel);
            }

            ecb.DestroyEntity(entityInQueryIndex, entity);
        }).ScheduleParallel(Dependency).Complete();
    }

    private static void PlantTree(EntityCommandBuffer.ParallelWriter ecb, int sortKey, float3 position, Entity model)
    {
        Entity instantiated = ecb.Instantiate(sortKey, model);
        ecb.SetComponent(sortKey, instantiated, new Translation
        {
            Value = position
        });
    }

    private static float3 RandomPositionInBoundaries(AABB boundaries, ref Random gen)
    {
        return gen.NextFloat3() * (boundaries.Max - boundaries.Min) + boundaries.Min;
    }
}