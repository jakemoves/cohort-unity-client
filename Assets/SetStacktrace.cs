using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetStacktrace : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
