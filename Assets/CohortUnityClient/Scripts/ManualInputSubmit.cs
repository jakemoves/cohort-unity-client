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

    try
    {
      Uri url = new Uri(inputConverted);
      //check that url is well formatted and that it at least has "join/occasion" in its path
      if (UrlHasOccasion(url) && url.IsWellFormedOriginalString())
      {
        PlayerPrefs.SetString("URL_from_QR", input.text);
        SceneManager.LoadScene("CohortDemoScene");
      }
      else
      {
        throw new UriFormatException("Url not in expected format");
      }
      
     
    } catch (UriFormatException e)
    {
      errorMessage.SetActive(true);
      throw new UriFormatException ("Url not in expected format", e);
    }

  }

  //a more comprehensive check could be implemented if you know exactly which server will be targeted
  //for now we just check that it contains "join/occasion"
  bool UrlHasOccasion(Uri url)
  {
    string path = url.AbsolutePath;
    string urlInput = "(join/occasions)";

    // Instantiate the regular expression objects.
    Regex compareUrl = new Regex(urlInput, RegexOptions.IgnoreCase);

    // Match the regular expression pattern against our URL string.
    Match matchUrl = compareUrl.Match(path);

    return (matchUrl.Length > 0) ? true : false;
  }
}
