using UnityEngine;
using UnityEngine.Video;

namespace Cohort {
  [System.Serializable]
  public class CHVideoCue {
    [SerializeField]
    public VideoClip videoClip;

    [SerializeField]
    public float cueNumber;

    // public AudioClip accessibleAlternative;
    // described video, captions...

    [SerializeField]
    public string accessibleAlternative;

    public CHVideoCue() { }

    public CHVideoCue(VideoClip clip) {
      videoClip = clip;
    }
  }
}
