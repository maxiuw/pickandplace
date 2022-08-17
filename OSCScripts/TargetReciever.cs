using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetReciever : MonoBehaviour
{
    //  playerPos = GameObject.Find ("Player(clone)").transform;
    public OSC osc;
    // Start is called before the first frame update
    void Start()
    {
        osc.SetAddressHandler("/TargetReciever", ChangeTarget);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (OscMessage m in osc.messagesReceived) {
            // Debug.Log($"I recieveed {m}");
            if (m.address == "/TargetReciever") {
                ChangeTarget(m);
            }
        }
    }
    public void ChangeTarget(OscMessage message) {
        Vector3 current = new Vector3(message.GetFloat(0),message.GetFloat(1),message.GetFloat(2));
        gameObject.transform.position = new Vector3(message.GetFloat(0),message.GetFloat(1),message.GetFloat(2));
    }
}
