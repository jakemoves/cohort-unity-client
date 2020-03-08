# Cohort Unity Client

This asset package allows a Cohort server to trigger playback of sound, video, and AR cues within a Unity app (as well as turn the device flashlight on and off, and trigger vibration).

An example project for this Unity asset package is available [here.](https://github.com/jakemoves/cohort-unity-client-demo)

## Getting started
- download the [Cohort Unity Client asset package]() and import it into your Unity project
- this client works with a server. You can [use ours](https://new.cohort.rocks/admin) or [run your own](https://github.com/jakemoves/cohort-server):
  - create an account
  - create an event
  - create an occasion
  - note down the event id and occasion id

### Import (paid) dependencies from Unity Asset Store
- [BestHTTP](https://assetstore.unity.com/packages/tools/network/best-http-10872)

### Creating cues
- create a plain GameObject and name it 'CohortManager'
- drag CohortUnityClient/Scripts/CHSession.cs onto the CohortManager GameObject
- select the CohortManager GameObject, and in the Inspector:
  - set the Server URL to `https://new.cohort.rocks` (or, if you're running your own server, enter the URL; if you're running your server locally, also set the Http Port)
  - set your username and password
  - when you finish entering your username and password, the Console should display "Login successful"
  - now any changes you make to your cues in Unity will be updated on the [admin site](https://new.cohort.rocks/admin) (or `[your URL]/admin`)

#### Sound cues
- create an Audio Source GameObject (name it if you want)
- select the CohortManager GameObject, and in the Inspector: 
  - under 'Settings': set the 'Audio Player' field to your Audio Source object
- import your sound files to /Resources (as Audio Clips)
  - you can set desired transcode / quality settings under your Audio Clips' Import Settings
- select the CohortManager GameObject
- under CH Session > Sound Cues, set Size to 1
- under Element 0:
  - set the Audio Clip field to one of your Audio Clips
  - set Cue Number to 1 (point cue numbers like 10.5 are fine too)
  - if the cue has speech: transcribe it in the 'Accessible Alternative' field
  - if the cue is music or sound effects: provide a description in the 'Accessible Alternative' field

#### Video cues
- content to come

#### Image cues
- content to come

### Testing locally
- content to come