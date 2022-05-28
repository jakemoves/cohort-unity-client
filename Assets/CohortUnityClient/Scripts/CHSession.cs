// Copyright Jacob Niedzwiecki, Nicole Goertzen 2020
// Released under the MIT License (see /LICENSE)

using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
// NOTE: Best HTTP is an old dependancy
using BestHTTP;
using BestHTTP.WebSocket;
// NOTE: This could be replaced if necessary
using LitJson;
using UnityEditor;
using UnityEngine.Networking;

namespace Cohort
{
    // TODO: Events for websocket connection
    // TODO: Reconnection (SignalR?, Robust Websockets?)

    public class CHSession : MonoBehaviour
    {
        /*
         * Editor fields
         */

        // This points to a Cohort server. The Unity editor serializes cuelists (and other Cohort objects) and sends them to the server.
        [Header("Server Info")]
        [SerializeField]
        //address is stored in player prefs from previous scene - i.e. The QR Code Scanner
        //set this to false if only working within the cohortDemoScene
        private bool useStoredAddress;

        [SerializeField]
        private string serverURL;

        [SerializeField]
        // Use '0' for prodution; use local host port for local development
        private int httpPort;

        [Header("Authentication")]
        [SerializeField]
        private string username = "MeetMe";

        [SerializeField]
        private string password = "MeetMe123";

        [Header("Event Info")]
        // this points to a specific production (Event) and performance (Occasion) on a Cohort server
        [SerializeField]
        private int eventId;

        [SerializeField]
        private int clientOccasion;

        //[SerializeField]
        // useful when testing receiving cues in the Unity Editor — probably outdated with the addition of ShowGraphSession
        //private string clientGrouping;

        [Header("Audio Cues")]

        [SerializeField]
        public List<CHSoundCue> soundCues;

        [Header("Video Cues")]

        [SerializeField]
        public List<CHVideoCue> videoCues;

        [Header("Image Cues")]

        [SerializeField]
        public List<CHImageCue> imageCues;

        [Header("Text Cues")]

        [SerializeField]
        public List<CHTextCue> textCues;

        [Header("Ordered Cuelist")]

        //[SerializeField]
        //private List<CueReference> orderedAssets;

        [Header("Settings")]

        [SerializeField]
        private AudioSource audioPlayer;

        [SerializeField]
        private VideoPlayer videoPlayer;

        [SerializeField]
        private UnityEngine.UI.Image imageCueSurface;

        [Header("Group Settings")]

        [SerializeField]
        // TODO: REMOVE
        private UnityEngine.UI.Dropdown groupSelectorDropdown;

        [field: SerializeField]

        // TODO: REMOVE
        public string[] Groups { get; set; } = null;

        /*
         * Events
         */

        public delegate void OnTextCue(CueAction cueAction, string cueContent);
        public event OnTextCue onTextCue;

        public delegate void OnImageCue(CueAction cueAction, Sprite sprite);
        public event OnImageCue onImageCue;

        public delegate void OnStatusChanged(string statusUpdate); // mostly for logging / debugging
        public event OnStatusChanged onStatusChanged;

        /*
         * public methods
         */

        public string getDeviceGUID()
        {
            return deviceGUID;
        }

        public string GetCurrentGrouping()
        {
            // TODO: 
            if (Groups.Length == 0 || groupSelectorDropdown == null)
                return "all";

            return Groups[groupSelectorDropdown.value];
        }

        private string webSocketPath = "/sockets";
        private VideoClip nullVideo;
        private WebSocket cohortSocket;
        private string deviceGUID; // eventually moves to CHDevice
        private int deviceID; // eventually moves to CHDevice
        private string grouping = ""; // TODO: Remove?
        private bool automaticCheckin;
        private int occasion;
        private string selectAShowCopy = "Select a show";

        private string checkingInPlaceholder = "CHECKING IN...";
        private string checkedInPlaceholder = "CHECKED IN";

        private bool socketConnectionActive = false;

        private string jwtToken = "";
        //private UnityWebRequest cohortUpdateEventRequest;

        private List<CHEpisode> episodesArray;

        private string episodeJson;
        private Boolean successfulServerRequest;
        private string cachedUsername;

        //get url from QR code
        private string URL_from_QR;

        // for control bar
        private int currentAssetIndex = 0;

        // For filtered Assets
        //private List<CueReference> filteredOrderedAssets;

