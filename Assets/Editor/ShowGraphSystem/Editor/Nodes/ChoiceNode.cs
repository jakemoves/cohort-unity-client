using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Collections.Specialized;
using TransitionCueEntry = ShowGraphSystem.Serialization.ChoiceNodeData.TransitionCueEntry;

namespace ShowGraphSystem.Editor
{
    // TODO: Add Questions to a container to display when node is collapsed
    public partial class ChoiceNode : ShowGraphNode
    {
        public static readonly Color DarkBlueGray = new Color(108f / 255f, 105f / 255f, 141f / 255f);

        protected ChoiceElement choiceContainer;
        protected Foldout portDescriptionContainer;
        protected Foldout transitionCuesFoldout;
        protected Dictionary<string, TransitionCueContainer> transitionCueContainers;

        // TODO: Public Properties for the choice data
        public ChoiceElement ChoiceContainer => choiceContainer;

        // TODO: FINISH
        public ChoiceNode() : base()
        {
            base.title = "Choice";
            this.titleContainer.style.backgroundColor = new StyleColor(DarkBlueGray);
        }

        public ChoiceNode(ObservableCollection<string> groups) : base(groups)
        {
            base.title = "Choice";
            this.titleContainer.style.backgroundColor = new StyleColor(DarkBlueGray);
        }

        public static ChoiceNode FromChoiceNodeData(Serialization.ChoiceNodeData choiceNodeData)
        {
            ChoiceNode choiceNode = ShowGraphNode.FromShowNodeData<ChoiceNode>(choiceNodeData);

            // NOTE to self "KeyList" is group tied to an indexer used for the bit shifting.
            foreach (var group in choiceNodeData.KeyList)
            {
                // NOTE: Should we check if the group in the keylist is in the group selection?
                if (!choiceNode.choiceContainer.AddChoice(group, choiceNodeData.GroupChoices[group]))
                    Debug.LogError($"An error occured while loading choices for node {choiceNode.ID}: A choice for the group {group} already exists.");
            }

            foreach (KeyValuePair<string, TransitionCueEntry> kv in choiceNodeData.TransitionCuesByGroups)
            {
                choiceNode.transitionCueContainers[kv.Key].HasTransition = kv.Value.HasTransition;
                choiceNode.transitionCueContainers[kv.Key].CueData = kv.Value.CueReference;
            }

            return choiceNode;
        }

        protected override void InitializeUi()
        {
            // Add Containers
            choiceContainer = new ChoiceElement()
            {
                name = "ChoiceContainer"
            };
            var choiceFoldout = new Foldout() { text = "Choice Questions" };
            choiceFoldout.Add(choiceContainer);
            choiceFoldout.value = false;
            extensionContainer.Add(choiceFoldout);

            // Transition Cues
            transitionCuesFoldout = new Foldout()
            {
                text = "Transition Cues"
            };
            transitionCuesFoldout.style.backgroundColor = new StyleColor(new Color(0, 0.05f, 0.05f));
            extensionContainer.Add(transitionCuesFoldout);

            // Port Descriptions
            portDescriptionContainer = new Foldout
            {
                text = "Port Descriptions",
                value = false
            };
            extensionContainer.Add(portDescriptionContainer);

            /* Event Subs */
            choiceContainer.ChoicesChanged += OnChoicesChanged;

            base.InitializeUi();
        }

        protected override void Groups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.Groups_CollectionChanged(sender, e);

            if (Groups != null)
            {
                transitionCuesFoldout.Clear();

                transitionCueContainers ??= new Dictionary<string, TransitionCueContainer>(Groups.Count);

                foreach (var groupName in Groups)
                {
                    if(!transitionCueContainers.ContainsKey(groupName))
                    {
                        var transitionCueContainer = new TransitionCueContainer(Groups, groupName)
                        {
                            visible = true
                        };
                        transitionCueContainers[groupName] = transitionCueContainer;
                    }

                    transitionCueContainers[groupName].style.borderBottomColor = new StyleColor(Color.white);
                    transitionCueContainers[groupName].style.borderBottomWidth = new StyleFloat(1);
                }
            }
        }

