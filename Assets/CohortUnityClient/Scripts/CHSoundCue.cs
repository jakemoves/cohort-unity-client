using UnityEngine;

namespace Cohort {
  [System.Serializable]
  public class CHSoundCue {
    [SerializeField]
    public AudioClip audioClip;

    [SerializeField]
    public float cueNumber;

    [TextArea (maxLines: 10, minLines: 3)]
    public string accessibleAlternative;

    public CHSoundCue() { }

    public CHSoundCue(AudioClip clip, string a11yAlternative){
      audioClip = clip;
      accessibleAlternative = a11yAlternative;
    }
  }
}
