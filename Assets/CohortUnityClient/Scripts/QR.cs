using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class QR : MonoBehaviour
{

  private bool camAvailable;
  private WebCamTexture backCam;
  private Texture backgroundTexture;
  private string QRresults;

  public RawImage backgroundImage;
  public AspectRatioFitter fit;
  public GameObject success;
  // Start is called before the first frame update
  void Start()
  {
    backgroundTexture = backgroundImage.texture;
    WebCamDevice[] devices = WebCamTexture.devices;

    if (devices.Length == 0)
    {
      Debug.Log("No camera present");
      camAvailable = false;
      return;
    }

    for (int i = 0; i < devices.Length; i++)
    {
      if (!devices[i].isFrontFacing)
      {
        backCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);

      }

    }

    if (backCam == null)
    {
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

    try
    {
      IBarcodeReader barcodeReader = new BarcodeReader();
      // decode the current frame
      var result = barcodeReader.Decode(backCam.GetPixels32(),
        backCam.width, backCam.height);
      if (result != null)
      {
        Debug.Log("DECODED TEXT FROM QR: " + result.Text);
        QRresults = result.Text;
        Uri uri = new Uri(QRresults);
        if (UrlHasOccasion(uri))
        {
          PlayerPrefs.SetString("URL_from_QR", QRresults);
          backCam.Stop();
          success.SetActive(true);
          SceneManager.LoadScene("CohortDemoScene");
        }
      }
    }
    catch (Exception ex) { Debug.LogWarning(ex.Message); }
  }

  bool UrlHasOccasion(Uri url)
  {
    string path = url.AbsolutePath;
    string urlInput = "(occasion)";

    // Instantiate the regular expression objects.
    Regex compareUrl = new Regex(urlInput, RegexOptions.IgnoreCase);

    // Match the regular expression pattern against our URL string.
    Match matchUrl = compareUrl.Match(path);

    return (matchUrl.Length > 0) ? true : false;
  }


}

