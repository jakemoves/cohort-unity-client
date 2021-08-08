using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientToGlobal : MonoBehaviour
{
    // Start is called before the first frame update
    Quaternion gimbalCorrection;
    void Start()
    {
        // Assume Gyro is enabled
        Input.compensateSensors = true;
        gimbalCorrection = Quaternion.AngleAxis(-180, new Vector3(1, 0, 0));
    }

    // Update is called once per frame
    void Update()
    {
        var q = (Input.gyro.attitude * gimbalCorrection);
        q.x *= -1;
        q.z *= -1;
        gameObject.transform.rotation = q;
    }
}
