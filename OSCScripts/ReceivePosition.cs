using UnityEngine;
using System.Collections;

public class ReceivePosition : MonoBehaviour {
    
   	public OSC osc;

	// Use this for initialization
	void Start () {
    //    string mesg = $"IP {osc.outIP} outport {osc.outPort} inport {osc.inPort}";
    //    outputtext.text = mesg;
	   osc.SetAddressHandler( "/CubeXYZ" , OnReceiveXYZ );
       osc.SetAddressHandler("/CubeX", OnReceiveX);
       osc.SetAddressHandler("/CubeY", OnReceiveY);
       osc.SetAddressHandler("/CubeZ", OnReceiveZ);
    }
	
	// Update is called once per frame
	void Update () {
        foreach (OscMessage m in osc.messagesReceived) {
            // string msg = $"I recieved {m}";
            // outputtext.text = msg;
            string substr = m.address.Substring(m.address.Length-3, 3); // start, length
            if (substr == "XYZ") {
                OnReceiveXYZ(m);
            } else if (m.address.Substring(m.address.Length-1, 1) == "X") 
                OnReceiveX(m);
            else if (m.address.Substring(m.address.Length-1, 1) == "Y")
                 OnReceiveY(m);
            else if (m.address.Substring(m.address.Length-1, 1) == "Z")
                 OnReceiveZ(m);
            Debug.Log($"{m.address.Substring(m.address.Length-3, 3)} val {m.values} {m.GetInt(0)}"); //.Substring(m.address.Length-4, m.address.Length-2)

        }
	}

	void OnReceiveXYZ(OscMessage message){
        Debug.Log($"I recieved {message}");
		float x = message.GetInt(0);
        float y = message.GetInt(1);
		float z = message.GetInt(2);

		transform.position = new Vector3(x,y,z);
	}

    void OnReceiveX(OscMessage message) {
        Debug.Log($"I recieved {message}");
        float x = message.GetFloat(0);

        Vector3 position = transform.position;

        position.x = x;

        transform.position = position;
    }

    void OnReceiveY(OscMessage message) {
        
        float y = message.GetFloat(0);

        Vector3 position = transform.position;

        position.y = y;

        transform.position = position;
    }

    void OnReceiveZ(OscMessage message) {
        float z = message.GetFloat(0);

        Vector3 position = transform.position;

        position.z = z;

        transform.position = position;
    }

	// Use this for initialization
	// void Start () {
	//    osc.SetAddressHandler( "/CubeXYZ" , OnReceiveXYZ );
    //    osc.SetAddressHandler("/CubeX", OnReceiveX);
    //    osc.SetAddressHandler("/CubeY", OnReceiveY);
    //    osc.SetAddressHandler("/CubeZ", OnReceiveZ);
    // }
	
	// // Update is called once per frame
	// void Update () {
	
	// }

	// void OnReceiveXYZ(OscMessage message){
	// 	float x = message.GetInt(0);
    //      float y = message.GetInt(1);
	// 	float z = message.GetInt(2);

	// 	transform.position = new Vector3(x,y,z);
	// }

    // void OnReceiveX(OscMessage message) {
    //     float x = message.GetFloat(0);

    //     Vector3 position = transform.position;

    //     position.x = x;

    //     transform.position = position;
    // }

    // void OnReceiveY(OscMessage message) {
    //     float y = message.GetFloat(0);

    //     Vector3 position = transform.position;

    //     position.y = y;

    //     transform.position = position;
    // }

    // void OnReceiveZ(OscMessage message) {
    //     float z = message.GetFloat(0);

    //     Vector3 position = transform.position;

    //     position.z = z;

    //     transform.position = position;
    // }


}
