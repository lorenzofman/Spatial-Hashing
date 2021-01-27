using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BezierEditor : MonoBehaviour
{
    private BezierUpdateMode callbackBezierUpdateMode;

    public BezierUpdateMode CallbackBezierUpdateMode
    {
        get => callbackBezierUpdateMode;
        set
        {
            callbackBezierUpdateMode = value;
            foreach (Handle handle in handles)
            {
                handle.updateMode = callbackBezierUpdateMode;
            }
        }
    }

    public Handle handlePrefab;
    private const float ScaleFactor = 0.03f;
    private readonly Handle[] handles = new Handle[4];
    private EntityArchetype bezierGeneratorArchetype;
    

    private Camera cam;

    private void Start()
    {
        for (int i = 0; i < handles.Length; i++)
        {
            handles[i] = Instantiate(handlePrefab);
            handles[i].AddCallbackOnChange(UpdateBezier);
        }
        bezierGeneratorArchetype = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(typeof(CubicBezier));
        cam = Camera.main;
        CreateBezier();
        UpdateBezier();
    }

    private void UpdateBezier()
    {
        Entity entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity(bezierGeneratorArchetype);
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(entity, new CubicBezier
        {
            p0 = handles[0].transform.position,
            p1 = handles[1].transform.position,
            p2 = handles[2].transform.position,
            p3 = handles[3].transform.position,
            segments = Program.Env.roadSegmentsResolution
        });
    }

    private void Update()
    {
        foreach (Handle t in handles)
        {
            t.transform.localScale = Vector3.Distance(cam.transform.position, t.transform.position) * ScaleFactor * Vector3.one;
        }
    }
    
    private void CreateBezier()
    {
        float2 bl = Environment.TerrainBoundaries.Min.xz + new float2(1, 1);
        float2 tr = Environment.TerrainBoundaries.Max.xz - new float2(1, 1);
        handles[0].transform.position = new Vector3(bl.x, 0, bl.y);
        handles[1].transform.position = new Vector3(bl.x, 0, tr.y);
        handles[2].transform.position = new Vector3(tr.x, 0, bl.y);
        handles[3].transform.position = new Vector3(tr.x, 0, tr.y);
    }
}