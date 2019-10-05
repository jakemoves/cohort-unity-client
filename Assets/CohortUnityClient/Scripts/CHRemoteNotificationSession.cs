using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotificationServices = UnityEngine.iOS.NotificationServices;
using NotificationType = UnityEngine.iOS.NotificationType;
using BestHTTP;
using LitJson;
using System;

namespace Cohort {
  public class CHRemoteNotificationSession {
    public event Action<bool> RegisteredForRemoteNotifications;
    public int remoteNotificationCount;
    public string status {
      get {
        return notificationStatus.ToString();
      }
    }

    private System.Uri pushServerURL;
    private System.Uri lastCohortMessageURL;
    private string deviceGUID;
    private int deviceID;
    private Status notificationStatus;
    private System.Action<UnityEngine.iOS.RemoteNotification> n10nReceivedCallback;
    private System.Action<Hashtable> validateCohortMessageCallback;

    public CHRemoteNotificationSession(
      System.Uri pushServerURL,
      System.Uri lastCohortMessageURL,
      string deviceGUID,
      System.Action<UnityEngine.iOS.RemoteNotification> onNotificationReceived,
      System.Action<Hashtable> validateCohortMessage
    ) {

      this.pushServerURL = pushServerURL;
      this.lastCohortMessageURL = lastCohortMessageURL;
      this.deviceGUID = deviceGUID;
      this.n10nReceivedCallback = onNotificationReceived;
      this.validateCohortMessageCallback = validateCohortMessage;

      notificationStatus = Status.initialized;
      Debug.Log("status: " + notificationStatus.ToString());
      remoteNotificationCount = 0;

      //if(PlayerPrefs.GetInt("registeredForNotifications", 0) == 0) {
      //  RegisterForRemotePushNotifications();
      //} else {
      //  notificationStatus = Status.registeredForNotifications;
      //  RegisteredForRemoteNotifications(true);
      //}
      RegisterForRemotePushNotifications();
      checkLastCohortMessage();
    }

    private void RegisterForRemotePushNotifications() {
      NotificationServices.RegisterForNotifications(
          NotificationType.Alert |
          NotificationType.Sound |
          NotificationType.Badge
      );
      notificationStatus = Status.waitingForDeviceTokenFromAPNS;
      Debug.Log("status: " + notificationStatus.ToString());
    }

    public void Update() {
      // below fires once Apple server provides the device token
      if (notificationStatus == Status.waitingForDeviceTokenFromAPNS) {
        byte[] token = NotificationServices.deviceToken;
        if (token != null) {
          notificationStatus = Status.receivedDeviceTokenFromAPNS;
          Debug.Log("status: " + notificationStatus.ToString());
        }
      }
      if (notificationStatus == Status.receivedDeviceTokenFromAPNS) {
        notificationStatus = Status.attemptingToRegisterForNotifications;
        Debug.Log("status: " + notificationStatus.ToString());
        Debug.Log("APNS token: " + System.BitConverter
                                    .ToString(NotificationServices.deviceToken)
                                    .Replace("-", ""));
        SubscribeToRemoteNotifications();
      }

      // fires when we receive a notification with the app open 
      // NB !!! not sure how this behaves if the app was closed
      if (notificationStatus == Status.registeredForNotifications) {
        if (NotificationServices.remoteNotifications.Length > remoteNotificationCount) {
          remoteNotificationCount++;
          Debug.Log("received remote notification in CHRemoteN10nSession");
          UnityEngine.iOS.RemoteNotification n10n = NotificationServices.GetRemoteNotification(
              NotificationServices.remoteNotifications.Length - 1);
          OnRemoteNotificationReceived(n10n);
        }
      }
    }

    private void SubscribeToRemoteNotifications() {
      string hexToken = System.BitConverter.ToString(NotificationServices.deviceToken)
                                  .Replace("-", "");

      RegisterForRemoteNotificationsRequest reqBody = new RegisterForRemoteNotificationsRequest();
      reqBody.token = hexToken;
      string reqString = JsonUtility.ToJson(reqBody);
      Debug.Log(reqString);

      Debug.Log("sending req 3");
      HTTPRequest req = new HTTPRequest(
          pushServerURL,
          HTTPMethods.Patch,
          (request, response) => {
            Debug.Log("req 3 complete");
            if (response.IsSuccess) {
              RegisteredForRemoteNotifications(true);
              notificationStatus = Status.registeredForNotifications;
              Debug.Log("status: " + notificationStatus.ToString());

              Debug.Log("n10n count: " + NotificationServices.remoteNotifications.Length);

            } else {
              notificationStatus = Status.receivedErrorOnRequest;
              Debug.Log("status: " + notificationStatus.ToString());
              Debug.Log(response.StatusCode + ": " + response.Message);
            }
          }
      );

      req.RawData = System.Text.Encoding.UTF8.GetBytes(reqString);
      req.AddHeader("Content-Type", "application/json");
      req.Send();
    }

