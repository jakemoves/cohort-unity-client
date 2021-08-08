using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleAudioPlay : MonoBehaviour
{
    // Start is called before the first frame update

    public AudioSource source;
    public bool playing;
    Button button;
    Text buttonText;
    void Start()
    {
        button = this.GetComponent<Button>();
        if (button == null) 
        { 
            this.enabled = false;
            Debug.LogError("Script is not connected to a Button!");
            if (playing) source.Play();
            return;
        }
        buttonText = button.gameObject.GetComponentInChildren<Text>();
        button.onClick.AddListener( delegate { OnClick(); } );
        ChangeState(playing);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClick() 
    {
        playing = !playing;
        ChangeState(playing);
    }

    void ChangeState(bool playState)
    {
        if (playState == source.isPlaying) return;

        if (playing) 
        {
            source.Play();
            buttonText.text = "Pause";
        }
        else 
        {
            source.Pause();
            buttonText.text = "Play";
        }
    }
}
