using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabOjectsScreen : MonoBehaviour
{
    private Vector3 mOffset;
    private float mZCoord;
    public Camera cam;
    void Start() {
        // cam = find camera object 
        cam = Camera.main;
    }
    void OnMouseDown() {
        // get z coordinate of the camera
        mZCoord = cam.WorldToScreenPoint(gameObject.transform.position).z;
        mOffset = gameObject.transform.position - GetMouseWorldPos(); 
    }
    private Vector3 GetMouseWorldPos() {
        // screen to mouse 
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mZCoord; // offset on the z coordinate
        return cam.ScreenToWorldPoint(mousePoint);
      
    }
    void OnMouseDrag() {
        // move the object to the mouse position
        transform.position = GetMouseWorldPos() + mOffset;
    }
}
