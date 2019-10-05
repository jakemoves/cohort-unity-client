using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;
using BestHTTP;
using BestHTTP.WebSocket;
using LitJson;


namespace Cohort {
  public class CHSession : MonoBehaviour {

    [SerializeField]
    private string serverURL;

    [SerializeField]
    private int httpPort;

    [SerializeField]
    private string webSocketPath;

    [SerializeField]
    private int eventId;

    [SerializeField]
    private string pushN10nEndpoint;

    [SerializeField]
    private int clientOccasion;

    [SerializeField]
    private string clientTag;

    [SerializeField]
    private AudioSource audioPlayer;

    [SerializeField]
    private List<CHSoundCue> soundCues;

    [SerializeField]
    private VideoPlayer videoPlayer;

    [SerializeField]
    private GameObject fullscreenVideoSurface;

    [SerializeField]
    private List<CHVideoCue> videoCues;

    [SerializeField]
    private VideoClip nullVideo;

    [SerializeField]
    private TextMeshProUGUI textCueArea;

    [SerializeField]
    private List<CHTextCue> textCues;

    [SerializeField]
    private FlashlightController flashlightController;

    [SerializeField]
    private GameObject[] groupingUI;

    [SerializeField]
    private GameObject[] occasionUI;
    private string[] occasionIDs;

    [SerializeField]
    private TextMeshProUGUI groupingLabel;

    [SerializeField]
    private GameObject connectionIndicator;

    private CHRemoteNotificationSession remoteN10nSession;
    private WebSocket cohortSocket;
    private string deviceGUID; // eventually moves to CHDevice
    private int deviceID; // eventually moves to CHDevice
    private string grouping = "";
    private bool automaticCheckin;
    private int occasion;
    private string selectAShowCopy = "Select a show";

    private string checkingInPlaceholder = "CHECKING IN...";
    private string checkedInPlaceholder = "CHECKED IN";

    private bool socketConnectionActive = false;



    //public void OnBtn07_07Clicked() {
    //  grouping = "07_07";
    //  PlayerPrefs.SetString("cohortTag", grouping);
    //  HideGroupingUI();
    //  ShowOccasionUI();
    //  textCueArea.text = selectAShowCopy;
    //}

    //public void OnBtn22Clicked() {
    //  grouping = "22";
    //  PlayerPrefs.SetString("cohortTag", grouping);
    //  HideGroupingUI();
    //  ShowOccasionUI();
    //  textCueArea.text = selectAShowCopy;
    //}

    //public void OnBtn9_14Clicked() {
    //  grouping = "9-14";
    //  PlayerPrefs.SetString("cohortTag", grouping);
    //  HideGroupingUI();
    //  ShowOccasionUI();
    //  textCueArea.text = selectAShowCopy;
    //}

    //public void OnBtn1984Clicked() {
    //  grouping = "1984";
    //  PlayerPrefs.SetString("cohortTag", grouping);
    //  HideGroupingUI();
    //  ShowOccasionUI();
    //  textCueArea.text = selectAShowCopy;
    //}

    public void OnBtnOccasionAClicked() {
      PlayerPrefs.SetString("cohortOccasion", occasionIDs[0]);
      HideOccasionUI();
      LoadOccasionAndTag();
      textCueArea.text = "Loading...";
      SetDeviceTagAndCheckInToEvent(grouping, eventId, occasion);
    }

    public void OnBtnOccasionBClicked() {
      PlayerPrefs.SetString("cohortOccasion", occasionIDs[1]);
      HideOccasionUI();
      LoadOccasionAndTag();
      textCueArea.text = "Loading...";
      SetDeviceTagAndCheckInToEvent(grouping, eventId, occasion);
    }

    public void OnBtnOccasionCClicked() {
      PlayerPrefs.SetString("cohortOccasion", occasionIDs[2]);
      HideOccasionUI();
      LoadOccasionAndTag();
      textCueArea.text = "Loading...";
      SetDeviceTagAndCheckInToEvent(grouping, eventId, occasion);
    }

    void onVideoEnded(UnityEngine.Video.VideoPlayer source) {
      Debug.Log("video cue ended");
      fullscreenVideoSurface.SetActive(false);
    }

