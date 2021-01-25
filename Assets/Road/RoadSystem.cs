using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

// ReSharper disable ForCanBeConvertedToForeach - Native Collections do not implement IEnumerable

// ReSharper disable AccessToDisposedClosure - Disposes only happens after job is completed but Rider fails to see that

public class RoadTreeRemovalSystem : SystemBase
{
    private EntityQuery roadQuery;

    public bool CacheHashTable { get; set; } = true;

    private bool hashTableCreated;

    private readonly Stopwatch generateHashtableWatch = new Stopwatch();
    private readonly Stopwatch removeTreesWatch = new Stopwatch();
    
    private NativeMultiHashMap<uint2, TreeHashNode> hashTable;

    private EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    protected override void OnCreate()
    {
        roadQuery = GetEntityQuery(ComponentType.ReadOnly<RoadSegment>());
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        // hashTable = new NativeArray<TreeHashNode>(TreePlantingInformation.TreeCount, Allocator.Persistent);
        Debug.Log("");
        hashTable = new NativeMultiHashMap<uint2, TreeHashNode>(Program.Env.treeCount, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        if (roadQuery.IsEmpty)
        {
            return;
        }

        if (Program.Env.treeCount != hashTable.Capacity)
        {
            hashTable.Dispose();
            hashTable = new NativeMultiHashMap<uint2, TreeHashNode>(Program.Env.treeCount, Allocator.Persistent);
            hashTableCreated = false;
        }

        Environment env = Program.Env;
        
        int sortCount = UnityEngine.Time.frameCount * 2; 
        // % 0 => Restore trees 
        // % 1 => Cut trees
        removeTreesWatch.Restart();
        
        EntityCommandBuffer.ParallelWriter ecb = entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();

        JobHandle removePreviousTags = RemovePreviousTags(ecb, sortCount);
        
        if (!CacheHashTable || !hashTableCreated)
        {
            hashTable.Clear();
            GenerateHashTable(1 / Program.Env.gridSize);
            hashTableCreated = true;
        }
        
        removePreviousTags.Complete();

        CutTrees(ecb, sortCount + 1, env);
        
        removeTreesWatch.Stop();
        
        InterfaceLogger.Logs.Enqueue($"Trees removed in: {removeTreesWatch.ElapsedMilliseconds} ms");
    }

    protected override void OnDestroy()
    {
        hashTable.Dispose();
        base.OnDestroy();
    }

    private void CutTrees(EntityCommandBuffer.ParallelWriter ecb, int sortKey, Environment env)
    {
        /* Burst requires conversion to locals */

        NativeMultiHashMap<uint2, TreeHashNode> hashTable = this.hashTable;
        
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
                    RemoveTreeFromSegment(hashTable, nodes[i], roadSegment, ecb, sortKey, env);
                }

                ecb.DestroyEntity(entityInQueryIndex, entity);
                nodes.Dispose();
            })
            .WithReadOnly(hashTable)
            .ScheduleParallel(Dependency).Complete();
    }
    
    private void GenerateHashTable(float inverseGridSize)
    {
        generateHashtableWatch.Restart();
        
        NativeMultiHashMap<uint2, TreeHashNode>.ParallelWriter hashTableParallel = hashTable.AsParallelWriter();
        Entities.WithAll<TreeTag>().ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
        {
            uint2 node = GridCell(translation.Value, inverseGridSize);
            hashTableParallel.Add(node, new TreeHashNode
            {
                entity = entity,
                position = translation.Value
            });
        }).ScheduleParallel(Dependency).Complete();

        
        generateHashtableWatch.Stop();
        InterfaceLogger.Logs.Enqueue($"Generate hashtable in: {generateHashtableWatch.ElapsedMilliseconds} ms");
        Debug.Log($"Generate hashtable in: {generateHashtableWatch.ElapsedMilliseconds} ms");
    }
    
    private JobHandle RemovePreviousTags(EntityCommandBuffer.ParallelWriter ecb, int sortKey)
    {
        JobHandle removePreviousTags = Entities.WithAll<DisableRendering>().ForEach(
            (Entity entity) =>
            {
                ecb.RemoveComponent<TreeIntersectsRoadTag>(sortKey, entity);
                ecb.RemoveComponent<DisableRendering>(sortKey, entity);
            }).ScheduleParallel(Dependency);
        return removePreviousTags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RemoveTreeFromSegment([ReadOnly] NativeMultiHashMap<uint2, TreeHashNode> hashTable,
        uint2 idx, RoadSegment roadSegment, EntityCommandBuffer.ParallelWriter ecb, int sortKey, Environment env)
    {
        NativeMultiHashMap<uint2, TreeHashNode>.Enumerator enumerator = hashTable.GetValuesForKey(idx);
        while (enumerator.MoveNext())
        {
            if (Distance.FromPointToLineSegmentSquared(enumerator.Current.position, roadSegment.initial, roadSegment.final) < 
                env.roadWidth * env.roadWidth)
            {
                ecb.AddComponent<TreeIntersectsRoadTag>(sortKey + 1, enumerator.Current.entity);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint2 GridCell(float3 position, float inverseGridSize)
    {
        uint x = (uint) ((position.x - Environment.TerrainBoundaries.Min.x) * inverseGridSize);
        uint z = (uint) ((position.z - Environment.TerrainBoundaries.Min.z) * inverseGridSize);
        return new uint2(x, z);
    }
}