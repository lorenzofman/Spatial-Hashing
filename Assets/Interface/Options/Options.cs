using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    [SerializeField] private Toggle cacheHashTable;
    [SerializeField] private TMP_Dropdown bezierUpdateMode;
    [SerializeField] private ProSlider trees;
    [SerializeField] private ProSlider gridSize;
    [SerializeField] private ProSlider bezierSegments;
    [SerializeField] private ProSlider riverRoadWidth;

    private RoadTreeRemovalSystem roadTreeRemovalSystem;
    private BezierEditor editor;

    private void Start()
    {
        cacheHashTable.isOn = true;
        
        ConfigureListeners();

        CacheReferences();
    }

    private void CacheReferences()
    {
        roadTreeRemovalSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<RoadTreeRemovalSystem>();
        editor = FindObjectOfType<BezierEditor>();
    }

    private void ConfigureListeners()
    {
        cacheHashTable.onValueChanged.AddListener(OnCacheHashTableToggleChange);
        bezierUpdateMode.onValueChanged.AddListener(OnBezierUpdateModeChange);
        trees.onValueChanged.AddListener(OnTreesCountChange);
        gridSize.onValueChanged.AddListener(OnGridSizeChange);
        bezierSegments.onValueChanged.AddListener(OnBezierSegmentsChange);
        riverRoadWidth.onValueChanged.AddListener(OnRiverRoadWidthChange);
    }

    private void OnCacheHashTableToggleChange(bool arg0)
    {
        roadTreeRemovalSystem.CacheHashTable = !roadTreeRemovalSystem.CacheHashTable;
    }
    
    private void OnBezierUpdateModeChange(int arg0)
    {
        editor.CallbackBezierUpdateMode = (BezierUpdateMode) arg0;
    }
    
    private static void OnTreesCountChange(float arg0)
    {
        int trees = (int) arg0;
        Program.Env.treeCount = trees;
    }
    
    private static void OnGridSizeChange(float arg0)
    {
        Program.Env.gridSize = arg0;
    }

    private static void OnBezierSegmentsChange(float arg0)
    {
        int segments = (int) arg0;
        Program.Env.roadSegmentsResolution = segments;
    }

    private static void OnRiverRoadWidthChange(float arg0)
    {
        Program.Env.roadWidth = arg0;
    }
    
}