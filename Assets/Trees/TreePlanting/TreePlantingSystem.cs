using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class TreePlantingSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;
    private int plantedTrees;
    protected override void OnCreate()
    {
        endSimulationEntityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = endSimulationEntityCommandBuffer.CreateCommandBuffer();
        Random random = new Random();
        random.InitState();

        Environment env = Program.Env;

        TreeModel treeModel = FetchTreePrefab();

        if (plantedTrees == Program.Env.treeCount)
        {
            return;
        }
        
        UnityEngine.Debug.Log($"Planting {env.treeCount} trees");

        Entities.WithAll<TreeTag>().ForEach((Entity e) =>
        {
            ecb.DestroyEntity(e);
        }).Run();

        
        for (int i = 0; i < env.treeCount; i++)
        {
            float3 p = RandomPositionInBoundaries(Environment.TerrainBoundaries, ref random);
            PlantTree(ecb, p, treeModel.model);
        }

        plantedTrees = Program.Env.treeCount;


    }

    /// <summary>
    /// Assumes that there is only one model
    /// </summary>
    /// <returns></returns>
    private TreeModel FetchTreePrefab()
    {
        NativeArray<TreeModel> trees = new NativeArray<TreeModel>(1, Allocator.Temp);
        Entities.ForEach((Entity entity, int entityInQueryIndex, in TreeModel model) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            trees[entityInQueryIndex] = model;
        }).Run();
        
        TreeModel t = trees[0];
        trees.Dispose();
        return t;
    }

    private static void PlantTree(EntityCommandBuffer ecb, float3 position, Entity model)
    {
        Entity instantiated = ecb.Instantiate(model);
        ecb.SetComponent(instantiated, new Translation
        {
            Value = position
        });
    }

    private static float3 RandomPositionInBoundaries(AABB boundaries, ref Random gen)
    {
        return gen.NextFloat3() * (boundaries.Max - boundaries.Min) + boundaries.Min;
    }
}