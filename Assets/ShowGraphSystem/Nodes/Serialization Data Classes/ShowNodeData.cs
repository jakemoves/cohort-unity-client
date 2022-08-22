using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace ShowGraphSystem.Serialization
{
    public interface IShowGraphNodeData
    {
        public string ID { get; }
        public Vector2 Position { get; set; }
    }

    [Serializable]
    public abstract class ShowNodeData : IShowGraphNodeData
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string Title { get; set; }
        [field: SerializeField] public SerializableDictionary<string, bool> GroupSelection { get; set; }
        [field: SerializeField] public List<string> Groups { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
    }

    [Serializable]
    public class SceneNodeData : ShowNodeData
    {
        [field: SerializeField] public SerializableDictionary<string, List<CueReference>> CueListByGroups { get; set; }
    }

    [Serializable]
    public class ChoiceNodeData : ShowNodeData
    {
        [field: SerializeField] public SerializableDictionary<string, string> GroupChoices { get; set; }
        [field: SerializeField] public List<string> KeyList { get; set; }
        [field: SerializeField] public SerializableDictionary<string, TransitionCueEntry> TransitionCuesByGroups { get; set; }

        [Serializable]
        public struct TransitionCueEntry
        {
            [field: SerializeField] public CueReference CueReference { get; set; }
            [field: SerializeField] public bool HasTransition { get; set; }

            public TransitionCueEntry(bool hasTransition, CueReference cueReference)
            {
                HasTransition = hasTransition;
                CueReference = cueReference;
            }

            //public static implicit operator TransitionCueEntry((bool hasTransition, CueReference CueReference) a) => new TransitionCueEntry { HasTransition = a.hasTransition, CueReference = a.CueReference };
            //public static implicit operator (bool hasTransition, CueReference CueReference)(TransitionCueEntry entry) => (entry.HasTransition, entry.CueReference);
        }
    }
}