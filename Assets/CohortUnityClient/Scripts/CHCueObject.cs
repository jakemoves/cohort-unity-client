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