    public void OnRemoteNotificationReceived(UnityEngine.iOS.RemoteNotification n10n) {
      // this local notification never appears, doesn't work at all
      UnityEngine.iOS.LocalNotification localn10n = new UnityEngine.iOS.LocalNotification();
      localn10n.alertBody = n10n.alertBody;
      localn10n.alertTitle = n10n.alertTitle;
      localn10n.soundName = n10n.soundName;
      localn10n.hasAction = true;
      localn10n.fireDate = System.DateTime.Now.AddSeconds(1);
      NotificationServices.ScheduleLocalNotification(localn10n);

      n10nReceivedCallback(n10n);
    }

    public void OnFocus(){
      Debug.Log("CHRemoteNotificationSession:OnFocus");
      checkLastCohortMessage();
    }

    void checkLastCohortMessage() {
      Debug.Log("checking last cohort message");
      int msgId;
      if (PlayerPrefs.HasKey("lastReceivedCohortMessageId")) {
        msgId = PlayerPrefs.GetInt("lastReceivedCohortMessageId");
      } else {
        msgId = -1;
      }

      HTTPRequest req = new HTTPRequest(
        lastCohortMessageURL,
        (request, response) => {
          if(response.IsSuccess){
            var jsonObj = JsonMapper.ToObject(response.DataAsText);

            if (jsonObj["id"] != null) {
              int remoteId = (int)jsonObj["id"];
              if (remoteId != msgId) {
                string messageTimestamp = (string)jsonObj["created_at"];
                System.DateTime cohortMessageSentTimestamp = System.DateTime.Parse(messageTimestamp);

                System.DateTime now = System.DateTime.UtcNow;

                //Debug.Log("now:      " + now.ToString());
                //Debug.Log("last msg: " + cohortMessageSentTimestamp.ToString());

                // there are two situations when we want to process this message and do something:
                //   - if it was a recent sound cue, the user might have opened the app from the icon rather than the notification banner, so we should play it
                //   - if it was a text cue with cue number zero, we want to display that text in the app

                System.TimeSpan timeSinceMessageSent = now.Subtract(cohortMessageSentTimestamp.ToUniversalTime());
                Debug.Log("last cohort message: \n\n" + response.DataAsText);
                var message = jsonObj["message"];
                Debug.Log("last cohort message sent " + timeSinceMessageSent.TotalSeconds + " seconds ago");
                Debug.Log("message mediaDomain is " + (int)message["mediaDomain"]);
                Debug.Log("message cueNumber is " + (int)message["cueNumber"]);


                if ((timeSinceMessageSent.TotalSeconds <= 15 && (int)message["mediaDomain"] == 0) /* recent sound cue*/||
                  ((int)message["mediaDomain"] == 2 && (int)message["cueNumber"] == 0) /* text notification */) {
                  // process the remote cohort message as if we'd received it live
                  // SUPER HACKY
                  Debug.Log("...processing the message");
                  Hashtable msgHashtable = new Hashtable();
                  msgHashtable["cueNumber"] = (int)message["cueNumber"];

                  msgHashtable["mediaDomain"] = (int)message["mediaDomain"];

                  msgHashtable["cueAction"] = (int)message["cueAction"];

                  ArrayList tags = new ArrayList();
                  //Debug.Log(message["targetTags"].GetType());
                  //Debug.Log(message["targetTags"]);

                  if (message.Keys.Contains("targetTags")) { 
                    for (int i = 0; i < message["targetTags"].Count; i++) {
                      string tag = message["targetTags"][i].ToString();
                      tags.Add(message["targetTags"][i]);
                    }
                  }

                  msgHashtable["targetTags"] = tags;

                  if (message.Keys.Contains("cueContent")) {
                    msgHashtable["cueContent"] = message["cueContent"];
                  }

                  validateCohortMessageCallback(msgHashtable);
                }
              }
            }
          }
        }
      );

      req.Send();
    }

    public enum Status {
      initialized,
      waitingForDeviceTokenFromAPNS,
      receivedDeviceTokenFromAPNS,
      attemptingToRegisterForNotifications,
      registeredForNotifications,
      receivedErrorOnRequest
    }

    [System.Serializable]
    public class RegisterForRemoteNotificationsRequest {
      public string token;
    }
  }
}
