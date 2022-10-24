using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class CamScript : MonoBehaviour
{

    int currentCamIdx = 0;
    WebCamTexture tex; 
    public RawImage display;

    public void SwapCam_Clicked() {
        if (WebCamTexture.devices.Length > 0) {
            currentCamIdx += 1;
            currentCamIdx %= WebCamTexture.devices.Length;
        }
    }
    
    public void StartStopCam_Clicked() {
        // stopping the prev output and clearing the texture
        if (tex != null) {
            display.texture = null;
            tex.Stop(); 
            tex = null;
        } else {
            WebCamDevice device = WebCamTexture.devices[currentCamIdx];
            tex = new WebCamTexture(device.name); // ref device neame 
            // raw image display to be reference 
            display.texture = tex; // if we switch cam, we want this cam to be ready for getting the input 
            tex.Play();
        }
        
    }
}
