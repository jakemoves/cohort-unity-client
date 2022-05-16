#define TESTING
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace ShowGraphSystem.Editor
{
    public interface IShowGraphNode
    {
        public string ID { get; }
    }

    public abstract class ShowGraphNode : Node, IShowGraphNode
    {
        private GroupSelectionUI groupSelectionUI;
        protected internal Port inputPort;
        private ObservableCollection<string> groups = null;

        public string ID { get; set; } = Guid.NewGuid().ToString();
        public ObservableCollection<string> Groups
        {
            get => groups;
            set
            {
                if (groups != null)
                    groups.CollectionChanged -= Groups_CollectionChanged;

                groups = value;

                if (groups != null)
                    groups.CollectionChanged += Groups_CollectionChanged;

                Groups_CollectionChanged(this,
                    new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
        }

        public VisualElement GroupContainer { get; } = new VisualElement()
        {
            name = "groupContainer",
        };

        public event EventHandler<GroupSelectionChangedEventArgs> GroupSelectionChanged;

        public ShowGraphNode()
        {
            InitializeNode();
            Groups = null;
        }

        public ShowGraphNode(ObservableCollection<string> groups)
        {
            InitializeNode();

            // Instialize Observable Collection for Groups
            Groups = groups;
        }

        protected static TShowGraphNode FromShowNodeData<TShowGraphNode>(Serialization.ShowNodeData showNodeData)
            where TShowGraphNode : Editor.ShowGraphNode, new()
        {
            TShowGraphNode graphNode = new TShowGraphNode()
            {
                ID = showNodeData.ID,
                title = showNodeData.Title,
                Groups = new ObservableCollection<string>(showNodeData.Groups)
            };
            graphNode.SetPosition(new Rect(showNodeData.Position, Vector2.zero));
            // Groups are set in the Show Graph View

            // NOTE: We didn't connect parent nodes - because we will do it top down

            return graphNode;
        }

        protected virtual void InitializeNode()
        {
            title = "Show Node";
            extensionContainer.Insert(0, GroupContainer);

            // Event
            //GroupSelectionChanged += ShowGraphNode_GroupSelectionChanged;

            // Styling
            this.AddToClassList("show-node");
            mainContainer.AddToClassList("main-container");
            extensionContainer.AddToClassList("extension-container");
            titleContainer.AddToClassList("title-container");

            // UI
            InitializeUi();
        }

        protected virtual void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InitGroupSelectionUi();
        }

        private void GroupSelectionUI_GroupSelectionChanged(object sender, GroupSelectionChangedEventArgs e)
        {
            GroupSelectionChanged?.Invoke(this, e);
            OnGroupSelectionChanged(sender, e);
        }

        // NOTE: if there no instructions in the body, make this abstract instead
        protected virtual void OnGroupSelectionChanged(object sender, GroupSelectionChangedEventArgs e)
        {
            // TODO: Update relavent Group stuff
        }

        // TODO: FIND BETTER NAME
        protected virtual void InitializeUi()
        {
            /* INPUT CONTAINER */
            // TODO: Change System.Type param
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
            inputPort.portName = "Previous Scene(s)";
            inputContainer.Add(inputPort);

            /* GROUPS */
            InitGroupSelectionUi();

            /* DEBUG STUFF */
#if TESTING
            // TODO!: REMOVE FOR PRODUCTION
            mainContainer.Insert(1, new Button(() =>
            {
            // Test GetSelectedGroups
            Debug.Log("DISPLAYING GROUP VALUES:");
                var selG = GetGroupSelection();
                foreach (var grp in selG)
                {
                    Debug.Log($"{grp.Key} = {grp.Value}");
                }
            })
            {
                text = "TEST NODE"
            });
#endif

            // TODO:
            //this.contentContainer.Insert(0, new Label("ERROR"));

            // NOTE: Decide where to put refresh epanded state method\
            RefreshExpandedState();
        }

        private void InitGroupSelectionUi()
        {
            GroupContainer.Clear();

            if (Groups != null)
            {
                // Draw Groups
                Foldout foldout = new Foldout()
                {
                    text = "Groups"
                };
                GroupContainer.Add(foldout);

                groupSelectionUI = new GroupSelectionUI()
                {
                    Groups = Groups
                };

                foldout.Add(groupSelectionUI);
                groupSelectionUI.GroupSelectionChanged += GroupSelectionUI_GroupSelectionChanged;
            }
        }

        /// <summary>
        /// Generates an <see cref="ReadOnlyDictionary{string, bool}"/> 
        /// of the enabled status of the groups this node is apart of.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyDictionary{string, bool}"/> of the nodes grouping status, with the group names as the key.</returns>
        public ReadOnlyDictionary<string, bool> GetGroupSelection() => groupSelectionUI?.GetGroupSelection();

        protected TShowNodeData ToShowNodeData<TShowNodeData>()
            where TShowNodeData : Serialization.ShowNodeData, new()
        {
            TShowNodeData nodeData = new TShowNodeData()
            {
                ID = ID,
                Title = title,
                Groups = Groups.ToList(),
                GroupSelection = new SerializableDictionary<string, bool>(GetGroupSelection()),
                Position = GetPosition().position
            };

            return nodeData;
        }

        public static Editor.ShowGraphNode FromShowNodeData<TShowNodeData>(TShowNodeData showNodeData)
            where TShowNodeData : Serialization.ShowNodeData
        {
            if (showNodeData is Serialization.SceneNodeData sceneNodeData)
                return SceneNode.FromSceneNodeData(sceneNodeData);
            else if (showNodeData is Serialization.ChoiceNodeData choiceNodeData)
                return ChoiceNode.FromChoiceNodeData(choiceNodeData);
            else
                throw new NotImplementedException($"The TShowNodeData type {typeof(TShowNodeData)} has not been implemented.");
        }
    
        public void SetGroupSelection(Dictionary<string, bool> groupSelection)
            => groupSelectionUI.SetGroupSelection(groupSelection);
    } 
}