    // Use this for initialization
    void Start() {
      Debug.Log("CHSession:Start()");
      //DontDestroyOnLoad(transform.gameObject);
      if (!Application.isEditor) {
        deviceGUID = UnityEngine.iOS.Device.vendorIdentifier;
      } else {
        deviceGUID = "unity-editor-jn";
      }

      HideGroupingUI();
      HideOccasionUI();

      videoPlayer.loopPointReached += onVideoEnded;

      if(clientOccasion != 0 && clientTag != null){
        Debug.Log("Setting client details for testing: clientOccasion: " + clientOccasion + ", clientTag: " + clientTag);
        PlayerPrefs.SetString("cohortOccasion", clientOccasion.ToString());
        PlayerPrefs.SetString("cohortTag", clientTag);
      }

      bool occasionAndTagSet = LoadOccasionAndTag();
      if (occasionAndTagSet) {
        automaticCheckin = true;
      } else {
        automaticCheckin = false;
      }

      //if (PlayerPrefs.GetInt("registeredForNotifications", 0) == 1) {
      //  UpdateAndShowGroupingLabel();
      //  registerForRemoteNotifications();
      //  // get last text msg
      //  // display it
      //  return;
      //}

      // disabled for packaging
      //textCueArea.text = checkingInPlaceholder;

      /*
       * Create a new device on the server 
       */
      if (!string.IsNullOrEmpty(serverURL)) {


        System.UriBuilder devicesCreateURL = UriWithOptionalPort(httpPort, "api/v1/devices");
        Debug.Log(devicesCreateURL.Uri);
        HTTPRequest req = new HTTPRequest(
          devicesCreateURL.Uri,
          HTTPMethods.Post,
          (request, response) => {

            Debug.Log("req 1 complete");
            if (response.IsSuccess) {
              var device = JsonMapper.ToObject(response.DataAsText);
              this.deviceID = (int)device["id"]; // eventually moves to CHDevice
              this.grouping = this.deviceID.ToString();
              //UpdateAndShowGroupingLabel();
              Debug.Log("got ID");
              if (automaticCheckin) {
                Debug.Log("automatic checkin");
                SetDeviceTagAndCheckInToEvent(grouping, eventId, occasion);
              } else {
                //// show grouping selection UI
                //ShowGroupingUI();

                Debug.Log("manual occasion checkin");
                PlayerPrefs.SetString("cohortTag", grouping);
                ShowOccasionUI();
                //textCueArea.text = "Select your group ID code";
              }
            } else {
              Debug.Log("Error " + response.StatusCode + ": " + response.Message);

            }
          }
        );
        req.SetHeader("Content-Type", "application/json");
        string jsonToSend = "{ \"guid\": \"" + deviceGUID + "\"}";
        Debug.Log(jsonToSend);
        req.RawData = new System.Text.UTF8Encoding().GetBytes(jsonToSend);
        Debug.Log("sending req 1");
        req.Send();
      }
    }



    void SetDeviceTagAndCheckInToEvent(string deviceTag, int eventID, int occasionID) {
      System.UriBuilder setTagsURL = UriWithOptionalPort(httpPort, "api/v1/devices/" + deviceID + "/set-tags");
      HTTPRequest req = new HTTPRequest(
        setTagsURL.Uri,
        HTTPMethods.Patch,
        (request, response) => {
          Debug.Log("req 1.5 complete");
          if (response.IsSuccess) {
            CheckInToEventOccasion(eventID, occasionID);
          } else {
            Debug.Log("Error " + response.StatusCode + ": " + response.Message);
          }
        }
      );
      req.SetHeader("Content-Type", "application/json");
      string jsonToSend = "{ \"tags\": [ \"" + deviceTag + "\"] }";
      req.RawData = new System.Text.UTF8Encoding().GetBytes(jsonToSend);
      Debug.Log("sending req 1.5");
      req.Send();
    }

    void CheckInToEvent(int eventID) {
      System.UriBuilder eventCheckInURL = UriWithOptionalPort(httpPort, "api/v1/events/" + eventID + "/check-in");
      Debug.Log(eventCheckInURL.Uri);
      HTTPRequest req = new HTTPRequest(
        eventCheckInURL.Uri,
          HTTPMethods.Patch,
          (request, response) => {
            Debug.Log("req 2 complete");
            if (response.IsSuccess) {
              PlayerPrefs.SetInt("checkedIntoJacqueries", 1);
              textCueArea.text = checkedInPlaceholder;
              registerForRemoteNotifications();
              openWebSocketConnection();
            } else {
              Debug.Log("Error " + response.StatusCode + ": " + response.Message);
            }
          }
        );
      req.SetHeader("Content-Type", "application/json");
      string jsonToSend = "{ \"guid\": \"" + deviceGUID + "\"}";
      req.RawData = new System.Text.UTF8Encoding().GetBytes(jsonToSend);
      Debug.Log("sending req 2");
      req.Send();
    }

