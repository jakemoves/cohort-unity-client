using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepScreenOn : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void OnEnable()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void OnDisable()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
