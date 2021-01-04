using Unity.Entities;
using Unity.Mathematics;

public struct CubicBezier : IComponentData
{
    public float3 p0;
    public float3 p1;
    public float3 p2;
    public float3 p3;
    public int segments;
}