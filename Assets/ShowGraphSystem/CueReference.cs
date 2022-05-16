using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShowGraphSystem
{
    [Serializable]
    public class CueReference
    {
        [field: SerializeField] public MediaDomain MediaDomain { get; set; }
        [field: SerializeField] public Dictionary<string, bool> GroupSelection { get; set; }
        [field: SerializeField] public int CueID { get; set; }
    }

    public enum MediaDomain
    {
        Sound,
        Video,
        Image,
        Text,
        Light,
        Haptic
    } 
}
