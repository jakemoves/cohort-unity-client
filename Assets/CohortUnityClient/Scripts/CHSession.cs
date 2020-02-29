// Copyright Jacob Niedzwiecki, 2020
// Released under the MIT License (see /LICENSE)

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using BestHTTP;
using BestHTTP.WebSocket;
using LitJson;
using UnityEditor;
using UnityEngine.Networking;


namespace Cohort
{
    public class CHSession : MonoBehaviour {

    /*
     * Editor fields
     */

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
    private UnityEngine.UI.Image imageCueSurface;

    [SerializeField]
    private List<CHImageCue> imageCues;

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

    /*
     * Events
     */

    public delegate void OnTextCue(CueAction cueAction, string cueContent);
    public event OnTextCue onTextCue;

    public delegate void OnStatusChanged(string statusUpdate); // mostly for logging / debugging
    public event OnStatusChanged onStatusChanged;

    /*
     * public methods
     */

    public string getDeviceGUID() {
      return deviceGUID;
    }

    private VideoClip nullVideo;
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

    private string cohortUpdateEventURL;
    private UnityWebRequest cohortUpdateEventRequest;
   

     void OnValidate()
        {
            Debug.Log("OnValidate");
            UpdateRemoteInfo();
        }
        void UpdateRemoteInfo()
        {
            Request();
            EditorApplication.update += EditorUpdate;
        }
        void Request()
        {
            cohortUpdateEventURL = serverURL;
            if(cohortUpdateEventURL == "http://localhost")
            {
                cohortUpdateEventURL = serverURL + ":" + httpPort + "/api/v2";
            }
            else
            {
                cohortUpdateEventURL = serverURL + "/api/v2";
            }
            cohortUpdateEventRequest = UnityWebRequest.Get(cohortUpdateEventURL);
            cohortUpdateEventRequest.SendWebRequest();
        }
        void EditorUpdate()
        {
            if (!cohortUpdateEventRequest.isDone)
            {
                return;
            }
            if (cohortUpdateEventRequest.isNetworkError)
            {
                Debug.Log(cohortUpdateEventRequest.error);
            }
            else
            {
                Debug.Log(cohortUpdateEventRequest.downloadHandler.text);
            }
            EditorApplication.update -= EditorUpdate;
        }



        void onVideoEnded(UnityEngine.Video.VideoPlayer source) {
      Debug.Log("video cue ended");
      if (fullscreenVideoSurface) { 
        fullscreenVideoSurface.SetActive(false);
      } else {
        videoPlayer.clip = null;
      }
    }

