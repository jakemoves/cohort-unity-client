using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSelection : MonoBehaviour
{
    [SerializeField]
    public List<AudioClip> selectionClips = new List<AudioClip>();
    public List<string> selectionNames = new List<string>();
    public Dropdown selector;
    public AudioSource source;
    int count;
    // Start is called before the first frame update
    void Start()
    {
        if (selector == null || source == null) {
            Debug.LogError("There is no Dropdowm and/or AudioSource connected");
            this.enabled = false;
            return;
        }
        if (selectionClips.Count != selectionNames.Count)
        {
            Debug.LogError("There Must be as many audio clips as there are names");
            count = Mathf.Min(selectionClips.Count, selectionNames.Count);
        }
        else count = selectionClips.Count;
        
        selector.ClearOptions();
        selector.AddOptions(selectionNames.GetRange(0, count));

        selector.onValueChanged.AddListener(delegate { OnSelectionChanged(selector); });

        source.clip = selectionClips[selector.value];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnSelectionChanged(Dropdown change)
    {
        var isPlaying = source.isPlaying;
        source.Stop();
        source.clip = selectionClips[selector.value];

        if (isPlaying) source.Play();
    }
}
