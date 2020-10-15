# Cohort Unity Client
Cohort's Unity Client allows you to easily create Android and iOS apps that can respond to Cohort cues to:
- play audio
- play video
- display images
- display text

If you are starting from scratch, skip ahead to the section "Using the example Unity project".

If you already have a Unity app, and you want to integrate Cohort functionality, download the Cohort Unity Client asset package and import it into your Unity project.

## Using the example Unity project
- clone or download this repo and open it in Unity
- open up CohortUnityClient/Scenes/CohortDemoScene and have a look at the CohortManager GameObject
- it connects to a Cohort server (online or local), keeps track of sound and video cues, and triggers them in response to Cohort messages received over the network
- in the Unity console, you should see "Logging into Cohort..." followed by "Login successful"
- switch to the Game tab to preview Cohort functionality in the Unity Editor
- the example project is set up with a shared 'demo' account on the Cohort Admin website 
- in a web browser, go to https://cohort.rocks/admin and sign in with username 'demouser' and password 'demodemo'
- click 'Details' next to Demo Event
- click 'Demo Occasion'
- if you see an 'Open Occasion' button, click it
- back in Unity, click the Play button to run your app in the Editor
- you should see a message in the preview window saying "Connected to Cohort (occasion id:9, grouping: all)"
- on the Cohort Admin website, drag the slider to the right to trigger sound cue 1. You should hear this cue (a cat meowing) play from Unity.
- click the 'next' button to see the other demo cues, and drag the slider to test them out

### Testing the example app on a device
- Android devices are easy to test with, iOS devices take a little more work. We'll start with Android.
- In Unity, go to File > Build Settings
- Make sure 'Android' is selected from the list of platforms on the left
- Plug in an Android device to your computer. (We've tested as far back as Android 9 'Pie')
- Select this device from the 'Run Device' dropdown menu (if it's not showing up, try hitting the Refresh button next to the dropdown)
- Click Build and Run. The app should install to your Android device and launch.
- Try sending some cues from the Cohort Admin site — you should see the results on your device.

---

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