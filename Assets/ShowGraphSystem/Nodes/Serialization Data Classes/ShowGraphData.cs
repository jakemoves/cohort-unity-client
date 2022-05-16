using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShowGraphSystem.Serialization
{
    public interface IGraphSerializedData
    {
        public string Name { get; set; }
        public List<string> Groups { get; set; }

        public RootNodeData Root { get; set; }

        // Lists For every Node Type - For Reasons
        // Serialization Reasons...
        public List<SceneNodeData> SceneNodes { get; set; }
        public List<ChoiceNodeData> ChoiceNodes { get; set; }

        public List<BasicShowEdgeData> ShowEdges { get; set; }
    }

    [Serializable]
    public class ShowGraphData : IGraphSerializedData
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public List<string> Groups { get; set; }
        [field: SerializeField] public RootNodeData Root { get; set; }
        [field: SerializeField] public List<SceneNodeData> SceneNodes { get; set; }
        [field: SerializeField] public List<ChoiceNodeData> ChoiceNodes { get; set; }
        [field: SerializeField] public List<BasicShowEdgeData> ShowEdges { get; set; }
    }

}