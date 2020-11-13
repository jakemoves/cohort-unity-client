using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextCopy : MonoBehaviour
{
  public GameObject TextObject;
  private TMP_Text tmp;
  private TMP_Text thisTmp;
  private void Start()
  {
    tmp = TextObject.GetComponent<TMP_Text>();
    thisTmp = gameObject.GetComponent<TMP_Text>();
  }

  // Update is called once per frame
  void Update()
    {
    if (tmp.text != null || tmp.text != "")
    {
      thisTmp.text = "<style=textCue>" + tmp.text;
    }

}

}
