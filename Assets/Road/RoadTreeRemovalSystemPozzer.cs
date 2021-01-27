using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

// ReSharper disable ForCanBeConvertedToForeach - Native Collections do not implement IEnumerable

// ReSharper disable AccessToDisposedClosure - Disposes only happens after job is completed but Rider fails to see that

public class RoadTreeRemovalSystemPozzer : SystemBase
{
    private EntityQuery roadQuery;

    private bool cacheHashTable;
    
    public bool Active { get; set; }

    public bool CacheHashTable
    {
        get => cacheHashTable;
        set
        {
            if (value)
            {
                regenerateBuffersAsPersistent = true;
            }

            cacheHashTable = value;
        }
    }

    private bool hashTableCreated;
    private bool regenerateBuffersAsPersistent;

    private readonly Stopwatch generateHashtableWatch = new Stopwatch();
    private readonly Stopwatch removeTreesWatch = new Stopwatch();
    

    private NativeArray<TreeHashNode> hashTable;

    private NativeArray<int> used;
    private NativeArray<int> initial;
    private NativeArray<int> final;

    private static int GridWidth;
    private static int GridHeight;
    private EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    protected override void OnCreate()
    {
        roadQuery = GetEntityQuery(ComponentType.ReadOnly<RoadSegment>());
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        hashTable = new NativeArray<TreeHashNode>(Program.Env.treeCount, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        if (roadQuery.IsEmpty || !Active)
        {
            return;
        }
        
        if (Program.Env.treeCount != hashTable.Length)
        {
            hashTable.Dispose();
            hashTable = new NativeArray<TreeHashNode>(Program.Env.treeCount, Allocator.Persistent);
            hashTableCreated = false;
        }

        Environment env = Program.Env;
        float inverseGridSize = 1 / env.gridSize;

        removeTreesWatch.Restart();

        EntityCommandBuffer.ParallelWriter ecb = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();

        JobHandle removePreviousTags = RemovePreviousTags(ecb);

        GridWidth = (int) math.ceil(Environment.TerrainBoundaries.Size.x / env.gridSize);
        GridHeight =
            (int) math.ceil(Environment.TerrainBoundaries.Size.z / env.gridSize);
        int arraySize = GridWidth * GridHeight;

        if (!CacheHashTable || !hashTableCreated)
        {
            DisposeIfCreated(used);
            DisposeIfCreated(initial);
            DisposeIfCreated(final);

            used = new NativeArray<int>(arraySize, Allocator.TempJob);
            initial = new NativeArray<int>(arraySize, Allocator.TempJob);
            final = new NativeArray<int>(arraySize, Allocator.TempJob);
            GenerateHashTable(used, initial, final, inverseGridSize, env);
        }
        else if (regenerateBuffersAsPersistent) /* Regeneration must occur when variable changes*/
        {
            regenerateBuffersAsPersistent = false;
            DisposeIfCreated(used);
            DisposeIfCreated(initial);
            DisposeIfCreated(final);
            used = new NativeArray<int>(arraySize, Allocator.Persistent);
            initial = new NativeArray<int>(arraySize, Allocator.Persistent);
            final = new NativeArray<int>(arraySize, Allocator.Persistent);
            GenerateHashTable(used, initial, final, inverseGridSize, env);
        }

        removePreviousTags.Complete();


        CutTrees(GridHeight, ecb, used, initial, env);

        removeTreesWatch.Stop();

        Debug.Log($"Trees removed in: {removeTreesWatch.ElapsedMilliseconds} ms");
        InterfaceLogger.Logs.Enqueue($"Trees removed in: {removeTreesWatch.ElapsedMilliseconds} ms");
    }

    protected override void OnDestroy()
    {
        hashTable.Dispose();
        base.OnDestroy();
    }

    private static void DisposeIfCreated<T>(NativeArray<T> arr) where T : struct
    {
        if (arr.IsCreated)
        {
            arr.Dispose();
        }
    }

    private void CutTrees(int gridHeight, EntityCommandBuffer.ParallelWriter ecb, NativeArray<int> used,
        NativeArray<int> initial, Environment env)
    {
        /* Burst requires conversion to locals */

        NativeArray<TreeHashNode> hashTable = this.hashTable;
        
        Entities.ForEach((Entity entity, int entityInQueryIndex, in RoadSegment roadSegment) =>
            {
                NativeList<uint2> nodes = new NativeList<uint2>(Allocator.Temp);

                float3 mid = (roadSegment.initial + roadSegment.final) / 2;
                float length = math.distance(roadSegment.initial, roadSegment.final) / 2;

                float2 center = (mid.xz - Environment.TerrainBoundaries.Min.xz) /
                                env.gridSize;
                float radius = (length + env.roadWidth) / env.gridSize;

                Circle influenceZone = new Circle(center, radius);

                GridIntersection.Circle(influenceZone, nodes);

                for (int i = 0; i < nodes.Length; i++)
                {
                    uint2 idx = nodes[i];
                    int sIdx = (int) idx.x * gridHeight + (int) idx.y;

                    if (sIdx < 0 || sIdx >= used.Length)
                    {
                        continue;
                    }

                    RemoveTreeFromSegment(used, initial, hashTable, sIdx, roadSegment, ecb, env);
                }

                ecb.DestroyEntity(entityInQueryIndex, entity);
                nodes.Dispose();
            })
            .WithReadOnly(used)
            .WithReadOnly(initial)
            .WithReadOnly(hashTable)
            .ScheduleParallel(Dependency).Complete();
    }

    private void GenerateHashTable(NativeArray<int> used, NativeArray<int> initial, NativeArray<int> final, float inverseGridSize, Environment env)
    {
        Profiler.BeginSample("Hash Table Generation");

        Profiler.BeginSample("Allocate");

        generateHashtableWatch.Restart();
        NativeArray<Translation> translations =
            new NativeArray<Translation>(env.treeCount, Allocator.TempJob);
        NativeArray<Entity> entities = new NativeArray<Entity>(env.treeCount, Allocator.TempJob);
        NativeArray<int> objectIndices = new NativeArray<int>(entities.Length, Allocator.TempJob);

        Profiler.EndSample();

        Profiler.BeginSample("Fetch entities");

        Entities.WithAll<TreeTag>().ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
        {
            entities[entityInQueryIndex] = entity;
            translations[entityInQueryIndex] = translation;
        }).ScheduleParallel(Dependency).Complete();

        Profiler.EndSample();

        Profiler.BeginSample("Calculate objects hashes");

        for (int e = 0; e < entities.Length; e++)
        {
            int index = HashFunction(translations[e].Value, inverseGridSize);
            objectIndices[e] = index;
            used[index]++;
        }

        Profiler.EndSample();

        Profiler.BeginSample("Accumulator");

        for (int e = 0, accumulator = 0; e < used.Length; e++)
        {
            initial[e] = accumulator;
            accumulator += used[e];
            final[e] = accumulator;
        }

        Profiler.EndSample();

        Profiler.BeginSample("Create nodes");

        for (int e = 0; e < entities.Length; e++)
        {
            hashTable[final[objectIndices[e]] - 1] = new TreeHashNode
            {
                entity = entities[e],
                position = translations[e].Value
            };
            final[objectIndices[e]]--;
        }

        Profiler.EndSample();

        Profiler.BeginSample("Dispose");

        objectIndices.Dispose();
        translations.Dispose();
        entities.Dispose();
        generateHashtableWatch.Stop();
        InterfaceLogger.Logs.Enqueue($"Generate hashtable in: {generateHashtableWatch.ElapsedMilliseconds} ms");
        Debug.Log($"Generate hashtable in: {generateHashtableWatch.ElapsedMilliseconds} ms");
        Profiler.EndSample();
        Profiler.EndSample();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RemoveTreeFromSegment(
        [ReadOnly] NativeArray<int> used,
        [ReadOnly] NativeArray<int> initial,
        [ReadOnly] NativeArray<TreeHashNode> hashTable,
        int idx, RoadSegment roadSegment, EntityCommandBuffer.ParallelWriter ecb, Environment env)
    {
        if (used[idx] == 0)
        {
            return;
        }

        for (int i = initial[idx]; i < initial[idx] + used[idx]; i++)
        {
            TreeHashNode treeNode = hashTable[i];
            if (Distance.FromPointToLineSegmentSquared(treeNode.position, roadSegment.initial, roadSegment.final) <
                env.roadWidth * env.roadWidth)
            {
                ecb.AddComponent<TreeIntersectsRoadTag>(1, treeNode.entity);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HashFunction(float3 position, float inverseGridSize)
    {
        int x = (int) ((position.x - Environment.TerrainBoundaries.Min.x) * inverseGridSize);
        int z = (int) ((position.z - Environment.TerrainBoundaries.Min.z) * inverseGridSize);
        return x * GridHeight + z;
    }
}