    // NB this is NOT the same as the above more general method
    void CheckInToEventOccasion(int eventID, int occasionID) {
      System.UriBuilder eventCheckInURL = UriWithOptionalPort(
        httpPort, 
        "api/v1/events/" + eventID + "/occasions/" + occasionID + "/check-in"
      );
      Debug.Log("check in URL: " + eventCheckInURL.Uri);
      HTTPRequest req = new HTTPRequest(
        eventCheckInURL.Uri,
          HTTPMethods.Patch,
          (request, response) => {
            Debug.Log("req 2 complete");
            if (response.IsSuccess) {
              PlayerPrefs.SetInt("checkedIntoJacqueries", 1);
              //UpdateAndShowGroupingLabel();

              if (!Application.isEditor) {
                //registerForRemoteNotifications();
                openWebSocketConnection();
              } else {
                //textCueArea.text = "CHECKED IN\n\n" + grouping + "\n\n(but not registered for notifications)";
                //UpdateAndShowGroupingLabel();
                openWebSocketConnection();
              }
            } else {
              Debug.Log("Error " + response.StatusCode + ": " + response.Message);
            }
          }
        );
      req.SetHeader("Content-Type", "application/json");
      string jsonToSend = "{ \"guid\": \"" + deviceGUID + "\"}";
      req.RawData = new System.Text.UTF8Encoding().GetBytes(jsonToSend);
      Debug.Log("sending req 2");
      req.Send();
    }

    void HandleOnRequestFinishedDelegate(HTTPRequest originalRequest, HTTPResponse response) {
    }

    /* 
     *   WebSockets
     */

    void openWebSocketConnection() {
      System.UriBuilder socketURL = (UriWithOptionalPort(httpPort, webSocketPath));
      Debug.Log(socketURL);
      cohortSocket = new WebSocket(UriWithOptionalPort(httpPort, webSocketPath).Uri);
      cohortSocket.OnOpen += OnWebSocketOpen;
      cohortSocket.OnMessage += OnWebSocketMessage;
      cohortSocket.OnClosed += OnWebSocketClosed;
      cohortSocket.OnErrorDesc += OnWebSocketErrorDescription;
      cohortSocket.Open();
    }

    void OnWebSocketOpen(WebSocket cs) {
      // this is a handshake with the server; the device must be registered (POST to /devices) and checked into an event (PATCH to /events/id/check-in) before trying to open a websocket connection
      CHSocketAuth msg = new CHSocketAuth();
      msg.guid = deviceGUID;
      msg.eventId = eventId;
      string message = JsonMapper.ToJson(msg);

      cs.Send(message);
    }

    void OnWebSocketMessage(WebSocket cs, string msg) {
      Debug.Log(msg);
      JsonData message = JsonMapper.ToObject(msg);
      Debug.Log(message);
      if (message.Keys.Contains("response")) {
        CHSocketSuccessResponse res = JsonMapper.ToObject<CHSocketSuccessResponse>(msg);
        Debug.Log(res);
        if (res.response == "success") {
          Debug.Log("opened websocket connnection");
          socketConnectionActive = true;
          //connectionIndicator.SetActive(true);
          //textCueArea.text = "READY";
        }
      } else
      // this is an ugly way to make sure it's a CHMessage, ugh
      if (message.Keys.Contains("mediaDomain")
         && message.Keys.Contains("cueNumber")
         && message.Keys.Contains("cueAction")) {
        CHMessage cohortMsg = JsonUtility.FromJson<CHMessage>(msg);
        //CHMessage cohortMsg = JsonMapper.ToObject<CHMessage>(msg);
        Debug.Log("received cohort message");
        Debug.Log(cohortMsg.mediaDomain);
        Debug.Log(cohortMsg.cueNumber);
        Debug.Log(cohortMsg.cueAction);
        OnCohortMessageReceived(cohortMsg);
      } else {
        Debug.Log("Warning: received non-Cohort message, taking no action");
        Debug.Log(message.Keys);
      }
    }