        //private List<CueReference> FilteredOrderedAssets
        //{
        //    get
        //    {
        //        if (filteredOrderedAssets == null)
        //            filteredOrderedAssets = orderedAssets;
        //        return filteredOrderedAssets;
        //    }

        //    set => filteredOrderedAssets = value;
        //}

        //private int currentFilteredAssetsIndex = 0;

        public class QRurl
        {
            public string scheme { get; set; }
            public string host { get; set; }
            public string occasionID { get; set; }
        }

        QRurl parseQrUrl(string url)
        {
            QRurl parsedUrl = new QRurl();
            Uri URL = new Uri(url);
            parsedUrl.host = URL.Host;
            parsedUrl.scheme = URL.Scheme;
            parsedUrl.occasionID = URL.PathAndQuery;
            Debug.Log(parsedUrl);
            return parsedUrl;

        }

        int findOccasionIdInUrl(string path)
        {
            string[] pathElements = path.Split('/');
            int id;
            int.TryParse(pathElements[3], out id);
            return id;
        }

        [Serializable]
        public class JwtToken
        {
            public string jwt;
        }

        [Serializable]
        public class CHEpisode
        {
            public int episodeNumber { get; set; }
            public string label { get; set; }
            public List<CHMessage> cues { get; set; }
        }

        string cohortApiUrl(string url)
        {
            // This does url parsing without System.Uri
            // it’s handy to know if we’re pointing at a local or remote URL

            //this checks for any of the following words/letter sequences, but I'm not sure how to use /(localhost|.local|192.168.)/mi in this context
            string urlInput = "(localhost|.local|192.168.)";
            string cohortUpdatedURL;

            // Instantiate the regular expression objects.
            Regex compareUrl = new Regex(urlInput, RegexOptions.IgnoreCase);

            // Match the regular expression pattern against our URL string.
            Match matchUrl = compareUrl.Match(url);

            //if match occurs add port number
            if (matchUrl.Length > 0)
            {
                cohortUpdatedURL = url + ":" + httpPort + "/api/v2";
            }
            else
            {
                cohortUpdatedURL = url + "/api/v2";
            }
            return cohortUpdatedURL;
        }

        void serverUrlFromPlayerPefs(bool usePlayerPrefs)
        {
            // Used to retrieve a URL from a scanned QR code.
            if (usePlayerPrefs)
            {
                URL_from_QR = PlayerPrefs.GetString("URL_from_QR", " ");

                QRurl brokenUpQrUrl = parseQrUrl(URL_from_QR);
                if (URL_from_QR != " ")
                {
                    serverURL = brokenUpQrUrl.scheme + "://" + brokenUpQrUrl.host;
                    clientOccasion = findOccasionIdInUrl(brokenUpQrUrl.occasionID);
                }
            }
            else
            {
                return;
            }
        }

        private void OnEnable()
        {
            serverUrlFromPlayerPefs(useStoredAddress);

        }

        void OnValidate()
        {
            //Debug.Log("OnValidate");
            //if true use stored server url, if false use editor url
            serverUrlFromPlayerPefs(useStoredAddress);

            //first check if we need to authenticate
            if (jwtToken == "" || cachedUsername != username)
            {
                Debug.Log("Logging into Cohort...");
                Credentials userCredentials = new Credentials();
                userCredentials.username = username;
                userCredentials.password = password;
                string loginJson = JsonMapper.ToJson(userCredentials);

                StartCoroutine(authenticationRequest(cohortApiUrl(serverURL), loginJson));
            }
            else
            {
                //uncomment to verify validate function is running when editor gets updated
                //Debug.Log("OnValidate");
                //uncomment to verify Json getting sent
                //Debug.Log(jsonFromCues());

                successfulServerRequest = false;
                string cuesJson = jsonFromCues();

                StartCoroutine(updateRemoteInfo(cohortApiUrl(serverURL), cuesJson));
            }
        }

