using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;
using UnityEditor;

namespace ShowGraphSystem.Editor
{
    public class RootNode : TokenNode, IShowGraphNode
    {
        public const string RootID = "root";

        public string ID => RootID;
        public Port StartPort => base.output;

        public RootNode() : base(null, Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object)))
        {
            StartPort.portName = "Entry Point";
            icon = EditorGUIUtility.IconContent("d_BlendTree").image;
        }

        public static RootNode FromRootNodeData(Serialization.RootNodeData nodeData)
        {
            if (nodeData == null) return null;

            var root = new RootNode();
            root.SetPosition(new Rect(nodeData.Position, Vector2.zero));

            return root;
        }

        public Serialization.RootNodeData ToRootNodeData() => new Serialization.RootNodeData() { Position = this.GetPosition().position };

        public static explicit operator Serialization.RootNodeData(RootNode v) => v.ToRootNodeData();
    }
}