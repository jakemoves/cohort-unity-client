// Copyright Jacob Niedzwiecki, 2019
// Released under the MIT License (see /LICENSE)

using System;
using System.Collections.Generic;


namespace Cohort {
    [Serializable]
    public class CHMessage {

        public List<string> targetTags;
        public MediaDomain mediaDomain;
        public double cueNumber;
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

        public CHMessage FromSoundCue(CHSoundCue cue) {
          Cue parsedCue;
          parsedCue.mediaDomain = MediaDomain.sound;
          parsedCue.cueNumber = cue.cueNumber;
          parsedCue.cueAction = CueAction.play;
          parsedCue.targetTags =  new List<string>();
          parsedCue.targetTags.Add("all");
          
          return cueToCHMessage(parsedCue);
        }

        public CHMessage FromVideoCue(CHVideoCue cue)
        {
          Cue parsedCue;
          parsedCue.mediaDomain = MediaDomain.video;
          parsedCue.cueNumber = cue.cueNumber;
          parsedCue.cueAction = CueAction.play;
          parsedCue.targetTags = new List<string>();
          parsedCue.targetTags.Add("all");

          return cueToCHMessage(parsedCue);
        }

        public CHMessage FromImageCue(CHImageCue cue)
        {
          Cue parsedCue;
          parsedCue.mediaDomain = MediaDomain.image;
          parsedCue.cueNumber = cue.cueNumber;
          parsedCue.cueAction = CueAction.play;
          parsedCue.targetTags = new List<string>();
          parsedCue.targetTags.Add("all");

          return cueToCHMessage(parsedCue);
        }



    CHMessage cueToCHMessage(Cue cue)
        {
          CHMessage msg = new CHMessage();
          msg.mediaDomain = cue.mediaDomain;
          msg.cueNumber = cue.cueNumber;
          msg.cueAction = cue.cueAction;
          msg.targetTags = cue.targetTags;
        
          return msg;
        }

        struct Cue
        {
      // float is throwing a Json Mapper max allowed depth error
          public MediaDomain mediaDomain;
          public double cueNumber;
          public CueAction cueAction;
          public List<string> targetTags;

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