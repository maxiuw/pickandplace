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
    string[] camtopics = {"/camera_top/image_raw", "/camera_arm/image_raw"}; // "/camera_arm/image_raw",
    int currenttopic = 0;
    int height = 256; 
    int width = 256; // 256 since we resized the image 
    // string topcamera = "/camera_top/image_raw";
    int frame = 0;
    void Start() {
        // start the ROS connection
        // if (currenttopic == 0) {
        //     width = 640;
        //     height = 480;
        // } else {
        //     width = 800;
        //     height = 448;
        // }
        // width = 640;
        // height = 480;
        m_Ros = ROSConnection.GetOrCreateInstance();
        // register topic name 
        // m_Ros.RegisterPublisher<RosMessageTypes.Sensor.ImageMsg>(topicName);
        // Debug.Log(m_Ros.RosIPAddress);
        texRos = new Texture2D(width, height, TextureFormat.RGB24, false); // , TextureFormat.RGB24   
        m_Ros.Subscribe<ImageMsg>(camtopics[0], StartStopCam_Clicked);
        // m_Ros.Subscribe<ImageMsg>(camtopics[1], StartStopCam_Clicked);

    
    }

    public void StartSub() {
        m_Ros.Subscribe<ImageMsg>(camtopics[currenttopic], StartStopCam_Clicked);
    }
    
    
    public void SwapCam_Clicked() {
        // switching between two cameras, works for 2 
        // if (WebCamTexture.devices.Length > 0) {
        //     currentCamIdx += 1;
        //     currentCamIdx %= WebCamTexture.devices.Length;
        frame = 0;
        m_Ros.Unsubscribe(camtopics[currenttopic]);
        currenttopic = Math.Abs(currenttopic - 1);
        m_Ros.Subscribe<ImageMsg>(camtopics[currenttopic], StartStopCam_Clicked);
        // if (currenttopic == 0) {
        //     width = 640;
        //     height = 480;
        // } else {
        //     width = 800;
        //     height = 448;
        // }
        texRos = new Texture2D(width, height, TextureFormat.RGB24, false); // , TextureFormat.RGB24
        // }
    }
    
    public void StartStopCam_Clicked(ImageMsg img) {
        // stopping the prev output and clearing the texture
        Debug.Log($"message {frame} recieved {img.data.Length}");
        frame++;
        // BgrToRgb(img.data); // done in the video_stream.cpp file 
        texRos.LoadRawTextureData(img.data); //
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
