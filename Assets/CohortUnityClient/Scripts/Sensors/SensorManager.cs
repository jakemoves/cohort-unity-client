using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorManager : MonoBehaviour
{
    public bool compensateSensors = true;

    Quaternion gimbalCorrection = Quaternion.AngleAxis(-180, new Vector3(1, 0, 0));
    public Quaternion Attitude { get; private set; }
    public Vector3 GlobalAcceleration { get; private set; }

    
    
    // Start is called before the first frame update
    void Start()
    {
        // Init
        if(!Input.gyro.enabled) {
            Input.gyro.enabled = true;
            Debug.LogWarning("Had to enable Gyro");
        }

        Input.compensateSensors = compensateSensors;
        gimbalCorrection = Quaternion.AngleAxis(-180, new Vector3(1, 0, 0));
    
        CorrectAttitude();
        CalculateGlobalVectors();
    }

    // Update is called once per frame
    void Update()
    {
        CorrectAttitude();
        CalculateGlobalVectors();

        Debug.Log($"ATT  x:{Attitude.eulerAngles.x} y:{Attitude.eulerAngles.y} z:{Attitude.eulerAngles.z}\n" +
        $"GACC x:{GlobalAcceleration.x} y:{GlobalAcceleration.y} z:{GlobalAcceleration.z}");
    
    }

    void CorrectAttitude()
    {
        var q = (Input.gyro.attitude * gimbalCorrection);
        q.x *= -1;
        q.z *= -1;
        Attitude = q;
    }

    void CalculateGlobalVectors()
    {
        GlobalAcceleration = Attitude * Input.gyro.userAcceleration;
    }
}
