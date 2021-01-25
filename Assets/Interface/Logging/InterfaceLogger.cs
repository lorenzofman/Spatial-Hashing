using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InterfaceLogger : MonoBehaviour
{
    public static readonly Queue<string> Logs = new Queue<string>();
    public Log logPrefab;
    public ScrollRect logManager;

    private void Update()
    {
        while (Logs.Count > 0)
        {
            string msg = Logs.Dequeue();
            Log instance = Instantiate(logPrefab, logManager.content);
            instance.logText.text = msg;
        }
    }
}
