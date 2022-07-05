using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class getConsoleOut : MonoBehaviour
{
    public TrajectoryPlanner planner;

    // private Stack<string> messages;
    void Update()
    {
        // Debug.Log(planner.messagestoshow.Count);
        if (planner.messagestoshow.Count != 0) {
            Text canvas_text = gameObject.GetComponent<Text>();
            canvas_text.text = planner.messagestoshow.Pop();
        }
    }
}