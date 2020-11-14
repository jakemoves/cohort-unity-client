using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;
using UnityEngine.XR.Management;

public class CohortUIManager : MonoBehaviour
{
  TMPro.TextMeshProUGUI textCueDisplay;
  TMPro.TextMeshProUGUI statusDisplay;
  TMPro.TextMeshProUGUI textCueBackground;
  GameObject videoSurface;

  // Start is called before the first frame update
  void Start()
  {
    //StartXR();
    CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
    cohortSession.onTextCue += onTextCueHandler;
    cohortSession.onStatusChanged += onStatusUpdateHandler;

    textCueDisplay = GameObject.Find("Text Cue Display").GetComponent<TMPro.TextMeshProUGUI>();
    textCueDisplay.text = "";
    statusDisplay = GameObject.Find("Status Display").GetComponent<TMPro.TextMeshProUGUI>();

    textCueBackground = GameObject.Find("TextBack").GetComponent<TMPro.TextMeshProUGUI>();
    videoSurface = GameObject.Find("CohortVideoSurface");

		// ASL Video only, hide captions
		textCueDisplay.enabled = false;
		textCueBackground.enabled = false;

	}

  // Update is called once per frame
  void Update()
  {
        
  }

  void onTextCueHandler(CueAction cueAction, string cueText) {
    if(textCueBackground)
		{
			if (cueAction == CueAction.play)
			{
				textCueDisplay.text = cueText;
			}
			else if (cueAction == CueAction.stop)
			{
				textCueDisplay.text = "";
			}
		}
   
  }

  void onStatusUpdateHandler(string message) {
    statusDisplay.text = message;
  }

  public IEnumerator StartXR(){
	Debug.Log("Initializing XR...");
	yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

	if (XRGeneralSettings.Instance.Manager.activeLoader == null)
	{
		Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
	}
	else
	{
		Debug.Log("Starting XR...");
		XRGeneralSettings.Instance.Manager.StartSubsystems();
	}
  }

  void StopXR(){
	Debug.Log("Stopping XR...");

	XRGeneralSettings.Instance.Manager.StopSubsystems();
	XRGeneralSettings.Instance.Manager.DeinitializeLoader();
	Debug.Log("XR stopped completely.");
  }

  public void toggleCaptions()
	{
      textCueDisplay.enabled = !textCueDisplay.enabled;
      textCueBackground.enabled = textCueDisplay.enabled;
	}

  public void toggleVideo()
	{
        videoSurface.SetActive(!videoSurface.activeSelf);
	}
}
