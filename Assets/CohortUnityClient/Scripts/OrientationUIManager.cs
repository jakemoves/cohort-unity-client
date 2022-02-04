using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationUIManager : MonoBehaviour
{
  public GameObject PlayButton;
  public GameObject StopButton;

  // Start is called before the first frame update
  void Start()
  {
        
  }

  // Update is called once per frame
  void Update()
  {
    Debug.Log(Input.deviceOrientation);
    if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
    {
      if (PlayButton != null)
      {
        var position = PlayButton.transform.localPosition;
        PlayButton.transform.localPosition = new Vector3(0, 66, position.z);
      }

      if (StopButton != null)
      {
        var position = StopButton.transform.localPosition;
        StopButton.transform.localPosition = new Vector3(0, -9, position.z);
      }
    }
    else //if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft || Input.deviceOrientation == DeviceOrientation.LandscapeRight)
    {
      if (PlayButton != null)
      {
        var position = PlayButton.transform.localPosition;
        PlayButton.transform.localPosition = new Vector3(-160, 5, position.z);
      }

      if (StopButton != null)
      {
        var position = StopButton.transform.localPosition;
        StopButton.transform.localPosition = new Vector3(160, 5, position.z);
      }
    }
        
  }
}
