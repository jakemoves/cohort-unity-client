using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ShowGraphSystem
{
    [Serializable]
    public class CueReference
    {
        [field: SerializeField] public MediaDomain MediaDomain { get; set; }
        [field: SerializeField] public SerializableDictionary<string, bool> GroupSelection { get; set; }
        [field: SerializeField] public int CueID { get; set; }
        [field: SerializeField] public bool VibrateOnCue { get; set; }
        public string[] GetSelectedGroups()
        {
            if (GroupSelection == null)
                return new string[0]; ;

            return (from kv in GroupSelection
                    where kv.Value
                    select kv.Key).ToArray();
        }

        public override string ToString()
            => $"{MediaDomain} Cue #{CueID} to {string.Join(", ", GetSelectedGroups())}";
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
