using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("ENABLING POWER MANAGEMENT: 10fps, quarter rez");
        Application.targetFrameRate = 10;
        Screen.SetResolution(180, 284, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
