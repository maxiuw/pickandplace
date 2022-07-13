using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignTarget : MonoBehaviour
{
    public TrajectoryPlanner planner;
    // Start is called before the first frame update
    public void assignTarget() {
        planner = (TrajectoryPlanner)FindObjectOfType(typeof(TrajectoryPlanner));
        planner.m_Target = GetComponent<GameObject>();
        planner.messagestoshow.Push($"Added a target at {planner.m_Target.transform.position}");
    }
}
