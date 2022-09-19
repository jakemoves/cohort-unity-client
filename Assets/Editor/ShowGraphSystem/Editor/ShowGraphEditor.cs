using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.IO;
using System.Linq;
using static UnityEditor.Progress;

namespace ShowGraphSystem.Editor
{
    public class ShowGraphEditor : EditorWindow
    {
        public const string DefaultFileName = "ShowGraph";
        public const string DefaultFolder = "Assets";
        public const string DefaultExtention = "asset";

        private ShowGraphView showGraphView;

        private string graphFilePath = null;

        public string GraphFilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(graphFilePath))
                    return $"{DefaultFolder}/{DefaultFileName}.{DefaultExtention}";
                return graphFilePath;
            }

            protected set
            {
                graphFilePath = value;
                PathChanged.Invoke(this, value);
            }
        }

        public event EventHandler<string> PathChanged;

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

            // TODO: on loading and saving - the textfield for the filename must match the file we loaded.
            var saveButton = new ToolbarButton(() => SaveGraph(graphView))
            { text = "Save" };
            toolbar.Add(saveButton);

            var saveAsButton = new ToolbarButton(() => SaveGraphAs(graphView))
            { text = "Save As" };
            toolbar.Add(saveAsButton);

            var loadButton = new ToolbarButton(() => OpenGraph(graphView))
            { text = "Load" };
            toolbar.Add(loadButton);

            var fileLabel = new Label() { };
            PathChanged += (sender, s) =>
            {
                fileLabel.text = $"{Path.GetFileNameWithoutExtension(s)} ({s})";
            };
            //toolbar.Add(fileLabel);

            //var qlabOsc = new ToolbarButton(() => ExportQLabInfo(showGraphView, graphFilePath))
            //{ text = "Export Q-Lab OSC" };

            toolbar.Add(new ToolbarSpacer() { flex = true });

            var validateButton = new ToolbarButton(() => Validation.ShowGraphValidator.ValidateGraph(this.showGraphView))
            { text = "Validate" };
            toolbar.Add(validateButton);

            var dataDropdown = new ToolbarMenu() { text = "Data" };
            dataDropdown.menu.AppendAction(
                "Export Q-Lab Choice Command OSC", 
                (action) => ExportQLabInfo(showGraphView, graphFilePath));

            dataDropdown.menu.AppendAction(
                "Export Node Data to TSV", 
                (action) =>
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        "Save TSV",
                        $"node-data_{Path.GetFileNameWithoutExtension(GraphFilePath)}",
                        "tsv",
                        "Enter Show Graph Filename",
                        Path.GetDirectoryName(GraphFilePath));

                    if (string.IsNullOrEmpty(path)) return;

                    ExportNodeDataTsv(new FileInfo(path), graphView);
                });

            dataDropdown.menu.AppendAction(
                "Import Cue Data from TSV",
                (action) =>
                {
                    var path = EditorUtility.OpenFilePanel(
                        "Import TSV",
                        Path.GetDirectoryName(GraphFilePath),
                        "tsv"
                        );

                    if (path == null) return;
                    var file = new FileInfo(path);
                    if (!file.Exists) return;

                    ImportCueDataTsv(file, graphView);
                });
            toolbar.Add(dataDropdown);

            rootVisualElement.Add(fileLabel);
            rootVisualElement.Add(toolbar);
        }

        private void ImportCueDataTsv(FileInfo file, ShowGraphView graphView)
        {
            var nodes = graphView.nodes.ToList();
            using var reader = file.OpenText();

            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                var data = line.Split('\t');

                if (data.Length != 5)
                {
                    Debug.LogError($"Could not import line - length mismatch: {line}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(data[0]))
                {
                    Debug.LogError($"Could not import line - node null: {line}");
                    continue;
                }

                var node = nodes
                    .Where(n => n is ShowGraphNode)
                    .Select(n => n as ShowGraphNode)
                    .SingleOrDefault(sgn => sgn.ID == data[0].Trim());

                if (node is null)
                {
                    Debug.LogError($"Could not import line - invalid node guid: {line}");
                    continue;
                }

                if (node is SceneNode scene && scene.CueListsByGroup.ContainsKey(data[1].Trim()))
                {
                    scene.CueListsByGroup[data[1].Trim()].Add(new CueReference()
                    {
                        CueID = int.Parse(data[2].Trim()),
                        VibrateOnCue = false,
                        MediaDomain = GetFromMime(data[3].Trim()),
                        GroupSelection = 
                            new SerializableDictionary<string, bool>(graphView.Groups.ToDictionary<string, string, bool>(g => g, g => data[4].Contains(g)))
                    });;
                }
            }

            MediaDomain GetFromMime(string s) 
            {
                s = s.Trim();
                if (s.StartsWith("audio"))
                    return MediaDomain.Sound;
                else if (s.StartsWith("video"))
                    return MediaDomain.Video;
                else if (s.StartsWith("image"))
                    return MediaDomain.Image;
                else throw new NotSupportedException($"Mime Type Not Supported: {s}");
            }
        }

        private void SaveGraph(ShowGraphView graphView)
        {
            // Null Root Warning
            if (graphView.Root is null)
            {
                EditorUtility.DisplayDialog("No Root Node Found", "Please add a Root Node before saving.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(graphFilePath))
            {
                SaveGraphAs(graphView);
                return;
            }

            // Overwrite Warning
            if (File.Exists(GraphFilePath) &&
                !EditorUtility.DisplayDialog("Confirm Save", $"{Path.GetFileName(GraphFilePath)} already exists.\nDo you want to overwrite it?", "Yes", "Cancel"))
                return;

            try
            {
                ShowGraphSystemIO.SaveGraphToSO(showGraphView, GraphFilePath);
            }
            catch (Exception ex)
            {
                // NOTE: This doesnt really help users other than to inform them an error occured
                EditorUtility.DisplayDialog("Error Occurred While Saving!", $"{ex.Message}\n(See Console for more details)", "OK");
                throw ex;
            }
        }

        private void SaveGraphAs(ShowGraphView graphView)
        {
            // Save File Dialogue
            // TODO: Error Checking
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Show Graph As",
                Path.GetFileNameWithoutExtension(GraphFilePath),
                DefaultExtention,
                "Enter Show Graph Filename",
                Path.GetDirectoryName(GraphFilePath));

            if (!string.IsNullOrWhiteSpace(path))
            {
                GraphFilePath = path;

                SaveGraph(graphView);
            }
        }

        private void OpenGraph(ShowGraphView graphView)
        {
            // Open File Dialogue
            var path = EditorUtility.OpenFilePanel("Open Show Graph Asset", Path.GetDirectoryName(GraphFilePath), DefaultExtention);

            if (string.IsNullOrWhiteSpace(path))
                return;

            // Set Current Path
            GraphFilePath = path.Replace(Application.dataPath, DefaultFolder);
            // Try Loading
            try
            {
                graphView.GenerateGraphFromData(
                    ShowGraphSystemIO.LoadGraphDataFromSO(GraphFilePath));
            }
            catch (FileNotFoundException fEx)
            {
                EditorUtility.DisplayDialog("File Not Found", fEx.Message, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error Occurred while loading", $"{ex.Message}\n(See Console for more details)", "OK");
            }
        }

        public static void ExportQLabInfo(ShowGraphView showGraphView, string assetPath = null)
        {
            System.Text.StringBuilder output = new System.Text.StringBuilder();
            output.AppendLine("Q-Lab Ouput for OSC via CoHort-OSC-Bridge");
            output.AppendLine("CoHot-OSC-Bridge: https://github.com/jakemoves/cohort-osc-bridge");
            output.AppendLine("\n");
            output.AppendLine($"Graph: {assetPath}");
            output.AppendLine($"Information Valid as of {DateTime.UtcNow} UTC");
            output.AppendLine("\n");

            showGraphView.nodes.ForEach(node =>
            {
                if (!(node is ChoiceNode))
                    return;

                var choiceNodeData = (node as ChoiceNode).ToChoiceNodeData();

                output.AppendLine("\n========== CHOICE NODE ==========");
                output.AppendLine($"\tID:\t{choiceNodeData.ID}");

                foreach (var groupID in choiceNodeData.GroupSelection.Where(kv => kv.Value).Select(kv => kv.Key))
                {
                    var decisionCommand = new DecisionCommand(choiceNodeData.ID, groupID);

                    output.AppendLine($"\n\t\t**** GROUP: {groupID} ****");
                    output.AppendLine($"\t\tQuestion:{choiceNodeData.GroupChoices[groupID]}");
                    output.AppendLine($"\n\t\t\t** OSC STRINGS **");
                    output.AppendLine($"/cohort 3 -1 0 \"all\" \"{decisionCommand}\"");

                    decisionCommand.Decision = !decisionCommand.Decision;
                    output.AppendLine($"/cohort 3 -1 0 \"all\" \"{decisionCommand}\"");
                }
            });

            var fn = !(assetPath is null) ? Path.GetFileNameWithoutExtension(assetPath) : "";
            using (var writer = new StreamWriter($"./{fn}-QLab_OSC.txt"))
            {
                writer.Write(output);
            }

            //System.Diagnostics.Process.Start($"./{fn}-QLab_OSC.txt");
        }

        public static void ExportNodeDataTsv(FileInfo file, ShowGraphView showGraph)
        {
            using var writer = file.CreateText();

            showGraph.nodes.ForEach((node) =>
            {
                if (!(node is ShowGraphNode showNode)) return;

                // Columns are GUID, Name, Groups <Array>, Type
                var data = new string[4]
                {
                    showNode.ID,
                    showNode.title,
                    string.Join(",", showNode.GetGroupSelection().Where(kv => kv.Value).Select(kv => kv.Key)),
                    showNode switch {
                        SceneNode scene => "Scene",
                        ChoiceNode choiceNode => "Choice",
                        _ => "Abstract"
                    }
                };

                writer.WriteLine(string.Join("\t", data));
            });
        }
    }
}