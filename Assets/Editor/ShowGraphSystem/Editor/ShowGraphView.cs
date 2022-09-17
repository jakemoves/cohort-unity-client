using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using ShowGraphSystem.Serialization;

namespace ShowGraphSystem.Editor
{
    public class ShowGraphView : GraphView
    {
        public RootNode Root { get; private set; } = null;

        public bool HasRootNode => (from n in nodes.ToList() where n is RootNode select n).Any();

        public ObservableCollection<string> Groups { get; set; } = new ObservableCollection<string>(new List<string> { "Qas", "Gemma", "Roslyn" });

        // TODO: ADD Drag and drop
        public ShowGraphView() : base()
        {
            this.name = "Show Graph";

            AddGridBackground();

            // Styles
            styleSheets.Add((StyleSheet)EditorGUIUtility.Load("Assets/Editor/ShowGraphSystem/Editor/Styles/ShowGraphView.uss"));
            styleSheets.Add((StyleSheet)EditorGUIUtility.Load("Assets/Editor/ShowGraphSystem/Editor/Styles/ShowGraphNode.uss"));

            // Manipulators
            AddManipulators();
            AddMiniMap();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
            // TODO: Add more logic to prevent ambiguous paths
            if (startPort.direction == port.direction ||
                    startPort.node == port.node || startPort == port)
                    return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public Vector2 GetLocalMousePosition(Vector2 mousePosition)
        {
            // Code copied from: https://github.com/Wafflus/unity-dialogue-system/blob/e860cbb77f668ea3b7bd12a5e3c8ca9bbdc799c6/Assets/Editor/DialogueSystem/Windows/DSGraphView.cs#:~:text=%7D-,public%20Vector2%20GetLocalMousePosition(Vector2%20mousePosition%2C%20bool%20isSearchWindow%20%3D%20false),%7D,-public%20void%20ClearGraph
            Vector2 worldMousePosition = mousePosition;

            //if (isSearchWindow)
            //{
            //    worldMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(editorWindow.rootVisualElement.parent, mousePosition - editorWindow.position.position);
            //}

            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);
            return localMousePosition;
        }

        private void AddMiniMap()
        {
            MiniMap miniMap = new MiniMap()
            {
                anchored = true
            };

            miniMap.SetPosition(new Rect(5, 25, 200, 200));

            Add(miniMap);
        }

        private void AddManipulators()
        {
            // Content Dragging And Zoom
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Contextual Menu
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                populateEvent =>
                {
                    populateEvent.menu.AppendAction("Add Scene Node", menuAction =>
                        AddElement(CreateShowNode<SceneNode>(GetLocalMousePosition(menuAction.eventInfo.mousePosition))));
                    populateEvent.menu.AppendAction("Add Choice Node", menuAction =>
                        AddElement(CreateShowNode<ChoiceNode>(GetLocalMousePosition(menuAction.eventInfo.mousePosition))));
                    populateEvent.menu.AppendSeparator();
                    //populateEvent.menu.AppendAction("Save", menuAction => SaveGraph("GraphTestSave"));
                    //populateEvent.menu.AppendAction("Load", menuAction => GenerateGraphFromData(ShowGraphSystemIO.LoadGraphDataFromSO(assetName: name)));
                    populateEvent.menu.AppendAction("Add Root Node", menuAction =>
                        {
                            if (!HasRootNode)
                                AddElement(CreateRootNode(GetLocalMousePosition(menuAction.eventInfo.mousePosition)));
                            else
                                EditorUtility.DisplayDialog("Can not create Root", "There is a root node already in the graph.", "Ok");
                        });
                }
            );
            this.AddManipulator(contextualMenuManipulator);
        }

        [Obsolete]
        private void SaveGraph(string fileName)
        {
            ShowGraphSystemIO.SaveGraphToSO(this, name);
        }

        private ShowNodeType CreateShowNode<ShowNodeType>(Vector2 position) where ShowNodeType : ShowGraphNode, new()
        {
            // Im so proud of this method //

            // TODO: UPDATE POSITIONING & PARAMS
            ShowNodeType node = new ShowNodeType();

            if (Groups != null)
                node.Groups = Groups;

            node.SetPosition(new Rect(position, Vector2.zero));

            // TODO: Remove this
            //node.RefreshExpandedState();
            return node;
        }

        private RootNode CreateRootNode(Vector2 position)
        {
            RootNode rootNode;
            if (!HasRootNode)
            {
                rootNode = new RootNode();
                Root = rootNode;
            }
            else
                rootNode = Root;

            rootNode.SetPosition(new Rect(position, Vector2.zero));
            return rootNode;
        }

        private void AddGridBackground()
        {
            var gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();

            // TODO: Style Background

            Insert(0, gridBackground);
        }

        private void ResetGraph()
        {
            // I decided to go with the precision approach
            // Instead of a full reset
            nodes.ForEach(node => RemoveElement(node));
            edges.ForEach(edge => RemoveElement(edge));

            Root = null;

            // Scale of one seems like the best choice
            UpdateViewTransform(Vector3.zero, Vector3.one);
        }