    void OnWebSocketClosed(WebSocket cs, ushort code, string msg) {
      Debug.Log("closed websocket connection");
      socketConnectionActive = false;
      //connectionIndicator.SetActive(false);
    }

    void OnWebSocketErrorDescription(WebSocket cs, string error) {
      Debug.Log("Error: WebSocket: " + error);
      socketConnectionActive = false;
      //connectionIndicator.SetActive(false);
    }

    /*
     *   Remote notifications 
     */

    void registerForRemoteNotifications(){
      /*
       * Register it for remote push notifications
       */

      string endpoint = pushN10nEndpoint.Replace(":id", this.deviceID.ToString());
      System.UriBuilder remoteN10nURL = UriWithOptionalPort(httpPort, endpoint);
      Debug.Log("push url: " + remoteN10nURL.Uri.ToString());

      string lastCohortMessageEndpoint = "api/v1/events/" + this.eventId + "/last-cohort-message";
      string queryForLastCohortMessage = "";//"?tag=" + grouping;
      System.UriBuilder lastCohortMessageURL = UriWithOptionalPort(httpPort, lastCohortMessageEndpoint, queryForLastCohortMessage);

      remoteN10nSession = new CHRemoteNotificationSession(
          remoteN10nURL.Uri,
          lastCohortMessageURL.Uri,
          deviceGUID,
          OnRemoteNotificationReceived,
          ValidateCohortMessage
      );

      remoteN10nSession.RegisteredForRemoteNotifications += OnRegisteredForRemoteNotifications;

    }

    // Update is called once per frame
    void Update() {
      // no callbacks so we have to set up our own observers...
      if (remoteN10nSession != null) {
        remoteN10nSession.Update();
      }
    }

    public void OnRegisteredForRemoteNotifications(bool result) {
      Debug.Log("CHSession: reg'd for remote n10n");
      PlayerPrefs.SetInt("registeredForNotifications", 1);
      if(textCueArea.text == checkingInPlaceholder || textCueArea.text == checkedInPlaceholder) {
        textCueArea.text = checkedInPlaceholder + "\n\n" + grouping;
      }
      //UpdateAndShowGroupingLabel();
    }

    void OnRemoteNotificationReceived(UnityEngine.iOS.RemoteNotification n10n) {
      Debug.Log("received remote notification in CHSession: ");
      Debug.Log("    " + n10n.alertTitle + ": " + n10n.alertBody);
      if (n10n.userInfo.Contains("cohortMessage")) {
        Debug.Log("n10n had a cohortMessage, processing");
        Hashtable msgHashtable = (Hashtable)n10n.userInfo["cohortMessage"];
        // process int64s
        msgHashtable["mediaDomain"] = System.Convert.ToInt32(msgHashtable["mediaDomain"]);
        msgHashtable["cueAction"] = System.Convert.ToInt32(msgHashtable["cueAction"]);

        ValidateCohortMessage(msgHashtable);
      } else {
        Debug.Log("notification had no cohortMessage, displaying text");
        // minor hack to mirror notification text in the text cue display area
        textCueArea.text = n10n.alertBody;
        textCueArea.gameObject.SetActive(true);
        UnityEngine.Handheld.Vibrate();
        if(n10n.soundName != "default.caf") {
          soundCues.ForEach(cue => {
            if(cue.audioClip.name == n10n.soundName) {
              audioPlayer.clip = cue.audioClip;
              audioPlayer.Play();
              return;
            }
          });
        }
      }
    }

