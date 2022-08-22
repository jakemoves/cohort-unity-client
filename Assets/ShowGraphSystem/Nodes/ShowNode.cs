using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShowGraphSystem;
using ShowGraphSystem.Serialization;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace ShowGraphSystem.Runtime
{
    [Serializable]
    public abstract class ShowNode
    {
        public string ID { get; protected set; }
        public string Title { get; protected set; }
        public string[] SelectedGroups { get; protected set; }
        public string[] Groups { get; protected set; }

        public List<ShowNode> PreviousShowNodes { get; } = new List<ShowNode>();

        protected static TShowNode FromShowNodeData<TShowNode>(ShowNodeData data, string[] masterGroupList)
            where TShowNode : ShowNode, new()
            //where TShowData : ShowNodeData
        {
            return new TShowNode()
            {
                Groups = masterGroupList,
                SelectedGroups = (from g in masterGroupList
                                 where data.GroupSelection.ContainsKey(g) && data.GroupSelection[g]
                                 select g).ToArray(),
                Title = data.Title,
                ID = data.ID
            };
        }

        //protected static TShowNode FromShowNodeData<TShowNode, TShowData>(TShowData data)
        //    where TShowNode : ShowNode, new()
        //    where TShowData : ShowNodeData
        //    => FromShowNodeData<TShowNode, TShowData>(data, data.Groups.ToArray());

        public override string ToString() => $"{this.GetType().Name} => {Title}";
    }

    [Serializable]
    public class SceneNode : ShowNode
    {
        public ReadOnlyDictionary<string, CueReference[]> CueListByGroups { get; protected set; }

        public List<ShowNode> NextShowNodes { get; } = new List<ShowNode>();

        public static SceneNode FromSceneNodeData(SceneNodeData data, string[] masterGroupList )
        {
            var node = ShowNode.FromShowNodeData<SceneNode>(data, masterGroupList);

            // We filter out any unessesary data that we kept when saving
            node.CueListByGroups = new ReadOnlyDictionary<string, CueReference[]>(
                (from g in node.SelectedGroups
                 where data.CueListByGroups.ContainsKey(g)
                 select (g, data.CueListByGroups[g].ToArray()))
                 .ToDictionary<(string g, CueReference[] cueReferences), string, CueReference[]>(kv => kv.g, kv => kv.cueReferences)
            );

            return node;
        }
    }

    [Serializable]
    public class ChoiceNode : ShowNode
    {
        public string[] GroupKeyArray { get; protected set; }

        public ReadOnlyDictionary<string, string> GroupChoices { get; protected set; }
        public ReadOnlyDictionary<string, CueReference> GroupTransitionCues { get; protected set; }


        public List<ShowNode>[] NextShowNodes { get; protected set; }

        public static ChoiceNode FromChoiceNodeData(ChoiceNodeData data, string[] masterGroupList)
        {
            var node = ShowNode.FromShowNodeData<ChoiceNode>(data, masterGroupList);

            node.GroupKeyArray = data.KeyList.ToArray();
            
            // NOTE: Passing a dictionary to the constructor means that if the underlying dicionary chages this changes too.
            // However we assume that won't happen as we expect the loaded data not to change in it's lifecycle in this context.
            node.GroupChoices = new ReadOnlyDictionary<string, string>(data.GroupChoices);
            node.GroupTransitionCues = new ReadOnlyDictionary<string, CueReference>(
                data.TransitionCuesByGroups.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.HasTransition ? kv.Value.CueReference : null));

            node.NextShowNodes = new List<ShowNode>[1 << masterGroupList.Length].Select(item => new List<ShowNode>(4)).ToArray();

            return node;
        }
    }
}