        public void GenerateGraphFromData(IGraphSerializedData data)
        {
            // Reset Graph
            ResetGraph();

            // Generate Nodes
            if (data.Root != null)
            {
                Root = RootNode.FromRootNodeData(data.Root);
                AddElement(Root);
            }

            foreach (var sceneNodeData in data.SceneNodes)
                AddElement(SceneNode.FromShowNodeData(sceneNodeData));

            foreach (var choiceNodeData in data.ChoiceNodes)
                AddElement(ChoiceNode.FromShowNodeData(choiceNodeData));

            var showNodeDictionary = (from node in nodes.ToList()
                                      where node is ShowGraphNode
                                      select node as ShowGraphNode).ToDictionary(n => n.ID);

            foreach (var showNodeKV in showNodeDictionary)
            {
                // This could be improved somewhat
                var node = showNodeKV.Value;

                node.Groups = Groups;
            }

            // Set the Groups here - why? trust me i have tried...
            // I think when it adds the Toggles to the visual hierarchy it resets their value
            // Those values are the values of the groupselection - so we lose data...
            foreach (var sceneNodeData in data.SceneNodes)
                showNodeDictionary[sceneNodeData.ID].SetGroupSelection(new Dictionary<string, bool>(sceneNodeData.GroupSelection));

            foreach (var choiceNodeData in data.ChoiceNodes)
                showNodeDictionary[choiceNodeData.ID].SetGroupSelection(new Dictionary<string, bool>(choiceNodeData.GroupSelection));

            // Generate Edges
            // NOTE: This assumes that there is only going to be 1 input port for each ShowNode
            foreach (var edgeData in data.ShowEdges)
            {
                Port inputPort = showNodeDictionary[edgeData.InputNodeID].inputPort;
                Port outputPort;

                if (edgeData.OutputNodeID == RootNode.RootID)
                    outputPort = Root?.StartPort ?? throw new InvalidOperationException("There are edges that connect to the Root but the root is null");
                else
                {
                    try
                    {
                        var outputPortQuery = from ve in showNodeDictionary[edgeData.OutputNodeID].outputContainer.Children()
                                              where ve is Port && (ve as Port).portName == edgeData.OutputPortName
                                              select ve as Port;

                        if (outputPortQuery.Any())
                            outputPort = outputPortQuery.Single();
                        else
                        {
                            Debug.LogError(
                                $"Could not connect edge: node {showNodeDictionary[edgeData.OutputNodeID].title}[ID:{showNodeDictionary[edgeData.OutputNodeID].ID}] " +
                                $"does not have a port called {edgeData.OutputPortName}");
                            continue;
                        }

                    }
                    catch (InvalidOperationException opEx)
                    {
                        Debug.LogException(opEx);
                        continue;
                    }
                }

                if (inputPort == null)
                {
                    Debug.LogError($"The input port in the {nameof(ShowGraphNode)} {(inputPort.node as ShowGraphNode).title}[ID: {(inputPort.node as ShowGraphNode).ID}] is NULL");
                    continue;
                }

                AddElement(outputPort.ConnectTo(inputPort));
            }
        }

        private static void SetShowGraphDataProperties(IGraphSerializedData showGraphData, Editor.ShowGraphView showGraphView)
        {
            showGraphData.Name = showGraphView.name;
            showGraphData.Groups = showGraphView.Groups.ToList();

            // Node Data Lists
            showGraphData.Root = (RootNodeData)showGraphView.Root;

            showGraphData.SceneNodes = (from node in showGraphView.nodes.ToList()
                                        where node is Editor.SceneNode
                                        select (SceneNodeData)(node as Editor.SceneNode)).ToList();
            showGraphData.ChoiceNodes = (from node in showGraphView.nodes.ToList()
                                        where node is Editor.ChoiceNode
                                        select (ChoiceNodeData)(node as Editor.ChoiceNode)).ToList();

            // Edges
            showGraphData.ShowEdges = (from edge in showGraphView.edges.ToList()
                                       where edge.CanGetShowEdgeData()
                                       select edge.ToBasicShowEdgeData()).ToList();
        }

        public static explicit operator ShowGraphDataSO(Editor.ShowGraphView showGraphView)
        {
            var showGraphData = (ShowGraphDataSO)ScriptableObject.CreateInstance(typeof(ShowGraphDataSO));
            SetShowGraphDataProperties(showGraphData, showGraphView);

            return showGraphData;
        }

        public static explicit operator ShowGraphData(Editor.ShowGraphView showGraphView)
        {
            ShowGraphData showGraphData = new ShowGraphData();
            //{
            //    Name = showGraphView.name,
            //    Groups = showGraphView.Groups.ToList(),
            //    SceneNodes = (from node in showGraphView.nodes.ToList()
            //                  where node is Editor.SceneNode
            //                  select (SceneNodeData)(node as Editor.SceneNode)).ToList(),
            //    ShowEdges = (from edge in showGraphView.edges.ToList()
            //                 where edge.CanGetShowEdgeData()
            //                 select edge.ToBasicShowEdgeData()).ToList()
            //};
            SetShowGraphDataProperties(showGraphData, showGraphView);

            return showGraphData;
        }
    } 
}
