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
    public RawImage table_display;
    ROSConnection m_Ros;
    CompressedImageMsg img_msg;
    string[] camtopics = {"/myresult", "/myresult_rs"}; // "/camera_top/image_raw" "/camera_arm/image_raw",
    int currenttopic = 0;
    int width = 640; // 256 since we resized the image 
    // string topcamera = "/camera_top/image_raw";
    int height = 480; 
    int frame = 0;
    int frame_unity = 0;
    Stack<byte[]> imgmessages;
    byte[] last_imgmessages;
    public GameObject XROrgin;
    public bool passthrough = false;
    void Start() {
        // init all necessary stuff
        m_Ros = ROSConnection.GetOrCreateInstance();
        texRos = new Texture2D(width, height, TextureFormat.RGB24, false); // , TextureFormat.RGB24   
        // queue for images since subscriber does not support that 
        imgmessages = new Stack<byte[]>();
        // passthrough disabled at the start 
        table_display.enabled = false;    
    }

    public void StartSub() {
        m_Ros.Subscribe<ImageMsg>(camtopics[currenttopic], StartStopCam_Clicked);
    }
    private void Update() {
        frame_unity++;
        if (frame_unity == 5) { //  & imgmessages.Count > 0
            frame_unity = 0;
            // BgrToRgb(img.data); // done in the video_stream.cpp file 
            try {
                texRos.LoadRawTextureData(imgmessages.Pop()); //
                texRos.Apply();
                display.texture = texRos; 
                table_display.texture = texRos;
                imgmessages = new Stack<byte[]>();
            } catch {
                // do nothing
            }       
        }
    }

    void Start_Overlap_Image_Click() {
        passthrough = !passthrough;
        table_display.enabled = passthrough;
    }
    
    
    public void SwapCam_Clicked() {
        // switching between two cameras, works for 2 
        // if (WebCamTexture.devices.Length > 0) {
        //     currentCamIdx += 1;
        //     currentCamIdx %= WebCamTexture.devices.Length;
        frame = 0;
        m_Ros.Unsubscribe(camtopics[currenttopic]);
        imgmessages = new Stack<byte[]>();
        currenttopic = Math.Abs(currenttopic - 1);
        m_Ros.Subscribe<ImageMsg>(camtopics[currenttopic], StartStopCam_Clicked);
        // if (currenttopic == 0) {
        //     width = 256;
        //     height = 256;
        // } else {
        //     width = 640;
        //     height = 480;
        // }
        // // imgmessages = new Stack<byte[]>();
        // texRos = new Texture2D(width, height, TextureFormat.RGB24, false); // , TextureFormat.RGB24
        // }
    }
    public void ComeBack() {
        XROrgin.transform.position = new Vector3(0.065f, 0.668f, 0.784f);
    }
    public void movetoImage() {
        XROrgin.transform.position = new Vector3(-13.5f, 25, 66);
    }
    
    public void StartStopCam_Clicked(ImageMsg img) {
        // Debug.Log("go message");
        // stopping the prev output and clearing the texture
        // Debug.Log($"message {frame} recieved {img.data.Length} stack {imgmessages.Count} size {img.height}");
        imgmessages.Push(img.data);
        // last_imgmessages = camtopics[currenttopic];

        frame++;
        // // BgrToRgb(img.data); // done in the video_stream.cpp file 
        // texRos.LoadRawTextureData(img.data); //
        // texRos.Apply();
        // display.texture = texRos;        
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
