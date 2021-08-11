using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;

public class CohortUIManager : MonoBehaviour
{
  TMPro.TextMeshProUGUI textCueDisplay;

  [SerializeField]
  public TMPro.TextMeshProUGUI statusDisplay;
  
  TMPro.TextMeshProUGUI textCueBackground;

  GameObject cohortUI;
  

  // Start is called before the first frame update
  void Start()
  {
    CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
    cohortSession.onTextCue += onTextCueHandler;
    cohortSession.onStatusChanged += onStatusUpdateHandler;

    textCueDisplay = GameObject.Find("Text Cue Display").GetComponent<TMPro.TextMeshProUGUI>();
    textCueDisplay.text = "";
    statusDisplay = GameObject.Find("Status Display").GetComponent<TMPro.TextMeshProUGUI>();

    textCueBackground = GameObject.Find("TextBack").GetComponent<TMPro.TextMeshProUGUI>();

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
      textCueBackground.enabled = textCueDisplay.enabled;
	}

  public void onShowUI(){
    cohortUI.SetActive(true);
  }

  public void onHideUI(){
    cohortUI = GameObject.Find("CohortUI");
    cohortUI.SetActive(false);
  }

  public void onSensorScene(){
    UnityEngine.SceneManagement.SceneManager.LoadScene("Sensors");
  }

}
