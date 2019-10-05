# Cohort Unity Client Demo

This Unity project includes the [Cohort Unity Client asset package](https://github.com/jakemoves/cohort-unity-client) and is useful as a template or for testing Cohort.

## Getting started

### Import (paid) dependencies from Unity Asset Store
- [BestHTTP](https://assetstore.unity.com/packages/tools/network/best-http-10872)
- [iOS Native Flashlight](https://assetstore.unity.com/packages/tools/integration/ios-native-flashlight-129556)

### Setting up cues
- look at CohortUnityClient/Scenes/CohortDemoScene for an example
- create a plain GameObject, name it 'CohortManager'
- drag CohortUnityClient/Scripts/CHSession.cs onto the CohortManager GameObject
- for sound cues:
  - create an Audio Source GameObject (name it if you want)
  - select the CohortManager GameObject
    - in the Inspector, under 'CH Session', set the 'Audio Player' field to your Audio Source object
  - import your sound files to /Resources (as Audio Clips)
    - you can set desired transcode / quality settings under your Audio Clips' Import Settings
  - select the CohortManager GameObject
  - under CH Session > Sound Cues, set Size to 1
  - under Element 0:
    - set the Audio Clip field to one of your Audio Clips
    - set Cue Number to 1 (point cue numbers like 10.5 are fine too)
    - if the cue has speech: transcribe it in the 'Accessible Alternative' field
    - if the cue is music or sound effects: provide a description in the 'Accessible Alternative' field

### Testing locally
- start an instance of [Cohort Server](https://github.com/jakemoves/cohort-server) running locally
  - for this test, make sure you have run the default DB seeds (THIS STEP IS INADEQUATELY DOCUMENTED)

### Back in Unity...
- under CH Session, enter the following values:

| | |
|-|-|
| Server URL      | localhost                             |
| Http Port       | 3000                                  |
| Web Socket Path | /sockets                              |
| Event Id        | 4                                     |
| Client Occasion | 1                                     |
| Client Tag      | any                                   |

- enter Play mode
- you should see console messages saying "req 1 complete", "req 1.5 complete", and "req 2 complete", as well as "opened websocket connection"

### Playing sound...
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
    