using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;
using UnityEngine.Audio;

public class CHMultitrackPlayer : MonoBehaviour
{
    private AudioMixerGroup mixer;

    private Dictionary<string, AudioSource> players;
    private List<CHSoundCue> soundCues;


    // Start is called before the first frame update
    void Start()
    {
        mixer = Resources.Load("Master") as AudioMixerGroup;
        CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
        
        players = new Dictionary<string, AudioSource>();
        soundCues = cohortSession.soundCues;
        Debug.Log(soundCues.Count);
        foreach(CHSoundCue cue in soundCues){
            Debug.Log(cue.cueNumber);
            Debug.Log(cue.accessibleAlternative);
            // AudioSource player = new AudioSource();
            // player.outputAudioMixerGroup = mixer;
            // players.Add(cue.cueNumber.ToString(), player);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onPlayBtn(){
        Debug.Log(players["2"]);
        players["2"].Play();
    }
    // test mixer and playback
    // branch from oncohortmessagereceived

}
