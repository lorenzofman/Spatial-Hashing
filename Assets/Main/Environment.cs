using Unity.Mathematics;

public struct Environment
{
    public static readonly AABB TerrainBoundaries = new AABB
    {
        Center = new float3(400, 0, 200),
        Extents = new float3(400, 0, 200)
    };
    public int treeCount;
    public float roadWidth;
    public float gridSize;
    public int roadSegmentsResolution;
}