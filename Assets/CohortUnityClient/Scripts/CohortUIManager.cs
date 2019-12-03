using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;

public class CohortUIManager : MonoBehaviour
{
  TMPro.TextMeshProUGUI textCueDisplay;

  // Start is called before the first frame update
  void Start()
  {
      CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
      cohortSession.onTextCue += onTextCueHandler;
      //Debug.Log(cohortSession.getDeviceGUID());

      textCueDisplay = GameObject.Find("Text Cue Display").GetComponent<TMPro.TextMeshProUGUI>();
  }

  // Update is called once per frame
  void Update()
  {
        
  }

  void onTextCueHandler(CueAction cueAction, string cueText) {
    textCueDisplay.text = cueText;
  }
}
