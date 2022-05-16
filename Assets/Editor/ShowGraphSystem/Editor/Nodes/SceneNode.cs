#define TESTING
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Specialized;

// NOTE: Nested Types are declared in SceneNode.GroupedCueReferencesUI.cs
namespace ShowGraphSystem.Editor
{
    public partial class SceneNode : ShowGraphNode
    {
        private Dictionary<string, ObservableCollection<CueReference>> cueListsByGroupDict = new Dictionary<string, ObservableCollection<CueReference>>();

        protected internal Port outputPort;

        private Dictionary<string, ObservableCollection<CueReference>> CueListsByGroupDictionary
        {
            get => cueListsByGroupDict;
            set
            {
                if (value != cueListsByGroupDict)
                {
                    cueListsByGroupDict = value;

                    if (cueListsByGroupDict != null)
                        CueListsByGroup = new ReadOnlyDictionary<string, ObservableCollection<CueReference>>(cueListsByGroupDict);
                    else
                        CueListsByGroup = null;
                }
            }
        }

        public VisualElement CueListContainer { get; private set; } = new VisualElement()
        {
            name = "cueListContainer"
        };

        public ReadOnlyDictionary<string, ObservableCollection<CueReference>> CueListsByGroup { get; private set; }

        public SceneNode() : base()
        {
            base.title = "Scene Node";
            CueListsByGroup = new ReadOnlyDictionary<string, ObservableCollection<CueReference>>(cueListsByGroupDict);
        }

        public SceneNode(ObservableCollection<string> groups) : base(groups)
        {
            base.title = "Scene Node";
            CueListsByGroup = new ReadOnlyDictionary<string, ObservableCollection<CueReference>>(cueListsByGroupDict);
        }

        public static SceneNode FromSceneNodeData(Serialization.SceneNodeData sceneNodeData)
        {
            SceneNode sceneNode = ShowGraphNode.FromShowNodeData<SceneNode>(sceneNodeData);

            sceneNode.SetCueListsByGroup(new Dictionary<string, List<CueReference>>(sceneNodeData.CueListByGroups));

            return sceneNode;
        }

        protected override void InitializeUi()
        {
            /* TITLE CONTAINER */
            var titleElement = titleContainer[0];
            TextField sceneNameTextField = new TextField()
            {
                value = title
            };

            titleElement.AddManipulator(new Clickable(() => { }));

            sceneNameTextField.RegisterValueChangedCallback(change =>
            {
                title = change.newValue;
            });

            sceneNameTextField.RegisterCallback<FocusOutEvent>(focusEvent =>
            {
                titleContainer.Insert(0, titleElement);
                sceneNameTextField.Blur();
                sceneNameTextField.RemoveFromHierarchy();
            });

            sceneNameTextField.RegisterCallback<KeyDownEvent>(keydownEvent =>
            {
                if (keydownEvent.keyCode == KeyCode.Return)
                {
                    titleContainer.Insert(0, titleElement);
                    sceneNameTextField.Blur();
                    sceneNameTextField.RemoveFromHierarchy();
                }
            });

            titleElement.RegisterCallback<ClickEvent>(clickEvent =>
            {
                if (clickEvent.clickCount == 2)
                {
                    titleElement.RemoveFromHierarchy();
                    titleContainer.Insert(0, sceneNameTextField);
                    sceneNameTextField.Focus();
                    sceneNameTextField.SelectAll();
                }
            });

            /* OUTPUT CONAINER */
            outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object));
            outputPort.portName = "Next Scene(s)";
            outputContainer.Add(outputPort);

            // TODO: Finish CUES
            /* CUES */
            extensionContainer.Add(CueListContainer);
            UpdateCueLists();

            base.InitializeUi();
        }

        public void UpdateCueLists()
        {
            CueListContainer?.Clear();

            if (Groups == null) return;

            Foldout cueListsFoldout = new Foldout()
            {
                text = "Cue Lists",
                value = false
            };
            CueListContainer.Add(cueListsFoldout);

            foreach (var groupName in Groups)
            {
                var groupedCuesUI = new GroupedCueReferencesUI(groupName, Groups);

                cueListsFoldout.Add(groupedCuesUI);

                if (CueListsByGroupDictionary.ContainsKey(groupName))
                    groupedCuesUI.CueReferences = CueListsByGroupDictionary[groupName];
                else
                    CueListsByGroupDictionary[groupName] = groupedCuesUI.CueReferences;
            }

            // NOTE: This method keeps old group cuelists and does not cull them;
        }

        protected override void Groups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCueLists();
            base.Groups_CollectionChanged(sender, e);
        }

        public void SetCueListsByGroup(Dictionary<string, List<CueReference>> newCueListsByGroup)
        {
            if (newCueListsByGroup == null)
            {
                CueListsByGroupDictionary = null;
            }
            else
            {
                CueListsByGroupDictionary = newCueListsByGroup.ToDictionary<KeyValuePair<string, List<CueReference>>, string, ObservableCollection<CueReference>>(
                    kv => kv.Key,
                    kv => new ObservableCollection<CueReference>(kv.Value));
            }

            // Update the Cue Lists
            UpdateCueLists();
        }

        public Serialization.SceneNodeData ToSceneNodeData()
        {
            Serialization.SceneNodeData nodeData = ToShowNodeData<Serialization.SceneNodeData>();
            nodeData.CueListByGroups = new SerializableDictionary<string, List<CueReference>>(
                CueListsByGroup.ToDictionary<KeyValuePair<string, ObservableCollection<CueReference>>, string, List<CueReference>>(
                    kv => kv.Key,
                    kv => kv.Value.ToList()));

            return nodeData;
        }

        public static explicit operator Serialization.SceneNodeData(SceneNode sceneNode) => sceneNode.ToSceneNodeData();
    } 
}
