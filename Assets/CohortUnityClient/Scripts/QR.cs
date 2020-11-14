﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using UnityEngine.SceneManagement;

public class QR : MonoBehaviour
{

  private WebCamTexture camTexture;
  private Rect screenRect;
  static public string QRresults;
  Vector2 pivotPoint; 

  void Start()
  {
    screenRect = new Rect(0, 0, Screen.width, Screen.height);

    camTexture = new WebCamTexture();
    camTexture.requestedHeight = Screen.height;
    camTexture.requestedWidth = Screen.width;
    if (camTexture != null)
    {
      camTexture.Play();
    }
  }


  void OnGUI()
  {
    //rotate image
    pivotPoint = new Vector2(Screen.width / 2, Screen.height / 2);
    GUIUtility.RotateAroundPivot(180, pivotPoint);
    // drawing the camera on screen

    GUI.DrawTexture(screenRect, camTexture, ScaleMode.ScaleToFit);
    // do the reading — you might want to attempt to read less often than you draw on the screen for performance sake
    try
    {
      IBarcodeReader barcodeReader = new BarcodeReader();
      // decode the current frame
      var result = barcodeReader.Decode(camTexture.GetPixels32(),
        camTexture.width, camTexture.height);
      if (result != null)
      {
        Debug.Log("DECODED TEXT FROM QR: " +result.Text);
        QRresults = result.Text;
        PlayerPrefs.SetString("URL_from_QR", QRresults);
        SceneManager.LoadScene("CohortDemoScene");
      }
    }
    catch (Exception ex) { Debug.LogWarning(ex.Message); }
  }

}