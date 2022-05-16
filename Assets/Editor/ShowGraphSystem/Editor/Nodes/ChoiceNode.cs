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
    // TODO: Add Questions to a container to display when node is collapsed
    public class ChoiceNode : ShowGraphNode
    {
        protected ChoiceElement choiceContainer;
        protected Foldout portDescriptionContainer;
        
        // TODO: Public Properties for the choice data

        // TODO: FINISH
        public ChoiceNode() : base() => base.title = "Choice";

        public ChoiceNode(ObservableCollection<string> groups) : base(groups) => base.title = "Choice";

        public static ChoiceNode FromChoiceNodeData(Serialization.ChoiceNodeData choiceNodeData)
        {
            ChoiceNode choiceNode = ShowGraphNode.FromShowNodeData<ChoiceNode>(choiceNodeData);

            foreach (var group in choiceNodeData.KeyList)
            {
                // NOTE: Should we check if the group in the keylist is in the group selection?
                if (!choiceNode.choiceContainer.AddChoice(group, choiceNodeData.GroupChoices[group]))
                    Debug.LogError($"An error occured while loading choices for node {choiceNode.ID}: A choice for the group {group} already exists.");
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

            portDescriptionContainer = new Foldout() { text = "Port Descriptions" };
            portDescriptionContainer.value = false;
            extensionContainer.Add(portDescriptionContainer);

            /* Event Subs */
            choiceContainer.ChoicesChanged += OnChoicesChanged;

            // TODO: CUES
            base.InitializeUi();
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
        }

        protected void InitializePorts()
        {
            // Clear Ports
            foreach (Port port in outputContainer.Children())
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
            foreach (Port port in outputContainer.Children())
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
            Serialization.ChoiceNodeData nodeData = ToShowNodeData<Serialization.ChoiceNodeData>();

            nodeData.KeyList = new List<string>(choiceContainer.KeyList);
            nodeData.GroupChoices = new SerializableDictionary<string, string>(choiceContainer.GroupChoices);

            return nodeData;
        }

        public static explicit operator Serialization.ChoiceNodeData(ChoiceNode choiceNode) => choiceNode.ToChoiceNodeData();

        protected class ChoiceElement : VisualElement
        {
            protected Dictionary<string, string> groupChoices = new Dictionary<string, string>();
            
            // We need this to ensure the order of the keys
            public List<string> keyList = new List<string>();

            public Dictionary<string, string> GroupChoices => groupChoices;
            public List<string> KeyList => keyList;

            public event System.EventHandler ChoicesChanged;

            public bool AddChoice(string group) => AddChoice(group, $"{group} does somthing?");

            public bool AddChoice(string group, string question)
            {
                if (groupChoices.ContainsKey(group))
                {
                    // The Logging was going to get annoying 
                    //Debug.Log($"Choice for the group {group} already exists");
                    return false;
                }

                // Create Choice UI
                var choiceVE = new VisualElement() { name = $"{group}Choice" };
                choiceVE.Add(new Label($"{group}'s Question:"));

                var field = new TextField()
                {
                    name = $"{group}ChoiceTextField",
                    value = question,
                    tooltip = "Enter a yes/no question."
                };
                field.RegisterCallback<ChangeEvent<string>, string>(ValueChanged, group);
                choiceVE.Add(field);

                // Insert the Element
                this.Add(choiceVE);
                groupChoices.Add(group, field.value);
                keyList.Add(group);
                return true;
            }

            public bool RemoveChoice(string group)
            {
                if (!groupChoices.ContainsKey(group))
                {
                    // The Logging was going to get annoying 
                    //Debug.Log($"Choice for the group {group} already doesn't exist");
                    return false;
                }

                // Get Appropriate Child
                var child = Children().Single(ve => ve.name.StartsWith(group));

                // Unregister Value Change Callback
                // I decided to go the safer route when finding the TextField
                var field = child.Children().Single(ve => ve is TextField) as TextField;
                field.UnregisterCallback<ChangeEvent<string>, string>(ValueChanged);

                this.Remove(child);
                groupChoices.Remove(group);
                keyList.Remove(group);
                return true;
            }

            public void ValueChanged(ChangeEvent<string> changeEvent, string group)
            {
                groupChoices[group] = changeEvent.newValue;

                ChoicesChanged?.Invoke(this, new System.EventArgs());
            }
        }

    } 
}
