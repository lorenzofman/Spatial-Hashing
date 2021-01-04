using Unity.Collections;
using Unity.Mathematics;

// Todo: Create a capsule for checking (for more precision)
public static class GridIntersection
{
    public static void Circle(Circle circle, NativeList<uint2> nodes)
    {
        int2 bottomLeft = (int2) math.floor(circle.center - new float2(circle.radius, circle.radius));
        int2 topRight = (int2) math.ceil(circle.center + new float2(circle.radius, circle.radius));

        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                if (x < 0 || y < 0)
                {
                    continue;
                }

                if (circle.IntersectSquare(new float2(x, y), new float2(x + 1, y + 1)))
                {
                    nodes.Add(new uint2((uint)x, (uint)y));
                }
            }   
        }
    }

    /// <summary>
    /// https://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection
    /// </summary>
    private static bool IntersectSquare(this Circle circle, float2 bottomLeft, float2 topRight)
    {
        return math.distancesq(circle.center, math.clamp(circle.center, bottomLeft, topRight)) < circle.radius * circle.radius;
    }
}