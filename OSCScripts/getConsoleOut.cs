using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class getConsoleOut : MonoBehaviour
{
    // public TrajectoryPlanner planner;
    public string output = "";
    public string stack = "";
    // private Stack<string> messages;
    void Update()
    {
        // Debug.Log(planner.messagestoshow.Count);
        if (output.Length != 0) {
            Text canvas_text = gameObject.GetComponent<Text>();
            canvas_text.text = output;
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
    }
}