    // Use this for initialization
    void Start() {
        
      Debug.Log("CHSession:Start()");

      // Universal links
      Application.deepLinkActivated += handleDeepLinkEvent;

      //DontDestroyOnLoad(transform.gameObject);
      if (!Application.isEditor) {
        deviceGUID = UnityEngine.iOS.Device.vendorIdentifier;
      } else {
        deviceGUID = "unity-editor-jn";
      }

      if (clientOccasion != 0 && clientTag != null){
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

      // setup for specific cue types
      videoPlayer.loopPointReached += onVideoEnded;

      // this value is set to transparent in the editor to avoid showing a big white rectangle when the imageCueDisplay is empty
      // so we set it back to opaque here
      imageCueSurface.gameObject.SetActive(false);
      imageCueSurface.color = Color.white;

      /*
       * Connect to a Cohort occasion and start listening for cues
       */
      if (!string.IsNullOrEmpty(serverURL)) {
        openWebSocketConnection();
      }
    }

    void HandleOnRequestFinishedDelegate(HTTPRequest originalRequest, HTTPResponse response) {
    }

    void handleDeepLinkEvent(string url){
      Debug.Log("deep link");
      Debug.Log(url);
      onStatusChanged("received URL from universal (deep) link: " + url);

			System.UriBuilder newServerURL;
			try {
        newServerURL = new System.UriBuilder(url);
			}
			catch (FormatException ex){
        Debug.Log("deep link url parse failed");
        Debug.Log(ex);
        return;
			}
      parseDeeplinkURL(newServerURL);
    }

    void parseDeeplinkURL(UriBuilder newServerURL) { 
      Debug.Log(newServerURL.Uri.ToString());
      string pathString = newServerURL.Path;

      Debug.Log(pathString);

      if (pathString.Substring(0,1) == "/") {
        pathString = pathString.Substring(1);
      }

      string[] pathComponents = pathString.Split('/');

      if(pathComponents[0] == "join" && pathComponents.Length == 3) {
        Debug.Log("deep link is a join link");
        End();
        serverURL = "" + newServerURL.Scheme + "://" + newServerURL.Host;

        if(newServerURL.Port != -1){
          httpPort = newServerURL.Port;
        }

        // convert occasion id to int
        int occasionInt = 0;
        if (!int.TryParse(pathComponents[2], out occasionInt)) {
          occasionInt = -1;
        }
        if (occasionInt != -1) {
          if(occasionInt == occasion && socketConnectionActive) {
            Debug.Log("Already connected to occasion:" + occasion);
            return;
          }
          occasion = occasionInt;
        } else {
          Debug.Log("Error: failed to parse occasion as integer");
          return;
        }

        PlayerPrefs.SetString("cohortOccasion", occasion.ToString());
        openWebSocketConnection();

      } else {
        Debug.Log("deep link does not match 'join' format");
        Debug.Log(pathString);
      }
		}

    void End() {
      if(cohortSocket != null) {
        cohortSocket.Close();
      }
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
      // this is a handshake with the server
      Debug.Log("socket open, sending handshake");
      CHSocketAuth msg = new CHSocketAuth();
      msg.guid = deviceGUID;
      msg.occasionId = occasion;
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
          onStatusChanged("Connected to Cohort (occasion id:" + clientOccasion + ")");
          socketConnectionActive = true;
          //connectionIndicator.SetActive(true);
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
      Debug.Log("closed websocket connection, code: " + code.ToString() + ", reason: " + msg);
      socketConnectionActive = false;
      onStatusChanged("Lost connection. Error code: " + code.ToString() + ", reason: " + msg);
      //connectionIndicator.SetActive(false);
    }

    void OnWebSocketErrorDescription(WebSocket cs, string error) {
      Debug.Log("Error: WebSocket: " + error);
      socketConnectionActive = false;
      onStatusChanged("Lost connection. WebSocket error: " + error);
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
      //check to see if editor changes, if so update server
       OnValidate();
      // no callbacks so we have to set up our own observers...
      if (remoteN10nSession != null) {
        remoteN10nSession.Update();
      }
    }

    public void OnRegisteredForRemoteNotifications(bool result) {
      Debug.Log("CHSession: reg'd for remote n10n");
      PlayerPrefs.SetInt("registeredForNotifications", 1);
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
        onTextCue(CueAction.play, n10n.alertBody);
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
                if (fullscreenVideoSurface) {
                  fullscreenVideoSurface.SetActive(true);
                }
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

        case MediaDomain.image:
          CHImageCue imageCue = imageCues.Find((CHImageCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
          if(imageCue != null) {
            switch (msg.cueAction) { 
            case CueAction.play:
                Debug.Log("got image cue");
              imageCueSurface.sprite = imageCue.image;
                imageCueSurface.gameObject.SetActive(true);
              break;
            case CueAction.stop:
                imageCueSurface.gameObject.SetActive(false);
                imageCueSurface.sprite = null;
              break;
             default:
              Debug.Log("Error: cue action not implemented");
              break;
            }
          } else {
            Debug.Log("Error: cue number not valid");
          }
          break;

        case MediaDomain.text:
          CHTextCue textCue = textCues.Find((CHTextCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
          if(textCue == null && msg.cueContent == null){
            return;
          }
          string textOfCue;
          if(msg.cueContent != null){
            textOfCue = msg.cueContent;
          } else if(textCue.text != null) {
            textOfCue = textCue.text;
          } else {
            Debug.Log("Error: Failed to find text for text cue in onboard text cues or in remote cue");
            return;
          }

          switch (msg.cueAction) {
            case CueAction.play:
              UnityEngine.Handheld.Vibrate();
              onTextCue(CueAction.play, textOfCue);
              break;
            case CueAction.pause:
              Debug.Log("action 'pause' is not defined for text cues");
              break;
            case CueAction.restart:
              Debug.Log("action 'restart' is not defined for text cues");
              break;
            case CueAction.stop:
              onTextCue(CueAction.stop, textOfCue);
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
    public int occasionId;
  }

}