using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SampleAudioManipulation : MonoBehaviour
{
    // Start is called before the first frame update
    AudioSource source;
    public Dropdown selector;
    int sel = 5;
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
                ManipulateAudio(Input.gyro.gravity);
                break;

            case 3:
                ManipulateAudio(Input.gyro.rotationRate);
                break;

            case 4:
                ManipulateAudio(Input.gyro.rotationRateUnbiased);
                break;

            case 5:
                ManipulateAudio(Input.gyro.attitude.eulerAngles);
                break;

            default:
                break;
        }
    }

    void ManipulateAudio(Vector3 vector)
    {
        if (source == null || vector == null) return;

        source.panStereo = vector.normalized.z;
        source.pitch = (vector.normalized.y * 1) + 1;
    }
}
