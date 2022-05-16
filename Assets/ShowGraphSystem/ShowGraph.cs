using System;
using System.Collections.Generic;
using UnityEngine;
using ShowGraphSystem;
using ShowGraphSystem.Serialization;
using System.Collections.ObjectModel;

namespace ShowGraphSystem.Runtime
{
    [Serializable]
    public sealed class ShowGraph
    {
        public string Name { get; private set; }
        public string[] MasterGroupArray { get; private set; }
        public List<ShowNode> EntryPoint { get; } = new List<ShowNode>();

        public ReadOnlyCollection<BasicShowEdgeData> ShowEdges { get; private set; }

        //public ReadOnlyDictionary<string, ShowNode>.ValueCollection Nodes => NodeDictionary.Values;
        public ReadOnlyDictionary<string, ShowNode> NodeDictionary { get; private set; }

        public static ShowGraph GenerateGraphFromData(IGraphSerializedData data)
        {
            // NOTE: We can - if needed - recover the master group array
            //       by transversing the tree and getting all the group lists from the nodes

            // Init 
            var graph = new ShowGraph()
            {
                Name = data.Name,
                MasterGroupArray = data.Groups.ToArray(),
                ShowEdges = new ReadOnlyCollection<BasicShowEdgeData>(data.ShowEdges)
            };

            // Recover Nodes
            var nodeDict = new Dictionary<string, ShowNode>(data.SceneNodes.Count);

            foreach (var sceneNodeData in data.SceneNodes)
                nodeDict[sceneNodeData.ID] = SceneNode.FromSceneNodeData(sceneNodeData, graph.MasterGroupArray);

            foreach (var choiceNodeData in data.ChoiceNodes)
                nodeDict[choiceNodeData.ID] = ChoiceNode.FromChoiceNodeData(choiceNodeData, graph.MasterGroupArray);

            graph.NodeDictionary = new ReadOnlyDictionary<string, ShowNode>(nodeDict);

            // Recover Edges
            // or rather Connections
            foreach (var edge in graph.ShowEdges)
            {
                if ((edge.OutputNodeID != RootNodeData.RootID) && !(nodeDict.ContainsKey(edge.OutputNodeID) && nodeDict.ContainsKey(edge.InputNodeID)))
                {
                    Debug.LogError("Edge data contained node IDs that were missing");
                    continue;
                }    

                // We assume that there is only one input port
                var inputtingNode = nodeDict[edge.InputNodeID];

                if (edge.OutputNodeID == RootNodeData.RootID)
                {
                    graph.EntryPoint.Add(inputtingNode);
                    continue;
                }

                ShowNode outputtingNode = nodeDict[edge.OutputNodeID];
                if (nodeDict[edge.OutputNodeID] is SceneNode sceneNode)
                {
                    // TODO: It would be nice to double check the port name to avoid potential errors
                    sceneNode.NextShowNodes.Add(inputtingNode);
                    //outputtingNode = sceneNode;
                }
                else if (nodeDict[edge.OutputNodeID] is ChoiceNode choiceNode)
                {
                    int portNumber;
                    if(!int.TryParse(edge.OutputPortName, out portNumber))
                    {
                        throw new System.IO.InvalidDataException(
                            $"The nameing format for port {edge.OutputPortName} in node {edge.OutputNodeID} a ChoiceNode was invalid.");
                    }

                    if (portNumber >= choiceNode.NextShowNodes.Length)
                    {
                        throw new System.IO.InvalidDataException(
                            $"The port number, {portNumber}, exceded the all the possible combinations for the ChoiceNode {choiceNode.ID}");
                    }

                    if (choiceNode.NextShowNodes[portNumber] == null)
                        choiceNode.NextShowNodes[portNumber] = new List<ShowNode>(graph.MasterGroupArray.Length);

                    choiceNode.NextShowNodes[portNumber].Add(inputtingNode);
                }
                else throw new NotImplementedException($"You forgot to implement {nodeDict[edge.OutputNodeID].GetType()}");
                
                inputtingNode.PreviousShowNodes.Add(outputtingNode);
            }
            return graph;
        }
    } 
}
