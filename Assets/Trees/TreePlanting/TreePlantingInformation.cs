using Unity.Mathematics;

public static class TreePlantingInformation
{
    public const int TreeCount = 800000;
    public const float RoadThreshold = 1f;
    public const float GridSize = 10f;

    public static readonly AABB TerrainBoundaries = new AABB
    {
        Center = new float3(400, 0, 200),
        Extents = new float3(400, 0, 200)
    };

}
