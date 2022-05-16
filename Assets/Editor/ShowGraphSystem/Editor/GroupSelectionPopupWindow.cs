#define TESTING
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShowGraphSystem.Editor
{
    public class GroupSelectionPopupWindow : PopupWindowContent //UnityEngine.UIElements.PopupWindow
    {
        public Dictionary<string, bool> GroupSelection { get; set; }
        public Action<Dictionary<string, bool>> CloseAction { get; set; }

        public GroupSelectionPopupWindow(Dictionary<string, bool> groupSelection) : base()
        {
            GroupSelection = groupSelection;
        }

        public override void OnGUI(Rect rect)
        {
            //editorWindow.maxSize = new Vector2(180, 30 + (20 * (GroupSelection?.Count ?? 0)));

            GUILayout.Label("Select Groups:", EditorStyles.boldLabel);
            if (GroupSelection != null)
            {
                // You cant use the Dictionary directly because if it's modified the Enumerator wont work
                var keyValueCopies = GroupSelection.ToList();

                foreach (var kvCopy in keyValueCopies)
                    GroupSelection[kvCopy.Key] = EditorGUILayout.Toggle(kvCopy.Key, kvCopy.Value);
            }
        }

        public override void OnClose()
        {
            CloseAction?.Invoke(GroupSelection);
        }
    } 
}
