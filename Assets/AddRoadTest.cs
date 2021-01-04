using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AddRoadTest : MonoBehaviour
{
    private EntityArchetype entityArchetype;
    private void Start()
    {
        entityArchetype = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(typeof(RoadSegment));
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }
        Entity entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity(entityArchetype);
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(entity, new RoadSegment
        {
            initial = new float3(1, 0, 1),
            final = new float3(100, 0, 100)
        });
    }
}
