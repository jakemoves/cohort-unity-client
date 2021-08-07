using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorTest : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject box;

    void Start()
    {
        if(!Input.gyro.enabled) {
            Input.gyro.enabled = true;
            Debug.LogWarning("Had to enable Gyro");
        }

        if(!Input.compass.enabled) {
            Input.compass.enabled = true;
            Debug.LogWarning("Had to enable the Compass");
        }
        Debug.Assert(Input.compass.enabled, "Compass cannot be enabled");
    }

    // Update is called once per frame
    void Update()
    {

        var acc = Input.acceleration;
        var uAcc = Input.gyro.userAcceleration;
        var grav = Input.gyro.gravity;
        var att = Input.gyro.attitude;
        var rot = Input.gyro.rotationRate;
        var rotUnbiased = Input.gyro.rotationRateUnbiased;
        var comp = Input.compass;
        var logStr = "";

        logStr += $"ACC          x:{acc.x} y:{acc.y} z:{acc.z} Mag={acc.magnitude}\n";
        logStr += $"ACC No-Grav  x:{uAcc.x} y:{uAcc.y} z:{uAcc.z} Mag={uAcc.magnitude}\n";
        logStr += $"GRAVITY      x:{grav.x} y:{grav.y} z:{grav.z} Mag={grav.magnitude}\n";
        logStr += $"ATTITUDE     x:{att.x} y:{att.y} z:{att.z}\n";
        logStr += $"ROTATION     x:{rot.x} y:{rot.y} z:{rot.z} Mag={rot.magnitude}\n";
        logStr += $"ROTATION un  x:{rotUnbiased.x} y:{rotUnbiased.y} z:{rotUnbiased.z} Mag={rotUnbiased.magnitude}\n";
        logStr += $"ROTATION Differance {Mathf.Abs(rot.magnitude - rotUnbiased.magnitude)}\n";
        logStr += $"Input.gyro.updateInterval={Input.gyro.updateInterval}";
        logStr += $"COMPASS raw  x:{comp.rawVector.x} y:{comp.rawVector.y} z:{comp.rawVector.z} Mag={comp.rawVector.magnitude}\n";
        logStr += $"COMPASS hdg  Mag:{comp.magneticHeading} Tru:{comp.headingAccuracy} ACCURACY={comp.headingAccuracy}\n";

        
        Debug.Log(logStr);
        if(box != null) box.transform.rotation = att;
        
        //Debug.Log($"ACCELEROMETER x:{Input.acceleration.x} y:{Input.acceleration.y} z:{Input.acceleration.z} Mag={Input.acceleration.magnitude}");
    }
}
