using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct RoadSegment : IComponentData
{
    public float3 initial;
    public float3 final;
}
