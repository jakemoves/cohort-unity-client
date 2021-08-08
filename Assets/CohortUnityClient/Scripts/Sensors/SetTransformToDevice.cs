using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTransformToDevice : MonoBehaviour
{
    public SensorManager manager;
    public bool accelerationEffects = true;
    // Start is called before the first frame update
    void Start()
    {
        if (manager == null)
        {
            Debug.LogError("Attatch Sensor Manager");
            this.enabled = false;
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.rotation = manager.Attitude;

        if (accelerationEffects) gameObject.transform.position = manager.GlobalAcceleration;
    }
}
