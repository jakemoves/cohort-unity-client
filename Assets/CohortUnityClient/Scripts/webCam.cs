using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class webCam : MonoBehaviour
{

  private bool camAvailable;
  private WebCamTexture backCam;
  private Texture backgroundTexture;

  public RawImage backgroundImage;
  public AspectRatioFitter fit;
    // Start is called before the first frame update
  void Start()
  {
    backgroundTexture = backgroundImage.texture;
    WebCamDevice[] devices = WebCamTexture.devices;

      if (devices.Length == 0) {
        Debug.Log("No camera present");
        camAvailable = false;
        return;
      }

      for(int i = 0; i < devices.Length; i++)
      {
        if (!devices[i].isFrontFacing)
        {
          backCam = new WebCamTexture(devices[i].name,Screen.width, Screen.height);

        }
        
      }

      if (backCam == null){
        Debug.Log("Unable to find back camera.");
        return;
        }

      backCam.Play();
      backgroundImage.texture = backCam;

      camAvailable = true;

    }

    // Update is called once per frame
    void Update()
    {
    if (!camAvailable)
    {
      return;
    }

    float ratio = (float)backCam.width / (float)backCam.height;
    fit.aspectRatio = ratio;

    float scaleY = backCam.videoVerticallyMirrored ? -1f : 1f;
    backgroundImage.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

    int orient = -backCam.videoRotationAngle;
    backgroundImage.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
        
    }
}
