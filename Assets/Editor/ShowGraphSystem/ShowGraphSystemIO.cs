using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using System.IO;
using ShowGraphSystem.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

// Based on and uses code from https://github.com/Wafflus/unity-dialogue-system/blob/master/Assets/Editor/DialogueSystem/Utilities/DSIOUtility.cs
namespace ShowGraphSystem.Editor
{
    public static class ShowGraphSystemIO
    {
        public const string ShowGraphSystemName = "ShowGraphSystem";

        static ShowGraphSystemIO()
        {
            //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public static ShowGraphDataSO LoadGraphDataFromSO(string fullPath)
        {
            var g = (ShowGraphDataSO)AssetDatabase.LoadMainAssetAtPath(fullPath);

            if (g == null)
                throw new FileNotFoundException($"The Graph Asset file {fullPath} was not found.");

            return g;
        }

        public static ShowGraphDataSO LoadGraphDataFromSO(string assetName, string directory = "Assets", string extention = "asset")
        {
            string fullPath = $"{directory.Trim('/', ' ')}/{assetName}.{extention}";
            return LoadGraphDataFromSO(fullPath);
        }

#if UNITY_EDITOR

        public static void SaveGraphToSO(ShowGraphView showGraphView, string path)
        {
            var so = (ShowGraphDataSO)showGraphView;

            // TODO: REMOVE BE FOR RELEASE
            var runtimeGraph = Runtime.ShowGraph.GenerateGraphFromData(so);

            // Save The Graph
            AssetDatabase.CreateAsset(so, path);
        }

        public static void SaveGraphToSO(ShowGraphView showGraphView, string name, string directory = "Assets", string extention = "asset")
        {
            SaveGraphToSO(showGraphView, $"{directory.Trim('/', ' ')}/{name}.{extention}");
        }

        public static void SaveGraphToBinary(ShowGraphView showGraphView, string path)
        {
            var so = (ShowGraphDataSO)showGraphView;

            var runtimeGraph = Runtime.ShowGraph.GenerateGraphFromData(so);

            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                binaryFormatter.Serialize(fs, runtimeGraph);
            }
        }

        /// <inheritdoc cref="AssetDatabase.CreateFolder(string, string)"/>
        public static void CreateFolder(string parentFolderPath, string newFolderName)
        {
            if (AssetDatabase.IsValidFolder($"{parentFolderPath}/{newFolderName}"))
                return;

            AssetDatabase.CreateFolder(parentFolderPath, newFolderName);
        }

        private static void CreateDefaultFolders()
        {
            var editorPath = $"Assets/Editor/{ShowGraphSystemName}";

            // Create Editor Save path
            CreateFolder("Assets", "Editor");
            CreateFolder("Assets/Editor", ShowGraphSystemName);

            CreateFolder(editorPath, "Graphs");

            // TODO: Create Folders For Assets/ ?
        }

        //public static void SaveGraphToJsonFile(ShowGraphView showGraphView)
        //{
        //    // Get the Graph Data
        //    ShowGraphData graphSaveData = (ShowGraphData)showGraphView;

        //    using (StreamWriter writer = new StreamWriter($"/{graphSaveData.Name}.json"))
        //    {
        //        writer.Write(EditorJsonUtility.ToJson(graphSaveData));
        //    }
        //}
#endif

    } 
}
