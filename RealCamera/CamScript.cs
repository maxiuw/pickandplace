using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.UnityRoboticsDemo;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using System;

public class CamScript : MonoBehaviour
{

    int currentCamIdx = 0;
    // WebCamTexture tex; 
    Texture2D texRos;
    public RawImage display;
    ROSConnection m_Ros;
    CompressedImageMsg img_msg;
    string[] camtopics = {"/camera_arm/image_raw","/camera_top/image_raw"};
    int currenttopic = 0;
    // string topcamera = "/camera_top/image_raw";
    
    void Start() {
        // start the ROS connection
        m_Ros = ROSConnection.GetOrCreateInstance();
        // register topic name 
        // m_Ros.RegisterPublisher<RosMessageTypes.Sensor.ImageMsg>(topicName);
        Debug.Log(m_Ros.RosIPAddress);
        m_Ros.Subscribe<ImageMsg>(camtopics[currenttopic], StartStopCam_Clicked);
    
    }
    
    
    public void SwapCam_Clicked() {
        // switching between two cameras, works for 2 
        if (WebCamTexture.devices.Length > 0) {
            currentCamIdx += 1;
            currentCamIdx %= WebCamTexture.devices.Length;
            m_Ros.Unsubscribe(camtopics[currenttopic]);
            currenttopic = Math.Abs(currenttopic - 1);
            m_Ros.Subscribe<ImageMsg>(camtopics[currenttopic], StartStopCam_Clicked);
        }
    }
    
    public void StartStopCam_Clicked(ImageMsg img) {
        // stopping the prev output and clearing the texture
        // if (texRos != null) {
        //     display.texture = null;
        //     // texRos.Stop(); 
        //     texRos = null;
        // } else {
        // RenderTexture rendtextRos = new RenderTexture(640, 480, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        // rendtextRos.Create();
        // rendtextRos.
        texRos = new Texture2D((int) img.width, (int) img.height, TextureFormat.RGB24, false); // , TextureFormat.RGB24
        BgrToRgb(img.data);
        texRos.LoadRawTextureData(img.data);
        texRos.Apply();
        display.texture = texRos;        
    }

    public void BgrToRgb(byte[] data) {
        for (int i = 0; i < data.Length; i += 3)
        {
            byte dummy = data[i];
            data[i] = data[i + 2];
            data[i + 2] = dummy;
        }
    }
}
