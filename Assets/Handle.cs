using System;
using Unity.Mathematics;
using UnityEngine;

public class Handle : MonoBehaviour
{
    private Camera cam;
    private Action updateBezier;

    private void Start()
    {
        Debug.Assert(GetComponent<Collider>());
        cam = Camera.main;
    }
    private void OnMouseDrag()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        transform.position = LineIntersectXZPlane(ray);
    }

    private void OnMouseUp()
    {
        updateBezier.Invoke();
    }

    private static float3 LineIntersectXZPlane(Ray ray)
    {
        float3 n = new float3(0, 1, 0);
        if (math.distance(math.dot(ray.direction, n), 0) < math.EPSILON)
        {
            return new float3(float.NaN, float.NaN, float.NaN);
        }
        float d = math.dot(-ray.origin, n) / math.dot(ray.direction, n);
        return ray.origin + ray.direction * d;
    }

    public void AddCallbackOnChange(Action updateBezier)
    {
        this.updateBezier = updateBezier;
    }
}
