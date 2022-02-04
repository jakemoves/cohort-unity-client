using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;

public class CohortUIManager : MonoBehaviour
{
  TMPro.TextMeshProUGUI textCueDisplay;
  TMPro.TextMeshProUGUI statusDisplay;
  TMPro.TextMeshProUGUI textCueBackground;

  GameObject cohortUI;

  public TMPro.TextMeshPro toggleUiText;
  

  // Start is called before the first frame update
  void Start()
  {
    // TODO: @jacob
    // These wiill throw an Ststem.NullReferenceExeption if the
    // GameObjects are not named exactly what they are looking for OR if they are disabled 
    // I have enabled the "Text Cue Display" object and added a Try-Catch for the "TextBack"
    // to handle these exceptions for now.
    CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
    cohortSession.onTextCue += onTextCueHandler;
    cohortSession.onStatusChanged += onStatusUpdateHandler;

    textCueDisplay = GameObject.Find("Text Cue Display").GetComponent<TMPro.TextMeshProUGUI>();
    textCueDisplay.text = "";

    statusDisplay = GameObject.Find("Status Display").GetComponent<TMPro.TextMeshProUGUI>();

    try
    {
        textCueBackground = GameObject.Find("TextBack").GetComponent<TMPro.TextMeshProUGUI>();
    }
    catch (System.NullReferenceException)
    {
      Debug.LogWarning("Could not find the GameObject named \"TextBack\". (Note: The exception of this error has been handled)", this);
      textCueBackground = null;
    }

  }

  // Update is called once per frame
  void Update()
  {
        
  }

  void onTextCueHandler(CueAction cueAction, string cueText) {
    if(cueAction == CueAction.play) { 
      textCueDisplay.text = cueText;
    } else if(cueAction == CueAction.stop) {
      textCueDisplay.text = "";
    }
  }

  void onStatusUpdateHandler(string message) {
    statusDisplay.text = message;
  }

  public void toggleCaptions()
	{
      textCueDisplay.enabled = !textCueDisplay.enabled;

    // TODO: Nic added this because
    // this was causing errors as this object could not be found
    if (textCueBackground != null) 
      textCueBackground.enabled = textCueDisplay.enabled;
	}

  public void onShowUI(){
    cohortUI.SetActive(true);
  }

  public void onHideUI(){
    cohortUI = GameObject.Find("CohortUI");
    cohortUI.SetActive(false);
  }

  public void toggleUI()
  {
    if (cohortUI == null)
      cohortUI = GameObject.Find("CohortUI");

    string displayText;

    if (cohortUI.activeSelf)
    {
      onHideUI();
      displayText = "Show UI";
    }
    else
    {
      onShowUI();
      displayText = "Hide UI";
    }

    if (toggleUiText != null)
      toggleUiText.text = displayText;
  }
}
