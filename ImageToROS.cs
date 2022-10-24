using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.UnityRoboticsDemo;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using System.Collections;
// credits 
// https://forum.unity.com/threads/getrawtexturedata-method-resulting-in-flipped-image-in-rviz.1148723/
/// <summary>
///
/// </summary>
 
// [RequireComponent(typeof(ROSClockSubscriber))]
public class ImageToROS : MonoBehaviour
{
    ROSConnection m_Ros;
    public string imageTopic = "/XRCamera";
    public string camInfoTopic = "/camera_info";
 
    public string CompressedImageTopic = "/XRCamera_compressed";
 
    public Camera target_camera;
 
    public bool compressed = false;
 
    public float pubMsgFrequency = 60f;
 
    private float timeElapsed;
    private RenderTexture renderTexture;
    private RenderTexture lastTexture;
 
    private Texture2D mainCameraTexture;
    private Rect frame;
 
 
    private int frame_width;
    private int frame_height;
    private const int isBigEndian = 0;
    private uint image_step = 4;
    TimeMsg lastTime;
    // string topicName = "XRCamera";
 
    // private ROSClockSubscriber clock;
 
    private ImageMsg img_msg;
    private CameraInfoMsg infoCamera;
 
    private HeaderMsg header;
 
    void Start()
    {
        // start the ROS connection
        m_Ros = ROSConnection.GetOrCreateInstance();
        // register topic name 
        // m_Ros.RegisterPublisher<RosMessageTypes.Sensor.ImageMsg>(topicName);
        Debug.Log(m_Ros.RosIPAddress);
        m_Ros.RegisterPublisher<ImageMsg>(imageTopic);
        m_Ros.RegisterPublisher<CompressedImageMsg>(CompressedImageTopic);

        m_Ros.RegisterPublisher<CameraInfoMsg>(camInfoTopic);
        // if(m_Ros)
        // {
        //     m_Ros.RegisterPublisher<ImageMsg>(imageTopic);
        //     m_Ros.RegisterPublisher<CompressedImageMsg>(CompressedImageTopic);
 
        //     m_Ros.RegisterPublisher<CameraInfoMsg>(camInfoTopic);
        //     // clock = GetComponent<ROSClockSubscriber>();
        // }
        // else
        // {
        //     Debug.Log("No ros connection found.");
        // }
 
 
        // if (!target_camera)
        // {
        //     // target_camera = Camera.main;
        //     Debug.Log("No camera found.");
        // }
 
        if (target_camera)
        {
            // the depth is the key! if 0 - nothing renders ...
            // Debug.Log("I arriaved to line 81");
            renderTexture = new RenderTexture(target_camera.pixelWidth, target_camera.pixelHeight, 8, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm); //R8G8B8A8_UNorm
            renderTexture.Create();
 
            frame_width = renderTexture.width;
            frame_height = renderTexture.height;
 
            frame = new Rect(0, 0, frame_width, frame_height);
 
            mainCameraTexture = new Texture2D(frame_width, frame_height, TextureFormat.RGBA32, false);
 
            header = new HeaderMsg();
 
            img_msg = new ImageMsg();
 
            img_msg.width = (uint) frame_width;
            img_msg.height = (uint) frame_height;
            img_msg.step = image_step * (uint) frame_width;
            img_msg.encoding = "rgba8";
 
            // infoCamera = CameraInfoMsg. CameraInfoGenerator.ConstructCameraInfoMessage(target_camera, header);
 
        }
        else
        {
            Debug.Log("No camera found.");
        }
    }
 
    private void Update()
    {
        if (target_camera)
        {
            timeElapsed += Time.deltaTime;
 
            if (timeElapsed > (1 / pubMsgFrequency))
            {
                // header.stamp = clock._time;
                // infoCamera.header = new RosMessageTypes.Std.HeaderMsg();
                RosMessageTypes.Std.HeaderMsg header_m = new RosMessageTypes.Std.HeaderMsg();
                header_m.frame_id = "XRCam";
                // img_msg.header = header_m;//new RosMessageTypes.Std.HeaderMsg();
                img_msg.data = get_frame_raw();
           
                m_Ros.Publish(imageTopic, img_msg);
                // m_Ros.Publish(camInfoTopic, infoCamera);
 
                timeElapsed = 0;
            }
        }
        else
        {
            Debug.Log("No camera found.");
        }
 
    }
 
    private byte[] get_frame_raw()
    {      
        target_camera.targetTexture = renderTexture;
        lastTexture = RenderTexture.active;
 
        RenderTexture.active = renderTexture;
        // target_camera.Render();
        target_camera.Render(); //WithShader(Shader.Find("Standard"));
        mainCameraTexture.ReadPixels(frame, 0, 0);
        mainCameraTexture.Apply();
 
        target_camera.targetTexture = lastTexture;
 
        target_camera.targetTexture = null;
 
        return mainCameraTexture.GetRawTextureData();
    }
}
