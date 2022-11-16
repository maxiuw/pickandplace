using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;

public class RestartScene : MonoBehaviour
{   
    ROSConnection m_Ros;
    public void Res_Scene() {
        // disconnect from the prev ROS connection 

        string scene_name = SceneManager.GetActiveScene().name;
        SceneManager.UnloadSceneAsync(scene_name);
        SceneManager.LoadScene("FrankaScene");
    }
}
