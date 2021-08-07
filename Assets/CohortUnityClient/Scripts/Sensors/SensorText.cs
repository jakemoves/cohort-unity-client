using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SensorText : MonoBehaviour
{
    // Start is called before the first frame update

    public Text display;
    public Dropdown selector;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (display == null || selector == null) return;

        //var str = "<sensor>";
        switch (selector.value)
        {
            case 0:
                display.text = Input.acceleration.magnitude.ToString("N4");
                break;

            case 1:
                display.text = Input.gyro.userAcceleration.magnitude.ToString("N4");
                break;

            case 2:
                display.text = Input.gyro.gravity.magnitude.ToString("N4");
                break;

            case 3:
                display.text = Input.gyro.rotationRate.magnitude.ToString("N4");
                break;

            case 4:
                display.text = Input.gyro.rotationRateUnbiased.magnitude.ToString("N4");
                break;

            case 5:
                display.text = "TODO";//Input.gyro.attitude.magnitude.ToString("%.4f");
                break;

            default:
                break;
        }

        
    }
}
