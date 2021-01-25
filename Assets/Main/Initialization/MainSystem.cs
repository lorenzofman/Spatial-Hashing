using UnityEngine;

[DefaultExecutionOrder(-200000)]
public class MainSystem : MonoBehaviour
{
    private void Awake()
    {
        Program.Env = new Environment
        {
            treeCount = 1000000,
            gridSize = 20.0f,
            roadSegmentsResolution = 10000,
            roadWidth = 0.5f
        };
    }
}
