using UnityEngine;
using System;
using System.Collections.Generic;

namespace ShowGraphSystem.Serialization
{
    [Serializable]
    public class RootNodeData : IShowGraphNodeData
    {
        public const string RootID = "root";

        public string ID => RootID;
        [field: SerializeField] public Vector2 Position { get; set; }
    }
}