        IEnumerator authenticationRequest(string uri, string json)
        {
            using (UnityWebRequest cohortLoginRequest = UnityWebRequest.Put(uri + "/login?sendToken=true", json))
            {
                cohortLoginRequest.SetRequestHeader("Content-Type", "application/json");
                cohortLoginRequest.method = "POST";
                // Request and wait for the desired page.
                yield return cohortLoginRequest.SendWebRequest();

                if (cohortLoginRequest.isNetworkError)
                {
                    Debug.Log("Error: " + cohortLoginRequest.error);

                }
                else if (cohortLoginRequest.isHttpError || cohortLoginRequest.responseCode != 200)
                {

                    //Editor messages can be created in a custom editor with a line like below
                    //EditorGUILayout.HelpBox(cohortLoginRequest.error, MessageType.Warning);
                    //if (Application.isEditor){
                    //  Debug.Log("Incredibly the credentials lack credibility. Please double check spelling and letter case.");
                    //}

                    Debug.Log("Error: " + cohortLoginRequest.downloadHandler.text + " (Error code " + cohortLoginRequest.responseCode + ")");

                }
                else
                {
                    // happy path - we got a token from the server
                    //Debug.Log("Received: " + cohortLoginRequest.downloadHandler.text);
                    JwtToken serverToken = new JwtToken();
                    serverToken = JsonUtility.FromJson<JwtToken>(cohortLoginRequest.downloadHandler.text);
                    Debug.Log("Login successful");
                    jwtToken = serverToken.jwt;
                    cachedUsername = username;
                }
            }
        }

        IEnumerator updateRemoteInfo(string uri, string json)
        {
            using (UnityWebRequest cohortUpdateEventRequest = UnityWebRequest.Put(uri + "/events/" + eventId + "/episodes", json))
            {
                cohortUpdateEventRequest.SetRequestHeader("Content-Type", "application/json");
                cohortUpdateEventRequest.SetRequestHeader("Authorization", "JWT " + jwtToken);
                cohortUpdateEventRequest.method = "POST";
                // Request and wait for the desired page.
                yield return cohortUpdateEventRequest.SendWebRequest();

                if (cohortUpdateEventRequest.isNetworkError)
                {
                    Debug.Log("Error: " + cohortUpdateEventRequest.error);

                }
                else if (cohortUpdateEventRequest.isHttpError || cohortUpdateEventRequest.responseCode != 200)
                {
                    Debug.Log("Error: " + cohortUpdateEventRequest.downloadHandler.text + " (code " + cohortUpdateEventRequest.responseCode + ")");
                }
                else
                {
                    //if package returned from the server successfully
                    //Debug.Log("Received: " + cohortUpdateEventRequest.downloadHandler.text);
                    successfulServerRequest = true;
                    Debug.Log("Save complete");
                }

            }
        }

        string jsonFromCues()
        {

            //setting up a new episode
            CHEpisode episode = new CHEpisode();
            episode.episodeNumber = 0;
            episode.label = "Act 2";
            episode.cues = new List<CHMessage>();

            //adding all the cues to episode.cues
            soundCues.ForEach(cue =>
            {
                CHMessage soundCue = new CHMessage();
                CHMessage cueDetails = soundCue.FromSoundCue(cue);
                episode.cues.Add(cueDetails);
            });


            videoCues.ForEach(cue =>
            {
                CHMessage videoCue = new CHMessage();
                CHMessage cueDetails = videoCue.FromVideoCue(cue);
                episode.cues.Add(cueDetails);
            });

            imageCues.ForEach(cue =>
            {
                CHMessage imageCue = new CHMessage();
                CHMessage cueDetails = imageCue.FromImageCue(cue);
                episode.cues.Add(cueDetails);
            });
            //server requires an array of episodes
            episodesArray = new List<CHEpisode>();
            episodesArray.Add(episode);

            //convert episode to JSON
            return JsonMapper.ToJson(episodesArray);
        }

        void onVideoEnded(UnityEngine.Video.VideoPlayer source)
        {
            Debug.Log("video cue ended");
            videoPlayer.clip = null;
        }

