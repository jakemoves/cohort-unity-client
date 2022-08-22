#define TESTING
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ShowGraphSystem.Editor
{
    public class CueReferenceUI : VisualElement
    {
        private Foldout foldout;
        private EnumField mediaDomainField;
        private Button setGroupsButton;
        private IntegerField cueNumberField;
        private Toggle vibrateOnCueField;

        private ObservableCollection<string> groups;

        public bool Expanded
        {
            get => foldout.value;
            set => foldout.value = value;
        }

        private CueReference _cueData;
        public CueReference CueData
        {
            get => _cueData;
            set
            {
                _cueData = value;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (!(mediaDomainField is null))
                mediaDomainField.value = CueData.MediaDomain;

            if (!(cueNumberField is null))
                cueNumberField.value = CueData.CueID;

            if (!(vibrateOnCueField is null))
                vibrateOnCueField.value = CueData.VibrateOnCue;

            if (!(setGroupsButton is null))
                OnUpdateSelection();

            if (!(foldout is null))
                UpdateFoldoutText();
        }

        public ObservableCollection<string> Groups
        {
            get => groups;
            set
            {
                if (groups != null)
                    groups.CollectionChanged -= Groups_CollectionChanged;

                groups = value;

                if (groups != null)
                {
                    groups.CollectionChanged += Groups_CollectionChanged;
                }
                UpdateGroupSelectionDictionary();
            }
        }

        public IDictionary<string, bool> GroupSelection
        {
            get => CueData.GroupSelection;
            private set => CueData.GroupSelection = new SerializableDictionary<string, bool>(value);
        }

        public CueReferenceUI(CueReference cueReferenceData, ObservableCollection<string> groups, string groupName = null) : base()
        {
            if (cueReferenceData is null)
                throw new System.ArgumentNullException(nameof(cueReferenceData));

            CueData = cueReferenceData;
            Groups = groups;

            /* Add Visual Elements */
            // Foldout
            foldout = new Foldout()
            {
                text = "Empty Reference",
                value = false
            };
            this.Add(foldout);

            // Media Domain
            mediaDomainField = new EnumField(MediaDomain.Image);
            foldout.Add(mediaDomainField);
            mediaDomainField.RegisterValueChangedCallback((EventCallback<ChangeEvent<System.Enum>>)(change =>
            {
                CueData.MediaDomain = (MediaDomain)change.newValue;

                // Change Foldout Text
                UpdateFoldoutText();
            }));

            if (cueReferenceData.MediaDomain != MediaDomain.Image)
                mediaDomainField.value = cueReferenceData.MediaDomain;

            // Group
            setGroupsButton = new Button(() =>
            {
                UnityEditor.PopupWindow.Show(setGroupsButton.LocalToWorld(setGroupsButton.contentRect),
                        new GroupSelectionPopupWindow(new Dictionary<string, bool>(GroupSelection)) { CloseAction = newSel => OnUpdateSelection(newSel) });
            })
            {
                text = "No Group Selected"
            };
            foldout.Add(setGroupsButton);

            // Cue ID Number
            cueNumberField = new IntegerField("Cue Number")
            {
                value = 0
            };
            foldout.Add(cueNumberField);
            cueNumberField.RegisterValueChangedCallback((EventCallback<ChangeEvent<int>>)(change =>
            {
                // Do not let the value go below 0
                if (change.newValue < 0)
                    cueNumberField.value = 0;

                CueData.CueID = change.newValue;

                // Change Foldout Text
                UpdateFoldoutText();
            }));

            if (cueReferenceData.CueID != 0)
                cueNumberField.value = cueReferenceData.CueID;

            // Vibrate On Cue
            vibrateOnCueField = new Toggle("Vibrate On Cue");
            foldout.Add(vibrateOnCueField);
            vibrateOnCueField.RegisterValueChangedCallback((EventCallback<ChangeEvent<bool>>)(change =>
            {
                CueData.VibrateOnCue = change.newValue;

                // Change Foldout Text
                UpdateFoldoutText();
            }));
            vibrateOnCueField.value = cueReferenceData.VibrateOnCue;

            UpdateFoldoutText();

            // Select the groupName
            // TODO!: Test
            if (groupName != null && GroupSelection.Count > 0)
            {
                if (GroupSelection.ContainsKey(groupName))
                {
                    GroupSelection[groupName] = true;
                    OnUpdateSelection();
                }
            }
        }

        private void OnUpdateSelection(IDictionary<string, bool> selection = null)
        {
            if (selection != null)
                this.GroupSelection = selection;

            var selectedGroups = GetSelectedGroups();

            if (selectedGroups.Length == 0)
            {
                // TODO: Warn if not part of any group
                setGroupsButton.text = "No Group Selected";
            }
            else
                setGroupsButton.text = $"Groups: {string.Join(",", GetSelectedGroups())}";
        }

        private string UpdateFoldoutText() => foldout.text = $"{mediaDomainField.text} Cue #{cueNumberField.value} {GetVibrationLabelText()}";
        private string GetVibrationLabelText() => CueData.VibrateOnCue ? "(w/ Vibrate)" : null;

        private void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => UpdateGroupSelectionDictionary();

        public string[] GetSelectedGroups() => (from kv in GroupSelection
                                                where kv.Value
                                                select kv.Key).ToArray();

        private void UpdateGroupSelectionDictionary()
        {
            Dictionary<string, bool> newSelectionDict = new Dictionary<string, bool>();

            if (groups != null)
                foreach (var groupName in groups)
                {
                    if (GroupSelection != null && GroupSelection.ContainsKey(groupName))
                        newSelectionDict[groupName] = GroupSelection[groupName];
                    else
                        newSelectionDict[groupName] = false;
                }

            GroupSelection = newSelectionDict;
        }
    }
}