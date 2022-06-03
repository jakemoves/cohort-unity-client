#define TESTING
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShowGraphSystem.Editor
{
    public partial class SceneNode
    {
        protected partial class GroupedCueReferencesUI : VisualElement
        {
            private ObservableCollection<CueReference> cueReferences;
            private string groupName;
            private Foldout cueFoldout;

            public VisualElement CueListContainer { get; } = new VisualElement();
            public ObservableCollection<string> Groups { get; set; }
            public string GroupName
            {
                get => groupName;
                set
                {
                    groupName = value;

                    if (cueFoldout != null)
                        cueFoldout.text = $"{value}'s Cue List";

                    if (cueReferences != null)
                        foreach (var cueReference in cueReferences)
                        {
                            // NOTE: If the GUI dislays the names of
                            // TODO: Update Group Selection
                        }
                }
            }

            public ObservableCollection<CueReference> CueReferences
            {
                get => cueReferences;
                set
                {
                    // Unhook Events //
                    if (cueReferences != null)
                        cueReferences.CollectionChanged -= CueReferences_CollectionChanged;

                    // Set Value //
                    cueReferences = value;

                    // Hook up Events //
                    if (cueReferences != null)
                        cueReferences.CollectionChanged += CueReferences_CollectionChanged;

                    RefreshCueList();
                }
            }

            public GroupedCueReferencesUI(string groupName, ObservableCollection<string> groups) : base()
                => Initialize(groupName, groups, new ObservableCollection<CueReference>());
            public GroupedCueReferencesUI(string groupName, ObservableCollection<string> groups, ObservableCollection<CueReference> cueReferences) : base()
                => Initialize(groupName, groups, cueReferences);
            private void Initialize(string groupName, ObservableCollection<string> groups, ObservableCollection<CueReference> cueReferences)
            {
                Groups = groups;

                cueFoldout = new Foldout()
                {
                    value = false
                };
                this.Add(cueFoldout);

                // Put this here to cheat setting the foldout text
                GroupName = groupName;

                // Add Cue Button
                Button addCueButton = new Button(() =>
                {
                    // Add Cue
                    var cueRef = new CueReference()
                    {
                        MediaDomain = MediaDomain.Image
                    };

                    CueReferences.Add(cueRef);
                })
                {
                    text = $"Add Cue Reference for {groupName}"
                };
                cueFoldout.Add(addCueButton);
                cueFoldout.Add(CueListContainer);

                /* Init Observable Collection */
                CueReferences = cueReferences;
            }

            private void CueReferences_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                // The .NET Framework somtimes assumes that the change involes only one item
                // And the ObservableCollection has no methods to change more than one item at once
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        CueReference cue = (CueReference)e.NewItems[0];
                        CueListContainer.Insert(e.NewStartingIndex, CreatCueRefUI(cue));
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        CueListContainer.RemoveAt(e.OldStartingIndex);
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        CueListContainer.RemoveAt(e.OldStartingIndex);
                        CueListContainer.Insert(e.NewStartingIndex, CreatCueRefUI((CueReference)e.NewItems[0]));
                        break;

                    default:
                        RefreshCueList();
                        break;
                }
            }

            private void AddCueRef(CueReference cueRef)
            {
                CueReferenceUI ui = CreatCueRefUI(cueRef);
                CueListContainer.Add(ui);
            }

            private CueReferenceUI CreatCueRefUI(CueReference cueRef)
            {
                CueReferenceUI ui = new CueReferenceUI(cueRef, Groups, GroupName);

                ui.AddManipulator(new ContextualMenuManipulator(menuBuilder =>
                {
                    menuBuilder.menu.AppendAction("Delete Cue", (action) =>
                    {
#if TESTING
                        Debug.Log($"Removing {cueRef.MediaDomain} Cue #{cueRef.CueID}");
#endif
                        CueReferences.Remove(cueRef);
#if TESTING
                        Debug.Log($"Cue Size = {CueReferences.Count}");
#endif
                    });
                    menuBuilder.StopPropagation();
                }));
                return ui;
            }

            public void RefreshCueList()
            {
                // Clear Container
                CueListContainer.Clear();

                if (cueReferences == null) return;

                // Populate container
                foreach (var cue in CueReferences)
                    AddCueRef(cue);
            }

            protected partial class CueReferenceUI : VisualElement
            {
                private Foldout foldout;
                private EnumField mediaDomainField;
                private Button setGroupsButton;
                private IntegerField cueNumberField;

                private ObservableCollection<string> groups;

                public bool Expanded { get => foldout.value; set => foldout.value = value; }
                public CueReference Data { get => (CueReference)this.userData; }

#pragma warning disable IDE1006 // Naming Styles
                public new object userData
#pragma warning restore IDE1006 // Naming Styles
                {
                    get => base.userData;
                    set
                    {
                        // TODO: (CueReferenceUI) RESET UI
                        if (value is CueReference)
                            base.userData = value;
                        else
                            throw new System.InvalidOperationException("This VisualElement does not allow this type to be stored in this property.");
                    }
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

                public IDictionary<string, bool> GroupSelection { get => Data.GroupSelection; private set => Data.GroupSelection = new SerializableDictionary<string, bool>(value); }

                public CueReferenceUI(CueReference cueReferenceData, ObservableCollection<string> groups, string groupName = null) : base()
                {
                    base.userData = cueReferenceData;
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
                    mediaDomainField.RegisterValueChangedCallback(change =>
                    {
                        (userData as CueReference).MediaDomain = (MediaDomain)change.newValue;

                        // Change Foldout Text
                        UpdateFoldoutText();
                    });

                    if (cueReferenceData.MediaDomain != MediaDomain.Image)
                        mediaDomainField.value = cueReferenceData.MediaDomain;

                    // Group
                    setGroupsButton = new Button(() =>
                    {
                        // TODO: Grouping popup
                        UnityEditor.PopupWindow.Show(setGroupsButton.LocalToWorld(setGroupsButton.contentRect),
                                new GroupSelectionPopupWindow(new Dictionary<string, bool>(GroupSelection)) { CloseAction = newSel => OnUpdateSelection(newSel) });
                    })
                    {
                        text = "No Group Selected"
                    };
                    foldout.Add(setGroupsButton);

                    cueNumberField = new IntegerField("Cue Number")
                    {
                        value = 0
                    };
                    foldout.Add(cueNumberField);
                    cueNumberField.RegisterValueChangedCallback(change =>
                    {
                        // Do not let the value go below 0
                        if (change.newValue < 0)
                            cueNumberField.value = 0;

                        (userData as CueReference).CueID = change.newValue;

                        // Change Foldout Text
                        UpdateFoldoutText();
                    });

                    if (cueReferenceData.CueID != 0)
                        cueNumberField.value = cueReferenceData.CueID;

                    UpdateFoldoutText();

                    // Select the groupName
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

                private string UpdateFoldoutText() => foldout.text = $"{mediaDomainField.text} Cue #{cueNumberField.value}";

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
    }
}
