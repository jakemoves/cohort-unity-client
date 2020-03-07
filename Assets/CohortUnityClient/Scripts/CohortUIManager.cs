using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;

public class CohortUIManager : MonoBehaviour
{
  TMPro.TextMeshProUGUI textCueDisplay;
  TMPro.TextMeshProUGUI statusDisplay;

  // Start is called before the first frame update
  void Start()
  {
    CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
    cohortSession.onTextCue += onTextCueHandler;
    cohortSession.onStatusChanged += onStatusUpdateHandler;

    textCueDisplay = GameObject.Find("Text Cue Display").GetComponent<TMPro.TextMeshProUGUI>();
    textCueDisplay.text = "";
    statusDisplay = GameObject.Find("Status Display").GetComponent<TMPro.TextMeshProUGUI>();
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
}