        protected virtual void OnChoicesChanged(object sender, EventArgs e)
        {
            UpdatePortDescriptions();
        }

        protected override void OnGroupSelectionChanged(object sender, GroupSelectionChangedEventArgs e)
        {
            base.OnGroupSelectionChanged(sender, e);

            foreach (var keyValue in e.GroupSelection)
            {
                if (keyValue.Value == true && choiceContainer.AddChoice(keyValue.Key))
                {

                }
                else if (keyValue.Value == false && choiceContainer.RemoveChoice(keyValue.Key))
                {

                }
            }
            InitializePorts();

            foreach (var keyValuePair in e.GroupSelection)
            {
                if (keyValuePair.Value && !transitionCuesFoldout.Contains(transitionCueContainers[keyValuePair.Key]))
                    transitionCuesFoldout.Add(transitionCueContainers[keyValuePair.Key]);
                else if (!keyValuePair.Value && transitionCuesFoldout.Contains(transitionCueContainers[keyValuePair.Key]))
                    transitionCuesFoldout.Remove(transitionCueContainers[keyValuePair.Key]);
            }
        }

        protected void InitializePorts()
        {
            // Clear Ports
            foreach (Port port in outputContainer.Children().Where(ve => ve is Port))
            {
                var edges = port.connections.ToList();

                foreach (Edge edge in edges)
                {
                    edge.output.Disconnect(edge);
                    edge.input.Disconnect(edge);

                    edge.parent.Remove(edge);
                }
            }

            outputContainer.Clear(); // TODO: Test

            // Check if there is no choices
            if (choiceContainer.KeyList.Count == 0) return;

            // Get number of posible combinations of choices
            var combis = 1 << choiceContainer.KeyList.Count;

            for (int i = 0; i < combis; i++)
            {
                var port = this.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object));
                port.portName = i.ToString();
                outputContainer.Add(port);
            }
            RefreshExpandedState();

            UpdatePortDescriptions();
        }

        protected void UpdatePortDescriptions()
        {
            // Clear Container
            portDescriptionContainer.Clear();

            // Generate the descriptions
            foreach (Port port in outputContainer.Children().Where(ve => ve is Port))
            {
                // This assumes that the port names are just numbers
                int i = int.Parse(port.portName);

                var portDescription = "";
                for (int j = 0; j < choiceContainer.KeyList.Count; j++)
                {
                    // Each choice is binary thus we do bitwise logic
                    // if that bit in i at the position is true then its a yes
                    //      bit position: j
                    //      choice truth number: 2^(bit position) OR 1 bit shifted by the bit position

                    string yesNo = (i & (1 << j)) != 0 ? "Yes" : "No";
                    portDescription += $"{choiceContainer.GroupChoices[choiceContainer.KeyList[j]]} {yesNo}\n";
                }
                AddPortDescription($"Port: {port.portName}", portDescription);
            }
        }

        protected void AddPortDescription(string header, string description)
        {
            var foldout = new Foldout() { text = header };
            foldout.contentContainer.Add(new Label() { text = description });
            portDescriptionContainer.Add(foldout);
        }

        public Serialization.ChoiceNodeData ToChoiceNodeData()
        {
            // TODO: Add Transition Cue Data
            Serialization.ChoiceNodeData nodeData = ToShowNodeData<Serialization.ChoiceNodeData>();

            nodeData.KeyList = new List<string>(choiceContainer.KeyList);
            nodeData.GroupChoices = new SerializableDictionary<string, string>(choiceContainer.GroupChoices);
            nodeData.TransitionCuesByGroups = new SerializableDictionary<string, TransitionCueEntry>(
                nodeData
                .GroupSelection
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToDictionary(groupName => groupName,
                groupName => new TransitionCueEntry(transitionCueContainers[groupName].HasTransition, transitionCueContainers[groupName].CueData)));

            return nodeData;
        }

        public static explicit operator Serialization.ChoiceNodeData(ChoiceNode choiceNode) => choiceNode.ToChoiceNodeData();

    }
}
