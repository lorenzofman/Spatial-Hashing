using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour
{
    public static readonly Queue<string> Logs = new Queue<string>();
    public Log logPrefab;
    public Toggle renderTrees;
    public Toggle cacheHashTable;

    public ScrollRect logManager;
    
    private DisableRenderingSystem disableRenderingSystem;
    private RoadTreeRemovalSystem roadTreeRemovalSystem;

    
    private void Start()
    {
        renderTrees.isOn = true;
        cacheHashTable.isOn = false;

        renderTrees.onValueChanged.AddListener(OnRenderTreeToggleChange);
        cacheHashTable.onValueChanged.AddListener(OnCacheHashTableToggleChange);

        disableRenderingSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<DisableRenderingSystem>();
        roadTreeRemovalSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<RoadTreeRemovalSystem>();
    }

    private void Update()
    {
        while (Logs.Count > 0)
        {
            string msg = Logs.Dequeue();
            Log instance = Instantiate(logPrefab, logManager.content);
            instance.logText.text = msg;
        }
    }

    private void OnCacheHashTableToggleChange(bool arg0)
    {
        roadTreeRemovalSystem.CacheHashTable = !roadTreeRemovalSystem.CacheHashTable;
    }

    private void OnRenderTreeToggleChange(bool unused)
    {
        disableRenderingSystem.ToggleRendering();
    }
}