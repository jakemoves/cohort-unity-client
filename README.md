# Cohort Unity Client

This Unity project includes the [Cohort Unity Client asset package](https://cohort.rocks/cohort-unity-client.unitypackage) and is useful as a template or for testing Cohort. If you have an existing Unity project, you can download and install the asset package linked above, and import it into your project.

## Using the example Unity project
- clone or download this repo and open it in Unity
- open up CohortUnityClient/Scenes/CohortDemoScene and have a look at the CohortManager GameObject
- it connects to a Cohort server (online or local), keeps track of sound and video cues, and triggers them in response to Cohort messages received over the network

## Getting started with the Cohort Unity Client
- download the Cohort Unity Client asset package and import it into your Unity project

### Import (paid) dependencies from Unity Asset Store
- [BestHTTP](https://assetstore.unity.com/packages/tools/network/best-http-10872)
- [iOS Native Flashlight](https://assetstore.unity.com/packages/tools/integration/ios-native-flashlight-129556)

### Setting up cues
- create a plain GameObject and name it 'CohortManager'
- drag CohortUnityClient/Scripts/CHSession.cs onto the CohortManager GameObject
- for sound cues:
  - create an Audio Source GameObject
  - select the CohortManager GameObject
    - in the Inspector, under 'CH Session', set the 'Audio Player' field to your Audio Source
  - import your sound files to /Resources (as Audio Clips)
    - you can set desired transcode / quality settings under your Audio Clips' Import Settings
  - select the CohortManager GameObject
  - under CH Session > Sound Cues, set Size to 1
  - under Element 0:
    - set the Audio Clip field to one of your Audio Clips
    - set Cue Number to 1 (point cue numbers like 10.5 are fine too)
    - if the cue has speech: transcribe it in the 'Accessible Alternative' field
    - if the cue is music or sound effects: provide a description in the 'Accessible Alternative' field
  - for additional cues, increment the Sound Cues > Size field and set the details (Audio Clip, Cue Number, Accessible Alternative) for each new cue
- video cues follow a similar flow:
  - create a Video Player GameObject 
  - link it to the CH Session using the Video Player field
  - import your video clips 
  - add a Video Cue for each clip

### Testing locally
- start an instance of [Cohort Server](https://github.com/jakemoves/cohort-server) running locally
  - for this test, make sure you have run the default DB seeds (THIS STEP IS INADEQUATELY DOCUMENTED)

### Back in Unity...
- under CH Session, enter the following values:

| Field           | Value                                 |
| --------------- | ------------------------------------- |
| Server URL      | localhost                             |
| Http Port       | 3000                                  |
| Web Socket Path | /sockets                              |
| Event Id        | [your event ID, or 4 for testing]     |
| Client Occasion | [your occasion ID, or 1 for testing]  |
| Client Tag      | any                                   |

- enter Play mode
- you should see console messages saying "req 1 complete", "req 1.5 complete", and "req 2 complete", as well as "opened websocket connection"

### Triggering cue playback
- use a tool like [Postman](https://www.getpostman.com) to send the following JSON as a POST request to 'http://localhost:3000/api/v1/events/4/broadcast' (set the `Content-Type` header to `application/json`): 
  ```
  {
    "mediaDomain": 0,
    "cueNumber": 1,
    "cueAction": 0,
    "targetTags": ["all"]
  }
  ```
- a 'meow' sound should play
- for video cues, set 'mediaDomain' to 1
- check the [Cohort Spec](https://github.com/jakemoves/cohort-spec) for detailed info on these messages
    
### Testing on a device
- select the CohortManager GameObject
  - in the Inspector, under CH Session > Server URL, replace 'localhost' with your IP address or computer name (i.e. 'jakemoves.local')
- build your Unity project and run it on a device from Xcode