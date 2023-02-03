using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamScreen : MonoBehaviour
{
    Vector3 initialpoisition;
    Quaternion initialrotation;

    // Start is called before the first frame update
    void Start()
    {
        // record initial rot and pose
        initialpoisition = this.transform.position;
        initialrotation = this.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // hold space bar for 1 sec to reset the camera view    
        if (Input.GetKey(KeyCode.Space)) {
            ResetCam();
        }

        // on hold left arrow press, keep on moving lef
        if (Input.GetKey(KeyCode.A)) {
            transform.Translate(new Vector3(-0.0025f, 0.001f, 0));
            // transform.Rotate(new Vector3(0, 0.1f, 0.05f));
        }
        // on right arrow press, move right
        if (Input.GetKey(KeyCode.D)) {
            transform.Translate(new Vector3(0.0025f, 0.001f, 0));
            // transform.Rotate(new Vector3(0, -0.1f, 0.05f));
        }
        // on up arrow press, move up
        if (Input.GetKey(KeyCode.W)) {
            transform.Translate(new Vector3(0, 0.0025f, 0));
            // transform.Rotate(new Vector3(0, -0.1f, 0.05f));
        }
        // on down arrow press, move down
        if (Input.GetKey(KeyCode.S)) {
            transform.Translate(new Vector3(0, -0.0025f, 0));
            // transform.Rotate(new Vector3(0, -0.1f, 0.05f));
        }
        // on holding the left mouse button, rotate in the direction where the mouse is moving
        if (Input.GetMouseButton(1)) {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            transform.Rotate(new Vector3(-y, x, 0));
        }
        // zoom in and out on the mouse scroll
        if (Input.GetAxis("Mouse ScrollWheel") > 0) {
            transform.Translate(new Vector3(0, 0, 0.1f));
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0) {
            transform.Translate(new Vector3(0, 0, -0.1f));
        }
    
    }
    void ResetCam() {
        this.transform.position = initialpoisition;
        this.transform.rotation = initialrotation;
    }
}
