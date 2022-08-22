using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

namespace ShowGraphSystem.Editor
{
    public partial class ChoiceNode
    {
        public class ChoiceElement : VisualElement
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