    void ValidateCohortMessage(Hashtable msgHashtable) {
      CHMessage msg = new CHMessage();

      msg.cueNumber = System.Convert.ToSingle(msgHashtable["cueNumber"]);

      //Debug.Log("processing tags");
      ArrayList tagsFromMsgHashtable = (ArrayList)msgHashtable["targetTags"];
      List<string> tags = new List<string>();
      for (int i = 0; i < tagsFromMsgHashtable.Count; i++) {
        string oneTag = tagsFromMsgHashtable[i].ToString();
        tags.Add(oneTag);
      }
      Debug.Log(string.Join(", ", tags.ToArray()));
      msg.targetTags = tags;

      msg.mediaDomain = (MediaDomain)msgHashtable["mediaDomain"];

      msg.cueAction = (CueAction)msgHashtable["cueAction"];

      if (msgHashtable.ContainsKey("id")) {
        msg.id = (int)msgHashtable["id"];
      } else {
        msg.id = -1;
      }


      // hacks related to text cues and notifications

      if(System.Math.Abs(msg.cueNumber) < System.Single.Epsilon && msgHashtable.ContainsKey("cueContent")) {
        Debug.Log("creating extemporaneous text cue to match cohort message");
        CHTextCue extemporaneousTextCue = new CHTextCue((string)msgHashtable["cueContent"].ToString());
        float highestTextCueNumber = 0;
        foreach(CHTextCue cue in textCues) {
          if(cue.cueNumber > highestTextCueNumber) {
            highestTextCueNumber = cue.cueNumber;
          }
        }
        Debug.Log("highest text cue number: " + highestTextCueNumber);
        float newTextCueNumber = highestTextCueNumber + 1;
        extemporaneousTextCue.cueNumber = newTextCueNumber;
        msg.cueNumber = newTextCueNumber;
        textCues.Add(extemporaneousTextCue);
      }



      Debug.Log(CHMessage.FormattedMessage(msg));

      OnCohortMessageReceived(msg);
    }

    /*
     *   Cohort handlers
     */

