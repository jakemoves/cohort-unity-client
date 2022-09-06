#define TESTING
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShowGraphSystem.Editor
{
    public partial class SceneNode
    {
        protected class GroupedCueReferencesUI : VisualElement
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
                        MediaDomain = MediaDomain.Image,
                        GroupSelection = new SerializableDictionary<string, bool>
                        (groups.ToDictionary<string, string, bool>(str => str, str => str == groupName)),
                        VibrateOnCue = false
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
                        CueListContainer.Insert(e.NewStartingIndex, CreateCueRefUI(cue));
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        CueListContainer.RemoveAt(e.OldStartingIndex);
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        CueListContainer.RemoveAt(e.OldStartingIndex);
                        CueListContainer.Insert(e.NewStartingIndex, CreateCueRefUI((CueReference)e.NewItems[0]));
                        break;

                    default:
                        RefreshCueList();
                        break;
                }
            }

            private void AddCueRef(CueReference cueRef)
            {
                CueReferenceUI ui = CreateCueRefUI(cueRef);
                CueListContainer.Add(ui);
            }

            private CueReferenceUI CreateCueRefUI(CueReference cueRef)
            {
                CueReferenceUI ui = new CueReferenceUI(cueRef, Groups, GroupName);

                ui.AddManipulator(new ContextualMenuManipulator(menuBuilder =>
                {
                    // Delete Cue
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

                    // Add Cue After
                    menuBuilder.menu.AppendAction("Add Cue Before", (action) =>
                    {
#if TESTING
                        Debug.Log($"Adding Cue before {cueRef.MediaDomain} Cue #{cueRef.CueID}");
#endif
                        // Add Cue
                        var nextCueRef = new CueReference()
                        {
                            MediaDomain = MediaDomain.Image,
                            GroupSelection = cueRef.GroupSelection,
                            VibrateOnCue = false
                        };

                        CueReferences.Insert(CueReferences.IndexOf(cueRef), nextCueRef);
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
        }
    }
}
