#define SHOW_GRAPH
// Copyright Jacob Niedzwiecki, Nicole Goertzen 2020
// Released under the MIT License (see /LICENSE)

using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
// NOTE: Best HTTP is an old dependancy
using BestHTTP;
using BestHTTP.WebSocket;
// NOTE: This could be replaced if necessary
using LitJson;
using UnityEditor;
using UnityEngine.Networking;

namespace Cohort
{
    public struct Cue
    {
        // float is throwing a Json Mapper max allowed depth error
        public MediaDomain mediaDomain;
        public double cueNumber;
        public CueAction cueAction;
        public List<string> targetTags;
    }

}

#if SHOW_GRAPH
namespace ShowGraphSystem
{
    public static class ShowGraphExtentions
    {
        public static Cohort.Cue ToCohortCue(this CueReference cueReference, Cohort.CueAction cueAction)
        {
            return new Cohort.Cue
            {
                cueAction = cueAction,
                cueNumber = cueReference.CueID,
                mediaDomain = cueReference.MediaDomain.GetCorhortMediaDomain(),
                targetTags = new List<string>(cueReference.GetSelectedGroups())
            };
        }

        public static Cohort.Cue GetVibrationCue(this CueReference cueReference, Cohort.CueAction cueAction = Cohort.CueAction.play)
        {
            return new Cohort.Cue
            {
                cueAction = cueAction,
                cueNumber = -1,
                mediaDomain = Cohort.MediaDomain.haptic,
                targetTags = new List<string>(cueReference.GetSelectedGroups())
            };
        }

        public static Cohort.MediaDomain GetCorhortMediaDomain (this MediaDomain mediaDomain)
        {
            return mediaDomain switch
            {
                MediaDomain.Sound => Cohort.MediaDomain.sound,
                MediaDomain.Video => Cohort.MediaDomain.video,
                MediaDomain.Image => Cohort.MediaDomain.image,
                MediaDomain.Text => Cohort.MediaDomain.text,
                MediaDomain.Light => Cohort.MediaDomain.light,
                MediaDomain.Haptic => Cohort.MediaDomain.haptic,
                _ => throw new NotImplementedException()
            };
        }
    }
}
#endif