using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;


public class cameraPub : MonoBehaviour
{
    ROSConnection m_Ros;
    public Camera sensorCamera;
    public RenderTexture targetTex;
    string topicName = "/XRCam";
    int resolutionWidth = 256;
    int resolutionHeight = 256;
    public int qualityLevel = 50;
    WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
    // Start is called before the first frame update
    void Start()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();
        // register topic name 
        m_Ros.RegisterPublisher<RosMessageTypes.Sensor.ImageMsg>(topicName);
        Debug.Log(m_Ros.RosIPAddress);
        
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("I update");
        SendImage();
    }
    
    void SendImage() {
        Texture2D texture2D = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        Rect rect = new Rect(0, 0, resolutionWidth, resolutionHeight);
        sensorCamera.targetTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        uint imageHeight = (uint)targetTex.height;
        uint imageWidth = (uint)targetTex.width;
        // RosMessageTypes.Sensor.ImageMsg rosImage = new RosMessageTypes.Sensor.ImageMsg(new RosMessageTypes.Std.HeaderMsg(), imageWidth, imageHeight, "rgba8", 0, imageBytes);

        RosMessageTypes.Sensor.ImageMsg message = new RosMessageTypes.Sensor.ImageMsg();
        message.header.frame_id = "xrcamera_out";
        message.encoding = "rgb24";
        // message.format = "jpeg";
        // message.header.
        texture2D.ReadPixels(rect, 0, 0);
        message.data = texture2D.EncodeToJPG(qualityLevel);
        // Publish(message);
        m_Ros.Publish(topicName, message);
        // Debug.Log("I sart  send img");
        // // var oldRT = RenderTexture.active;
        // // RenderTexture.active = sensorCamera.targetTexture;
        // // var oldRT = sensorCamera.targetTexture;
        // // sensorCamera.Render();
        // // targetTex.
        // // forcing the script to wait for the frame to end (error with readPixels)
        // // yield return frameEnd;
        // Debug.Log("I sent img");
        // uint imageHeight = (uint)targetTex.height;
        // uint imageWidth = (uint)targetTex.width;
        // // // Copy the pixels from the GPU into a texture so we can work with them
        // // // For more efficiency you should reuse this texture, instead of creating a new one every time
        // // Texture2D camText = new Texture2D(targetTex.width, targetTex.height);
        // // camText.ReadPixels(new Rect(0, 0, targetTex.width, targetTex.height), 0, 0);
        // // camText.Apply();
        // // RenderTexture.active = targetTex;
        // const int isBigEndian = 0;
        // uint step = imageWidth * 4; 
        // // // Encode the texture as a PNG, and send to ROS 
        // // // camText.format = TextureFormat.RGB24;
        // // byte[] imageBytes = camText.EncodeToPNG();
        // // var message = new MCompressedImage(new MHeader(), "png", imageBytes);
        // byte[] imageBytes = getTex();
        // RosMessageTypes.Sensor.ImageMsg rosImage = new RosMessageTypes.Sensor.ImageMsg(new RosMessageTypes.Std.HeaderMsg(), imageWidth, imageHeight, "rgba8", isBigEndian, step, imageBytes);
        // // Debug.Log(rosImage);
        // m_Ros.Publish(topicName, rosImage);
    }

    // private byte[] getTex() {
        // Texture2D texture2D = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        // Rect rect = new Rect(0, 0, resolutionWidth, resolutionHeight);
        // sensorCamera.targetTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        // RosMessageTypes.Sensor.ImageMsg message = new RosMessageTypes.Sensor.ImageMsg();
        // message.header.frame_id = "xrcamera_out";
        // // message.format = "jpeg";
        // // message.header.
        // texture2D.ReadPixels(rect, 0, 0);
        // message.data = texture2D.EncodeToJPG(qualityLevel);
        // // Publish(message);
        // m_Ros.Publish(topicName, message);



        // // sensorCamera.targetTexture = targetTex;
        // // RenderTexture currentRT = RenderTexture.active;
        // // RenderTexture.active = targetTex;
        // // sensorCamera.Render();
        // Texture2D mainCameraTexture = new Texture2D(targetTex.width, targetTex.height);
        // RenderTexture.active = targetTex;
        // mainCameraTexture.ReadPixels(new Rect(0, 0, targetTex.width, targetTex.height), 0, 0);
        // mainCameraTexture.Apply(); // this is ready texture 2d 
        // Rect rect = new Rect(0, 0, (uint)targetTex.width, (uint)targetTex.height);

        // // RenderTexture.active = currentRT;
        // // Get the raw byte info from the screenshot
        // byte[] imageBytes = mainCameraTexture.GetRawTextureData();
        // sensorCamera.targetTexture = null;
        // return imageBytes;
    // }
}
