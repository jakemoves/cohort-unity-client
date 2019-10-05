using UnityEngine;

namespace Cohort {
  [System.Serializable]
  public class CHTextCue {
    [TextArea(maxLines: 10, minLines: 3)]
    public string text;

    [SerializeField]
    public float cueNumber;

    public AudioClip accessibleAlternative;

    public CHTextCue() { }

    public CHTextCue(string textForCue){
      text = textForCue;
    }

    public CHTextCue(string textForCue, AudioClip a11yAlternative) {
      text = textForCue;
      accessibleAlternative = a11yAlternative;
    }
  }
}
