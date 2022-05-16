using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.IO;

namespace ShowGraphSystem.Editor
{
    public class ShowGraphEditor : EditorWindow
    {
        public const string DefaultFileName = "ShowGraph";

        private ShowGraphView showGraphView;
        private string fileName = DefaultFileName;

        [MenuItem("Window/UI Toolkit/ShowGraphEditor")]
        public static void ShowExample()
        {
            ShowGraphEditor wnd = GetWindow<ShowGraphEditor>();
            wnd.titleContent = new GUIContent("ShowGraphEditor");
        }

        public void CreateGUI()
        {
            showGraphView = AddGraphView();
            AddToolbar(showGraphView);
            //// Each editor window contains a root VisualElement object
            //VisualElement root = rootVisualElement;

            //// VisualElements objects can contain other VisualElement following a tree hierarchy.
            //VisualElement label = new Label("Hello World! From C#");
            //root.Add(label);

            //// Import UXML
            //var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ShowGraphEditor.uxml");
            //VisualElement labelFromUXML = visualTree.Instantiate();
            //root.Add(labelFromUXML);

            //// A stylesheet can be added to a VisualElement.
            //// The style will be applied to the VisualElement and all of its children.
            //var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/ShowGraphEditor.uss");
            //VisualElement labelWithStyle = new Label("Hello World! With Style");
            //labelWithStyle.styleSheets.Add(styleSheet);
            //root.Add(labelWithStyle);
        }

        private ShowGraphView AddGraphView()
        {
            // Create Our Graph View
            var graphView = new ShowGraphView();
            graphView.StretchToParentSize();

            rootVisualElement.Add(graphView);

            return graphView;
        }

        private void AddToolbar(ShowGraphView graphView)
        {
            if (graphView == null)
                throw new ArgumentNullException("GraphView Cannot be null, the toolbar requires a valid GraphView to function");

            // Create Toolbar UI
            var toolbar = new Toolbar();

            var fileNameTextField = new TextField()
            {
                value = DefaultFileName,
                label = "File Name: "
            };
            fileNameTextField.RegisterValueChangedCallback(change =>
            {
                fileName = change.newValue.Trim();
            });
            toolbar.Add(fileNameTextField);

            // TODO: on loading and saving - the textfield for the filename must match the file we loaded.
            var saveButton = new ToolbarButton(() =>
            {
                // TODO: Overwrite Warning
                ShowGraphSystemIO.SaveGraphToSO(showGraphView, !String.IsNullOrWhiteSpace(fileName) ? fileName : DefaultFileName);
            })
            { text = "Save" };
            toolbar.Add(saveButton);

            var loadButton = new ToolbarButton(() =>
            {
                try
                {
                    graphView.GenerateGraphFromData(
                        ShowGraphSystemIO.LoadGraphDataFromSO(
                            !String.IsNullOrWhiteSpace(fileName) ? fileName : DefaultFileName));
                }
                catch (FileNotFoundException fEx)
                {
                    EditorUtility.DisplayDialog("File Not Found", fEx.Message, "OK");
                }
            })
            { text = "Load" };
            toolbar.Add(loadButton);

            rootVisualElement.Add(toolbar);
        }
    } 
}