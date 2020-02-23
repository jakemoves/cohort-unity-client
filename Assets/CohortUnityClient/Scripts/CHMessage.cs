// Copyright Jacob Niedzwiecki, 2019
// Released under the MIT License (see /LICENSE)

using System.Collections.Generic;

namespace Cohort {
  public class CHMessage {
    public List<string> targetTags;
    public MediaDomain mediaDomain;
    public float cueNumber;
    public CueAction cueAction;
    public int id;
    public string cueContent;

    public static string FormattedMessage(CHMessage msg) {
      string tagsForPrint = "";
      msg.targetTags.ForEach(tag => tagsForPrint = tagsForPrint + tag + ", ");

      return "    " + "cohort message: "
        + "\n      tags:         " + tagsForPrint
        + "\n      media domain: " + msg.mediaDomain
        + "\n      cue #:        " + msg.cueNumber
        + "\n      action:       " + msg.cueAction;
    }
  }

  public enum MediaDomain {
    sound,
    video,
    image,
    text,
    light,
    haptic
  }

  public enum CueAction {
    play, // or "on"
    pause,
    restart,
    stop // or "off"
  }
}