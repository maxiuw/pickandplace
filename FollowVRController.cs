using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowVRController : MonoBehaviour
{
    public float x = 0.4f;
    public float y = -0.2f;
    public float z = -0.8f;
    public GameObject controller;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        Vector3 controller_position = controller.transform.position;
        controller_position.x += x;
        controller_position.z += z;
        controller_position.y += y;
        this.transform.position = controller_position;
        this.transform.rotation = controller.transform.rotation;
    }
}
