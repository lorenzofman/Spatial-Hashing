using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class GridTest : MonoBehaviour
{
    private const uint GridSize = 16;
    public PaintedNode prefab;
    public Transform pointA;
    public Transform pointB;
    private readonly Dictionary<uint2, PaintedNode> cells = new Dictionary<uint2, PaintedNode>();
    private void Start()
    {
        for (uint i = 0; i < GridSize; i++)
        {
            for (uint j = 0; j < GridSize; j++)
            {
                cells.Add(new uint2(i, j), Instantiate(prefab, new Vector3(i, j, 0), Quaternion.identity));
            }
        }
    }

    private void Update()
    {
        NativeList<uint2> collectedNodes = new NativeList<uint2>(Allocator.Temp);
        foreach (PaintedNode node in cells.Values)
        {
            node.Paint(Color.white);
        }

        float3 center = pointA.position;
        Circle circle = new Circle(center.xy, math.distance(pointB.position, pointA.position));
        GridIntersection.Circle(circle, collectedNodes);

        foreach (uint2 cell in collectedNodes)
        {
            if (cells.ContainsKey(cell))
            {
                cells[cell].Paint(Color.green);
            }
        }

        collectedNodes.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawWireSphere(pointA.position, math.distance(pointA.position, pointB.position));
    }
}