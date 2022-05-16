using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShowGraphSystem.Serialization
{
    [Serializable]
    public class ShowGraphDataSO : ScriptableObject, IGraphSerializedData
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public List<string> Groups { get; set; }
        [field: SerializeField] public RootNodeData Root { get; set; }
        [field: SerializeField] public List<SceneNodeData> SceneNodes { get; set; }
        [field: SerializeField] public List<ChoiceNodeData> ChoiceNodes { get; set; }
        [field: SerializeField] public List<BasicShowEdgeData> ShowEdges { get; set; }
    }
}
