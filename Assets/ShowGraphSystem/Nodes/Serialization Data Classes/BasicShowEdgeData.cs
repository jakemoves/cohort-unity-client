using System;
using UnityEngine;


namespace ShowGraphSystem.Serialization
{
    [Serializable]
    public class BasicShowEdgeData
    {
        // NOTE: Ensure port names do not repeat in the nodes
        [SerializeField] private string inputNodeID;
        [SerializeField] private string outputNodeID;
        [SerializeField] private string inputPortName;
        [SerializeField] private string outputPortName;

        public string InputNodeID { get => inputNodeID; }
        public string OutputNodeID { get => outputNodeID; }
        public string InputPortName { get => inputPortName; }
        public string OutputPortName { get => outputPortName; }

        public BasicShowEdgeData(string inputNodeID, string outputNodeID, string inputPortName, string outputPortName)
        {
            this.inputNodeID = inputNodeID;
            this.outputNodeID = outputNodeID;
            this.inputPortName = inputPortName;
            this.outputPortName = outputPortName;
        }

        //public static explicit operator BasicShowEdgeData(Edge edge) => new BasicShowEdgeData(edge);
    } 
}