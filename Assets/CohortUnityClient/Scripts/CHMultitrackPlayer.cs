using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;
using UnityEngine.Audio;

public class CHMultitrackPlayer : MonoBehaviour
{
    [SerializableField]
    private AudioMixerGroup mixer;

    private Dictionary<AudioPlayer> players;
    private List<CHSoundCue> soundCues;


    // Start is called before the first frame update
    void Start()
    {
        CHSession cohortSession = GameObject.Find("CohortManager").GetComponent<CHSession>();
        
        soundCues = cohortSession.soundCues;
        foreach(CHSoundCue cue in soundCues){
            AudioPlayer player = new AudioPlayer();
            player.audioClip = cue.audioClip;
            player.output = mixer;
            players.Add(cue.cueNumber.ToString(), player);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
