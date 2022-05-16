using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShowGraphSystem.Editor
{
    public class GroupSelectionChangedEventArgs : EventArgs
    {
        public ReadOnlyDictionary<string, bool> GroupSelection { get; set; }
    }

    public class GroupSelectionUI : VisualElement
    {
        protected Dictionary<string, Toggle> groupToggles = null;
        private ObservableCollection<string> groups;

        public ObservableCollection<string> Groups
        {
            get => groups;
            set
            {
                if (groups == value)
                    return;

                if (groups != null)
                    groups.CollectionChanged -= Groups_CollectionChanged;

                groups = value;
                if (groups != null)
                    groups.CollectionChanged += Groups_CollectionChanged;

                RefreshUI();
            }
        }

        private void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshUI();
        }

        public event EventHandler<GroupSelectionChangedEventArgs> GroupSelectionChanged;

        public GroupSelectionUI() : base()
        {

        }

        /// <summary>
        /// Generates the group UI in the Group Container.
        /// </summary>
        /// <remarks>
        /// The generated Group UI depends on the items in observable collection <see cref="Groups">Groups</see>.
        /// </remarks>
        /// <seealso cref="GroupContainer"/>
        public virtual void InitializeGroupsUI()
        {
            if (Groups == null) return;

            Button selectAllButton = new Button(() =>
            {
                if (groupToggles != null)
                    foreach (var toggle in groupToggles.Values)
                        toggle!.value = true;
            })
            {
                text = "Enable All",
                tooltip = "Toggles all groups to true."
            };
            this.Add(selectAllButton);

            groupToggles = new Dictionary<string, Toggle>(Groups.Count);
            foreach (var group in Groups)
            {
                Toggle groupToggle = new Toggle(group)
                {
                    name = group,
                };

                groupToggle.RegisterValueChangedCallback(OnGroupSelectionChanged);

                groupToggles[groupToggle.name] = groupToggle;
                this.Add(groupToggle);
            }
        }

        public void RefreshUI()
        {
            ReadOnlyDictionary<string, bool> selection = null;
            if (groupToggles != null)
            {
                selection = GetGroupSelection();

                foreach (var toggle in groupToggles.Values)
                    toggle.UnregisterValueChangedCallback(OnGroupSelectionChanged);
            }
            this.Clear();

            InitializeGroupsUI();

            if (selection != null)
                SetGroupSelection(new Dictionary<string, bool>(selection));
        }

        /// <summary>
        /// Generates an <see cref="ReadOnlyDictionary{string, bool}"/> 
        /// of the enabled status of the groups this node is apart of.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyDictionary{string, bool}"/> of the nodes grouping status, with the group names as the key.</returns>
        public ReadOnlyDictionary<string, bool> GetGroupSelection() =>
            new ReadOnlyDictionary<string, bool>(Groups.ToDictionary(g => g, g => groupToggles[g].value));

        public string[] GetSelectedGroups() => (from kv in groupToggles
                                                where kv.Value.value
                                                select kv.Key).ToArray();

        protected void OnGroupSelectionChanged(ChangeEvent<bool> changeEvent) => OnGroupSelectionChanged();

        protected void OnGroupSelectionChanged()
        {
            // Raise Grouping Changed Event
            GroupSelectionChanged?.Invoke(this, new GroupSelectionChangedEventArgs()
            {
                GroupSelection = GetGroupSelection()
            });
        }

        public void SetGroupSelection(Dictionary<string, bool> groupSelection) => SetGroupSelection(groupSelection, true);

        // TODO: Refactor this into a property
        protected void SetGroupSelection(Dictionary<string, bool> groupSelection, bool notify)
        {
            foreach (var groupSel in groupSelection)
            {
                if (groupToggles.ContainsKey(groupSel.Key))
                    groupToggles[groupSel.Key].SetValueWithoutNotify(groupSel.Value);
            }

            if (notify) OnGroupSelectionChanged();
        }
    } 
}