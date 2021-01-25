using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public static class Distance
{
    #region Point - Line Segment

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FromPointToLineSegmentSquared(float3 point, float3 x1, float3 x2)
    {
        float3 d = math.normalize(x2 - x1);
        float3 x = x1 + d * Vector3.Dot(point - x1, d);
        float3 closestPoint = ClampToSegment(x, x1, x2);
        return math.distancesq(closestPoint, point);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 ClampToSegment(float3 x, float3 a, float3 b)
    {
        float d = math.distancesq(a, b);
        float da = math.distancesq(x, a);
        float db = math.distancesq(x, b);

        if (da < d && db < d)
        {
            return x;
        }

        return da < db ? a : b;
    }
    
    #endregion

}
