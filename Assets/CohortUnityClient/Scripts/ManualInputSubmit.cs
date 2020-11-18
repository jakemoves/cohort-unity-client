using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManualInputSubmit : MonoBehaviour
{
  public GameObject errorMessage;
  
  public void checkURL()
  {
    Text input = gameObject.GetComponent<Text>();
    string inputConverted = input.text;
    //this needs error handling in case user inputs invalid url structure
    Uri url = new Uri(inputConverted);

    if(UrlHasOccasion(url)){
      PlayerPrefs.SetString("URL_from_QR", input.text);
      SceneManager.LoadScene("CohortDemoScene");
    } else
    {
      //this error message needs to be more comprehensive but gives the user some indication something isn't right
      errorMessage.SetActive(true);
    }
  }

  //a more comprehensive check could be implemented if you know exactly which server will be targeted
  //for now we just check that it contains "occasion"
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
