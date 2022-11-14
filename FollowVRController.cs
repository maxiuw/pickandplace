using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowVRController : MonoBehaviour
{
    public GameObject controller;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        Vector3 controller_position = controller.transform.position;
        controller_position.y += 0.2f;
        controller_position.z -= 0.2f;
        this.transform.position = controller_position;
        this.transform.rotation = controller.transform.rotation;
    }
}
