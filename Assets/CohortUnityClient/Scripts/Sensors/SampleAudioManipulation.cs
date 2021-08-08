using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SampleAudioManipulation : MonoBehaviour
{
    // Start is called before the first frame update
    AudioSource source;
    public Dropdown selector;
    public SensorManager sensorManager;
    int sel = 5;

     
    float lowPassFilterFactor = (1 / 60) / 1f;
    Vector3 lowPassValue = Vector3.zero;

    void Start()
    {
        source = this.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        sel = selector.value;
        switch (sel)
        {
            case 0:
                ManipulateAudio(Input.acceleration);
                break;

            case 1:
                ManipulateAudio(Input.gyro.userAcceleration);
                break;

            case 2:
                if (sensorManager != null) ManipulateAudio(sensorManager.GlobalAcceleration);
                else ManipulateAudio(Vector3.zero);
                break;

            case 3:
                ManipulateAudio(Input.gyro.gravity);
                break;

            case 4:
                ManipulateAudio(Input.gyro.rotationRate);
                break;

            case 5:
                ManipulateAudio(Input.gyro.rotationRateUnbiased);
                break;

            case 6:
                if (sensorManager != null) ManipulateAudio(sensorManager.Attitude.eulerAngles);
                else ManipulateAudio(Input.gyro.attitude.eulerAngles);
                break;

            default:
                break;
        }
    }

    void ManipulateAudio(Vector3 vector)
    {
        if (source == null || vector == null) return;
        //lowPassValue = Vector3.Lerp(lowPassValue, vector.normalized, lowPassFilterFactor);
        //source.panStereo = vector.normalized.x;
        //source.pitch = lowPassValue.z;//(vector.normalized.z * Time.deltaTime * 9.81f) + 1;
        source.pitch = vector.normalized.z + 1;
    }
}