        // Use this for initialization
        void Start()
        {
            if (!Groups.Contains("all"))
                Groups = (new string[] { "all" }).Concat(Groups).ToArray();

            Debug.Log(URL_from_QR);


            Debug.Log("CHSession:Start()");
            Debug.Log(jwtToken);

            // Universal links
            Application.deepLinkActivated += handleDeepLinkEvent;

            //DontDestroyOnLoad(transform.gameObject);
            if (!Application.isEditor)
            {
#if UNITY_IOS
        deviceGUID = UnityEngine.iOS.Device.vendorIdentifier;
#endif
#if UNITY_ANDROID
                deviceGUID = UnityEngine.SystemInfo.deviceUniqueIdentifier; // this should get hashed, we don't need to track it
#endif
            }
            else
            {
                deviceGUID = "unity-editor-jn";
            }

            if (clientOccasion != 0)
            {
                var clientGrouping = GetCurrentGrouping();
                Debug.Log("Setting client details for testing: clientOccasion: " + clientOccasion + ", clientGrouping: " + clientGrouping);
                PlayerPrefs.SetString("cohortOccasion", clientOccasion.ToString());

                if (clientGrouping != null && clientGrouping != "")
                {
                    PlayerPrefs.SetString("cohortGrouping", clientGrouping);
                }
            }

            bool occasionAndGroupingSet = LoadOccasion();
            if (occasionAndGroupingSet)
            {
                automaticCheckin = true;
            }
            else
            {
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
            if (!string.IsNullOrEmpty(serverURL))
            {
                openWebSocketConnection();
            }

            // for control bar
            Debug.Log("Logging into Cohort...");
            Credentials userCredentials = new Credentials();
            userCredentials.username = username;
            userCredentials.password = password;
            string loginJson = JsonMapper.ToJson(userCredentials);

            StartCoroutine(authenticationRequest(cohortApiUrl(serverURL), loginJson));
        }

        void HandleOnRequestFinishedDelegate(HTTPRequest originalRequest, HTTPResponse response)
        {
        }

        void handleDeepLinkEvent(string url)
        {
            Debug.Log("deep link");
            Debug.Log(url);
            onStatusChanged("received URL from universal (deep) link: " + url);

            System.UriBuilder newServerURL;
            try
            {
                newServerURL = new System.UriBuilder(url);
            }
            catch (FormatException ex)
            {
                Debug.Log("deep link url parse failed");
                Debug.Log(ex);
                return;
            }
            parseDeeplinkURL(newServerURL);
        }

        void parseDeeplinkURL(UriBuilder newServerURL)
        {
            Debug.Log(newServerURL.Uri.ToString());
            string pathString = newServerURL.Path;

            Debug.Log(pathString);

            if (pathString.Substring(0, 1) == "/")
            {
                pathString = pathString.Substring(1);
            }

            string[] pathComponents = pathString.Split('/');

            if (pathComponents[0] == "join" && pathComponents.Length == 3)
            {
                Debug.Log("deep link is a join link");
                End();
                serverURL = "" + newServerURL.Scheme + "://" + newServerURL.Host;

                if (newServerURL.Port != -1)
                {
                    httpPort = newServerURL.Port;
                }

                // convert occasion id to int
                int occasionInt = 0;
                if (!int.TryParse(pathComponents[2], out occasionInt))
                {
                    occasionInt = -1;
                }
                if (occasionInt != -1)
                {
                    //if(occasionInt == occasion && socketConnectionActive) {
                    //  Debug.Log("Already connected to occasion:" + occasion);
                    //  return;
                    //} // this caused issues because socketConnectionActive is not always correct (esp after switching away and back to app
                    occasion = occasionInt; // <-- happy path 
                }
                else
                {
                    Debug.Log("Error: failed to parse occasion as integer");
                    return;
                }

                PlayerPrefs.SetString("cohortOccasion", occasion.ToString());
                openWebSocketConnection();


                /*
                 * Parse and set grouping (optional)
                 */

                string queryString = newServerURL.Query;

                if (queryString != null && queryString != "")
                {
                    queryString = queryString.Replace("?", "");
                    string[] queryParams = queryString.Split('&');
                    foreach (string parameterPair in queryParams)
                    {
                        string[] param = parameterPair.Split('=');
                        if (param[0] == "grouping")
                        {
                            Debug.Log("join URL included a grouping (" + param[1] + ")  ");
                            grouping = param[1]; // TODO: Check if we need to keep this.
                            PlayerPrefs.SetString("cohortGrouping", grouping);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("deep link does not match 'join' format");
                Debug.Log(pathString);
            }
        }

        void End()
        {
            if (cohortSocket != null)
            {
                cohortSocket.Close();
            }
        }

        /* 
         *   WebSockets
         */

        void openWebSocketConnection()
        {
            System.UriBuilder socketURL = (UriWithOptionalPort(httpPort, webSocketPath));
            Debug.Log(socketURL);
            cohortSocket = new WebSocket(UriWithOptionalPort(httpPort, webSocketPath).Uri);
            cohortSocket.OnOpen += OnWebSocketOpen;
            cohortSocket.OnMessage += OnWebSocketMessage;
            cohortSocket.OnClosed += OnWebSocketClosed;
            cohortSocket.OnErrorDesc += OnWebSocketErrorDescription;
            cohortSocket.Open();
        }

        void OnWebSocketOpen(WebSocket cs)
        {
            // this is a handshake with the server
            Debug.Log("socket open, sending handshake");
            CHSocketAuth msg = new CHSocketAuth();
            msg.guid = deviceGUID;
            msg.occasionId = occasion;
            string message = JsonMapper.ToJson(msg);
            Debug.Log(message);

            cs.Send(message);
        }

        void OnWebSocketMessage(WebSocket cs, string msg)
        {
            Debug.Log(msg);
            JsonData message = JsonMapper.ToObject(msg);
            Debug.Log(message);
            if (message.Keys.Contains("response"))
            {
                CHSocketSuccessResponse res = JsonMapper.ToObject<CHSocketSuccessResponse>(msg);
                Debug.Log(res);
                if (res.response == "success")
                {
                    Debug.Log("opened websocket connnection");
                    string groupingStatus = ""; // TODO: Check if this is neccessary?
                    if (grouping != null & grouping != "")
                    {
                        groupingStatus = ", grouping: " + grouping;
                    }
                    onStatusChanged("Connected to Cohort (occasion id:" + occasion + groupingStatus + ")");
                    socketConnectionActive = true;
                    //connectionIndicator.SetActive(true);
                }
            }
            else
            // this is an ugly way to make sure it's a CHMessage, ugh
            if (message.Keys.Contains("mediaDomain")
               && message.Keys.Contains("cueNumber")
               && message.Keys.Contains("cueAction"))
            {
                CHMessage cohortMsg = JsonUtility.FromJson<CHMessage>(msg);
                //CHMessage cohortMsg = JsonMapper.ToObject<CHMessage>(msg);
                Debug.Log("received cohort message");
                Debug.Log(cohortMsg.mediaDomain);
                Debug.Log(cohortMsg.cueNumber);
                Debug.Log(cohortMsg.cueAction);
                OnCohortMessageReceived(cohortMsg);
            }
            else
            {
                Debug.Log("Warning: received non-Cohort message, taking no action");
                Debug.Log(message.Keys);
            }
        }

        void OnWebSocketClosed(WebSocket cs, ushort code, string msg)
        {
            Debug.Log("closed websocket connection, code: " + code.ToString() + ", reason: " + msg);
            socketConnectionActive = false;
            onStatusChanged("Lost connection. Error code: " + code.ToString() + ", reason: " + msg);
            //connectionIndicator.SetActive(false);
        }

        void OnWebSocketErrorDescription(WebSocket cs, string error)
        {
            Debug.Log("Error: WebSocket: " + error);
            socketConnectionActive = false;
            onStatusChanged("Lost connection. WebSocket error: " + error);
            //connectionIndicator.SetActive(false);
        }

        /*
         *   Remote notifications 
         */

        //void registerForRemoteNotifications(){
        //  /*
        //   * Register it for remote push notifications
        //   */

        //string endpoint = pushN10nEndpoint.Replace(":id", this.deviceID.ToString());
        //System.UriBuilder remoteN10nURL = UriWithOptionalPort(httpPort, endpoint);
        //Debug.Log("push url: " + remoteN10nURL.Uri.ToString());

        //string lastCohortMessageEndpoint = "api/v1/events/" + this.eventId + "/last-cohort-message";
        //string queryForLastCohortMessage = "";//"?tag=" + grouping;
        //System.UriBuilder lastCohortMessageURL = UriWithOptionalPort(httpPort, lastCohortMessageEndpoint, queryForLastCohortMessage);

        //remoteN10nSession = new CHRemoteNotificationSession(
        //    remoteN10nURL.Uri,
        //    lastCohortMessageURL.Uri,
        //    deviceGUID,
        //    OnRemoteNotificationReceived,
        //    ValidateCohortMessage
        //);

        //remoteN10nSession.RegisteredForRemoteNotifications += OnRegisteredForRemoteNotifications;

        //}

        // Update is called once per frame
        void Update()
        {
            // no callbacks so we have to set up our own observers...
            //if (remoteN10nSession != null) {
            //  remoteN10nSession.Update();
            //}
        }

        public void OnRegisteredForRemoteNotifications(bool result)
        {
            Debug.Log("CHSession: reg'd for remote n10n");
            PlayerPrefs.SetInt("registeredForNotifications", 1);
        }

        //void OnRemoteNotificationReceived(UnityEngine.iOS.RemoteNotification n10n) {
        //  Debug.Log("received remote notification in CHSession: ");
        //  Debug.Log("    " + n10n.alertTitle + ": " + n10n.alertBody);
        //  if (n10n.userInfo.Contains("cohortMessage")) {
        //    Debug.Log("n10n had a cohortMessage, processing");
        //    Hashtable msgHashtable = (Hashtable)n10n.userInfo["cohortMessage"];
        //    // process int64s
        //    msgHashtable["mediaDomain"] = System.Convert.ToInt32(msgHashtable["mediaDomain"]);
        //    msgHashtable["cueAction"] = System.Convert.ToInt32(msgHashtable["cueAction"]);

        //    ValidateCohortMessage(msgHashtable);
        //  } else {
        //    Debug.Log("notification had no cohortMessage, displaying text");
        //    // minor hack to mirror notification text in the text cue display area
        //    onTextCue(CueAction.play, n10n.alertBody);
        //    if(n10n.soundName != "default.caf") {
        //      soundCues.ForEach(cue => {
        //        if(cue.audioClip.name == n10n.soundName) {
        //          audioPlayer.clip = cue.audioClip;
        //          audioPlayer.Play();
        //          return;
        //        }
        //      });
        //    }
        //  }
        //}

        void ValidateCohortMessage(Hashtable msgHashtable)
        {
            CHMessage msg = new CHMessage();

            msg.cueNumber = System.Convert.ToSingle(msgHashtable["cueNumber"]);

            //Debug.Log("processing tags");
            ArrayList tagsFromMsgHashtable = (ArrayList)msgHashtable["targetTags"];
            List<string> tags = new List<string>();
            for (int i = 0; i < tagsFromMsgHashtable.Count; i++)
            {
                string oneTag = tagsFromMsgHashtable[i].ToString();
                tags.Add(oneTag);
            }
            Debug.Log(string.Join(", ", tags.ToArray()));
            msg.targetTags = tags;

            msg.mediaDomain = (MediaDomain)msgHashtable["mediaDomain"];

            msg.cueAction = (CueAction)msgHashtable["cueAction"];

            if (msgHashtable.ContainsKey("id"))
            {
                msg.id = (int)msgHashtable["id"];
            }
            else
            {
                msg.id = -1;
            }


            // hacks related to text cues and notifications

            if (System.Math.Abs(msg.cueNumber) < System.Single.Epsilon && msgHashtable.ContainsKey("cueContent"))
            {
                Debug.Log("creating extemporaneous text cue to match cohort message");
                CHTextCue extemporaneousTextCue = new CHTextCue((string)msgHashtable["cueContent"].ToString());
                float highestTextCueNumber = 0;
                foreach (CHTextCue cue in textCues)
                {
                    if (cue.cueNumber > highestTextCueNumber)
                    {
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

        void OnCohortMessageReceived(CHMessage msg)
        {
            if (msg.id > 0)
            {
                PlayerPrefs.SetInt("lastReceivedCohortMessageId", msg.id);
            }

            // TODO: On message recieved
            Debug.Log("current grouping: " + GetCurrentGrouping());
            if (GetCurrentGrouping() != "all" && !msg.targetTags.Contains(GetCurrentGrouping()) && !msg.targetTags.Contains("all"))
            {
                Debug.Log("cohort message is for another grouping (not " + GetCurrentGrouping() + "), not processing it");
                return;
            }

            // DO STUFF
            switch (msg.mediaDomain)
            {
                case MediaDomain.sound:
                    CHSoundCue soundCue = soundCues.Find((CHSoundCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
                    if (soundCue != null)
                    {
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

                    switch (msg.cueAction)
                    {
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
                    if (videoCue != null)
                    {
                        if (videoPlayer.clip != videoCue.videoClip)
                        {
                            videoPlayer.Pause();
                            videoPlayer.clip = videoCue.videoClip;
                        }

                        switch (msg.cueAction)
                        {
                            case CueAction.play:
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
                    }
                    else
                    {
                        Debug.Log("Error: cue number not valid");
                    }
                    break;

                case MediaDomain.image:
                    CHImageCue imageCue = imageCues.Find((CHImageCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
                    if (imageCue != null)
                    {
                        // Throw OnImageCue Event
                        onImageCue?.Invoke(msg.cueAction, imageCue.image);

                        switch (msg.cueAction)
                        {
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
                    }
                    else
                    {
                        Debug.Log("Error: cue number not valid");
                    }
                    break;

                case MediaDomain.text:
                    CHTextCue textCue = textCues.Find((CHTextCue matchingCue) => System.Math.Abs(matchingCue.cueNumber - msg.cueNumber) < 0.00001);
                    if (textCue == null && msg.cueContent == null)
                    {
                        return;
                    }
                    string textOfCue;

                    if (msg.cueContent != null)
                    {
                        //adding padding for backing in a kinda hacky way
                        textOfCue = " " + msg.cueContent + " ";
                    }
                    else if (textCue.text != null)
                    {
                        textOfCue = " " + textCue.text + " ";
                    }
                    else
                    {
                        Debug.Log("Error: Failed to find text for text cue in onboard text cues or in remote cue");
                        return;
                    }

                    switch (msg.cueAction)
                    {
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

                //case MediaDomain.light:
                //  switch(msg.cueAction) {
                //    case CueAction.play:
                //      flashlightController.TurnOn();
                //      break;
                //    case CueAction.stop:
                //      flashlightController.TurnOff();
                //      break;
                //    case CueAction.pause:
                //      flashlightController.TurnOff();
                //      break;
                //    case CueAction.restart:
                //      Debug.Log("action 'restart' is not defined for light cues");
                //      break;
                //  }
                //  break;

                case MediaDomain.haptic:
                    UnityEngine.Handheld.Vibrate();
                    break;
            }
        }

        System.UriBuilder UriWithOptionalPort(int port, string endpoint, string query = "")
        {
            System.UriBuilder uri;
            if (port != null && port != 0)
            {
                uri = new System.UriBuilder(serverURL)
                {
                    Port = port,
                    Path = endpoint,
                    Query = query
                };
            }
            else
            {
                uri = new System.UriBuilder(serverURL)
                {
                    Path = endpoint,
                    Query = query
                };
            }

            return uri;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log("OnApplicationFocus: " + hasFocus);
            //connectionIndicator.SetActive(false);
            //openWebSocketConnection(); // this has issues -- it creates multiple sockets on the device, on the server

            //if(hasFocus && remoteN10nSession.status == CHRemoteNotificationSession.Status.registeredForNotifications.ToString()) {
            //  remoteN10nSession.OnFocus();
            //}
        }

        bool LoadOccasion()
        {
            string occasionPref = PlayerPrefs.GetString("cohortOccasion", "");
            //string groupingPref = PlayerPrefs.GetString("cohortGrouping", "");

            if (occasionPref != "")
            {
                // convert to int
                int occasionInt = 0;
                if (!int.TryParse(occasionPref, out occasionInt))
                {
                    occasionInt = -1;
                }
                if (occasionInt != -1)
                {
                    occasion = occasionInt;
                }
                else
                {
                    Debug.Log("Error: failed to parse occasion as integer");
                    return false;
                }
            }
            else
            {
                Debug.Log("Error: Cannot check in without an occasion");
                return false;
            }

            //if (groupingPref != "") {
            //  grouping = groupingPref;
            //} else {
            //  return false;
            //}

            return true;
        }

        /* 
         *   for control bar 
         */

        //void updateControlBar()
        //{
        //    Button nextBtn = GameObject.Find("Next Asset").GetComponent<Button>();
        //    Button prevBtn = GameObject.Find("Prev Asset").GetComponent<Button>();

        //    if (currentFilteredAssetsIndex == FilteredOrderedAssets.Count - 1)
        //    {
        //        // disable next button
        //        nextBtn.interactable = false;
        //    }
        //    else
        //    {
        //        nextBtn.interactable = true;
        //    }
        //    if (currentFilteredAssetsIndex == 0)
        //    {
        //        // disable prev button
        //        prevBtn.interactable = false;
        //    }
        //    else
        //    {
        //        prevBtn.interactable = true;
        //    }

        //    string labelText = "";
        //    // set label to cue number + accessible alt.
        //    TextMeshProUGUI assetLabel = GameObject.Find("Current Asset Label").GetComponent<TextMeshProUGUI>();
        //    labelText = "" + FilteredOrderedAssets[currentFilteredAssetsIndex].mediaDomain + " cue " + FilteredOrderedAssets[currentFilteredAssetsIndex].cueNumber;

        //    string assetDescription = "";
        //    // add cue description
        //    if (FilteredOrderedAssets[currentFilteredAssetsIndex].mediaDomain == MediaDomain.sound)
        //    {
        //        assetDescription = soundCues.Find(cue => cue.cueNumber == FilteredOrderedAssets[currentFilteredAssetsIndex].cueNumber).accessibleAlternative;

        //    }
        //    else if (FilteredOrderedAssets[currentFilteredAssetsIndex].mediaDomain == MediaDomain.image)
        //    {
        //        assetDescription = imageCues.Find(cue => cue.cueNumber == FilteredOrderedAssets[currentFilteredAssetsIndex].cueNumber).accessibleAlternative;
        //    }

        //    labelText += ": " + assetDescription;
        //    assetLabel.text = labelText;
        //}

        //public void onNextAssetBtn()
        //{
        //    currentFilteredAssetsIndex++;
        //    updateControlBar();
        //}

        //public void onPrevAssetBtn()
        //{
        //    currentFilteredAssetsIndex--;
        //    updateControlBar();
        //}

        //public void onPlayAssetBtn()
        //{
        //    // send web request with auth, cue media domain, cue number, cue action = 0 
        //    onFireCurrentAsset(CueAction.play);
        //}

        //public void onStopAssetBtn()
        //{
        //    // send web request with auth, cue media domain, cue number, cue action = 3
        //    onFireCurrentAsset(CueAction.stop);
        //}

        //public void onFireCurrentAsset(CueAction cueAction)
        //{
        //    Debug.Log("onFireCurrentAsset");

        //    Debug.Log(jwtToken);
        //    Debug.Log(cachedUsername);

        //    successfulServerRequest = false;

        //    // build JSON payload from Cue object
        //    Cue assetCue = new Cue();
        //    assetCue.mediaDomain = FilteredOrderedAssets[currentFilteredAssetsIndex].mediaDomain;
        //    assetCue.cueNumber = FilteredOrderedAssets[currentFilteredAssetsIndex].cueNumber;
        //    assetCue.cueAction = cueAction;
        //    assetCue.targetTags = new List<string>() { groups[FilteredOrderedAssets[currentFilteredAssetsIndex].groupIndex] };

        //    string jsonCue = JsonMapper.ToJson(assetCue);

        //    Debug.Log(jsonCue);

        //    StartCoroutine(fireCue(cohortApiUrl(serverURL), jsonCue));
        //}
        public void FireCue(Cue cue)
        {
            Debug.Log("onFireCurrentAsset");

            Debug.Log(jwtToken);
            Debug.Log(cachedUsername);

            successfulServerRequest = false;

            // build JSON payload from Cue object

            string jsonCue = JsonMapper.ToJson(cue);

            Debug.Log(jsonCue);

            StartCoroutine(fireCue(cohortApiUrl(serverURL), jsonCue));
        }

        IEnumerator fireCue(string uri, string json)
        {
            // TODO: Exception Handling in this coroutine
            /*
             * error possibilities:
             *  - can’t connect to server
             *  - can connect to server but gets error response (i.e code 403, 500, etc)
             *  - fails to serialize cue
             */
            Debug.Log("fireCue");
            Debug.Log(uri);
            Debug.Log(json);

            using (UnityWebRequest cohortBroadcastRequest = UnityWebRequest.Put(uri + "/occasions/" + clientOccasion + "/broadcast", json))
            {
                cohortBroadcastRequest.SetRequestHeader("Content-Type", "application/json");
                cohortBroadcastRequest.SetRequestHeader("Authorization", "JWT " + jwtToken);
                cohortBroadcastRequest.method = "POST";
                // Request and wait for the desired page.
                yield return cohortBroadcastRequest.SendWebRequest();

                if (cohortBroadcastRequest.isNetworkError)
                {
                    Debug.Log("Error: " + cohortBroadcastRequest.error);

                }
                else if (cohortBroadcastRequest.isHttpError || cohortBroadcastRequest.responseCode != 200)
                {
                    Debug.Log("Error: " + cohortBroadcastRequest.downloadHandler.text + " (code " + cohortBroadcastRequest.responseCode + ")");
                }
                else
                {
                    //if package returned from the server successfully
                    Debug.Log("Received: " + cohortBroadcastRequest.downloadHandler.text);
                    successfulServerRequest = true;
                    Debug.Log("Save complete");
                }

            }
        }
    }

    public class CHDeviceCreateResponse
    {
        public string guid;
    }

    public class CHSocketSuccessResponse
    {
        public string response;
    }

    public class CHSocketAuth
    {
        public string guid;
        public int occasionId;
    }

    public struct Credentials
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public struct CueReference
    {
        public MediaDomain mediaDomain;
        public double cueNumber;
        public int groupIndex;
    }
}