    void OnCohortMessageReceived(CHMessage msg) {
      if(msg.id > 0){
        PlayerPrefs.SetInt("lastReceivedCohortMessageId", msg.id);
      }

      if(!msg.targetTags.Contains(grouping) && !msg.targetTags.Contains("all")) {
        Debug.Log("cohort message is for another grouping, not processing it");
        return;
      }

      // DO STUFF
      switch (msg.mediaDomain) {
        case MediaDomain.sound:
          CHSoundCue soundCue = soundCues.Find((CHSoundCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
          if (soundCue != null) {
            audioPlayer.clip = soundCue.audioClip;
          }

          // looped cues
          //if(Math.Abs(msg.cueNumber - 4) < Mathf.Epsilon) {
          //  if(msg.cueAction == CueAction.play) {
          //    audioPlayer.loop = true;
          //  } else {
          //    audioPlayer.loop = false;
          //  }
          //}

          switch (msg.cueAction) {
            case CueAction.play:
              audioPlayer.Play();
              break;
            case CueAction.pause:
              audioPlayer.Pause();
              break;
            case CueAction.restart:
              audioPlayer.Pause();
              audioPlayer.time = 0;
              audioPlayer.Play();
              break;
            case CueAction.stop:
              audioPlayer.Stop();
              audioPlayer.clip = null;
              break;
          }
          break;

        case MediaDomain.video:
          CHVideoCue videoCue = videoCues.Find((CHVideoCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
          if (videoCue != null) {
            if (videoPlayer.clip != videoCue.videoClip) {
              videoPlayer.Pause();
              videoPlayer.clip = videoCue.videoClip;
            }

            switch (msg.cueAction) {
              case CueAction.play:
                fullscreenVideoSurface.SetActive(true);
                videoPlayer.Play();
                break;
              case CueAction.pause:
                videoPlayer.Pause();
                break;
              case CueAction.restart:
                videoPlayer.Pause();
                videoPlayer.time = 0;
                videoPlayer.Play();
                break;
              case CueAction.stop:
                videoPlayer.Stop();
                videoPlayer.clip = nullVideo;
                videoPlayer.Play();
                break;
              default:
                Debug.Log("Error: cue action not implemented");
                break;
            }
            break;
          } else {
            Debug.Log("Error: cue number not valid");
          }
          break;
        

        case MediaDomain.text:
          CHTextCue textCue = textCues.Find((CHTextCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
          Debug.Log(textCue);
          if(textCue != null){
            textCueArea.text = textCue.text;
            UnityEngine.Handheld.Vibrate();
          }
          switch (msg.cueAction) {
            case CueAction.play:
              textCueArea.gameObject.SetActive(true);
              break;
            case CueAction.pause:
              Debug.Log("action 'pause' is not defined for text cues");
              break;
            case CueAction.restart:
              Debug.Log("action 'restart' is not defined for text cues");
              break;
            case CueAction.stop:
              textCueArea.gameObject.SetActive(false);
              break;
          }
          break;

        case MediaDomain.light:
          switch(msg.cueAction) {
            case CueAction.play:
              flashlightController.TurnOn();
              break;
            case CueAction.stop:
              flashlightController.TurnOff();
              break;
            case CueAction.pause:
              flashlightController.TurnOff();
              break;
            case CueAction.restart:
              Debug.Log("action 'restart' is not defined for light cues");
              break;
          }
          break;

        case MediaDomain.haptic:
          UnityEngine.Handheld.Vibrate();
          break;
      }
    }

    System.UriBuilder UriWithOptionalPort(int port, string endpoint, string query = ""){
      System.UriBuilder uri;
      if (port != null && port != 0) {
        uri = new System.UriBuilder(serverURL) {
          Port = port,
          Path = endpoint,
          Query = query
        };
      } else {
        uri = new System.UriBuilder(serverURL) {
          Path = endpoint,
          Query = query
        };
      }

      return uri;
    }

    void OnApplicationFocus(bool hasFocus) {
      Debug.Log("OnApplicationFocus: " + hasFocus);
      //connectionIndicator.SetActive(false);
      //openWebSocketConnection(); // this has issues -- it creates multiple sockets on the device, on the server

      if(hasFocus && remoteN10nSession.status == CHRemoteNotificationSession.Status.registeredForNotifications.ToString()) {
        remoteN10nSession.OnFocus();
      }
    }

    void ShowGroupingUI() {
      foreach (GameObject btn in groupingUI) {
        btn.SetActive(true);
      }
    }

    void HideGroupingUI() {
      foreach (GameObject btn in groupingUI) {
        btn.SetActive(false);
      }
    }

    void ShowOccasionUI() {
      string eventIdString = eventId.ToString();
      DateTime currentDate = DateTime.Now;
      string currentDateString = currentDate.ToString("yyyy-MM-dd");
      Debug.Log(currentDateString);

      System.UriBuilder getTodaysOccasionsURL = UriWithOptionalPort(
        httpPort,
        "api/v1/events/" + eventIdString + "/occasions/upcoming?onOrAfterDate=" + currentDateString
      );

      HTTPRequest req = new HTTPRequest(
        getTodaysOccasionsURL.Uri,
        HTTPMethods.Get,
        (request, response) => {
          Debug.Log("req for occasions complete");
          if (response.IsSuccess) {
            var results = JsonMapper.ToObject(response.DataAsText);
            Debug.Log(results);

            int occasionCount;
            if(results.Count <= 3){
              occasionCount = results.Count; 
            } else {
              occasionCount = 3;
            }

            occasionIDs = new string[occasionCount];

            for (int i = 0; i<occasionCount; i++) {
              JsonData occasionForToday = results[i];
              System.DateTime occasionStartTime = System.DateTime.Parse((string)occasionForToday["startDateTime"]);
              string showDateTime = occasionStartTime.ToString("MMM d h:mmtt");
              UnityEngine.UI.Text btnLabel = occasionUI[i].GetComponentInChildren<UnityEngine.UI.Text>();
              btnLabel.text = showDateTime;
              int id = (int)occasionForToday["id"];
              Debug.Log(id);
              occasionIDs[i] = id.ToString();
              Debug.Log(occasionIDs[i]);
              occasionUI[i].SetActive(true);
            }
          } else {
            Debug.Log("Error " + response.StatusCode + ": " + response.Message);
          }
        }
      );
      Debug.Log("sending req for occasions");
      req.Send();

    }

    void HideOccasionUI() {
      foreach (GameObject btn in occasionUI) {
        btn.SetActive(false);
      }
    }

    void UpdateAndShowGroupingLabel() {
      groupingLabel.text = this.grouping;
      groupingLabel.gameObject.SetActive(true);
    }

    bool LoadOccasionAndTag() {
      string occasionPref = PlayerPrefs.GetString("cohortOccasion", "");
      string tagPref = PlayerPrefs.GetString("cohortTag", "");

      if (occasionPref != "") {
        // convert to int
        int occasionInt = 0;
        if (!int.TryParse(occasionPref, out occasionInt)) {
          occasionInt = -1;
        }
        if (occasionInt != -1) {
          occasion = occasionInt;
        } else {
          Debug.Log("Error: failed to parse occasion as integer");
          return false;
        }
      } else {
        Debug.Log("Error: Cannot check in without an occasion");
        return false;
      }

      if (tagPref != "") {
        //grouping = tagPref;
        return true;
      } else {
        return false;
      }

    }
  }

  public class CHDeviceCreateResponse {
    public string guid;
  }

  public class CHSocketSuccessResponse {
    public string response;
  }

  public class CHSocketAuth {
    public string guid;
    public int eventId;
  }

}
