using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace ShowGraphSystem.Editor
{
    public partial class ChoiceNode
    {
        protected class TransitionCueContainer : VisualElement
        {
            private Label groupLabel;
            private Toggle hasTransitionToggle;
            private CueReferenceUI cueReferenceUI;

            public bool CueReferenceExpanded 
            { 
                get => cueReferenceUI.Expanded; 
                set => cueReferenceUI.Expanded = value; 
            }

            public CueReference CueData
            {
                get => cueReferenceUI.CueData;
                set
                {
                    if (value is null)
                        HasTransition = false;
                    else
                        cueReferenceUI.CueData = value;
                }
            }

            public ObservableCollection<string> Groups
            {
                get => cueReferenceUI.Groups;
                set => cueReferenceUI.Groups = value;
            }

            public IDictionary<string, bool> GroupSelection
            {
                get => CueData.GroupSelection;
                private set => CueData.GroupSelection = new SerializableDictionary<string, bool>(value);
            }

            public bool HasTransition
            {
                get => hasTransitionToggle.value;
                set
                {
                    hasTransitionToggle.value = value;

                    // Manually set just in case
                    cueReferenceUI.visible = value;
                    cueReferenceUI.Expanded = false;
                }
            }

            public TransitionCueContainer(ObservableCollection<string> groups, string groupName) : base()
            {
                if (groups is null)
                    throw new ArgumentNullException(nameof(groups));

                if (string.IsNullOrEmpty(groupName))
                    throw new ArgumentNullException(nameof(groupName));

                // CueReferenceUI
                cueReferenceUI = new CueReferenceUI(
                    new CueReference
                    {
                        CueID = 0,
                        GroupSelection = new SerializableDictionary<string, bool>(groups.ToDictionary<string, string, bool>(g => g, g => g == groupName)),
                        MediaDomain = MediaDomain.Sound,
                        VibrateOnCue = false
                    },
                    groups,
                    groupName)
                {
                    Expanded = false,
                    visible = false
                };

                // Group Label
                groupLabel = new Label(groupName);
                groupLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                groupLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);


                // Has Transition Toggle
                hasTransitionToggle = new Toggle("Has Transition Cue");
                hasTransitionToggle.RegisterValueChangedCallback((EventCallback<ChangeEvent<bool>>)(change =>
                {
                    if (change.newValue == change.previousValue) return;
                    cueReferenceUI.visible = change.newValue;
                    cueReferenceUI.Expanded = false;
                }));

                Add(groupLabel);
                Add(hasTransitionToggle);
                Add(cueReferenceUI);

                this.style.marginTop = new StyleLength(5);
            }
        }
    }
}