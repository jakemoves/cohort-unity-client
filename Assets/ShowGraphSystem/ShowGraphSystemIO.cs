#if UNITY_EDITOR
using UnityEditor;
#endif
using ShowGraphSystem.Serialization;

// Based on and uses code from https://github.com/Wafflus/unity-dialogue-system/blob/master/Assets/Editor/DialogueSystem/Utilities/DSIOUtility.cs
namespace ShowGraphSystem.Runtime
{
    public static class ShowGraphSystemIO
    {
        public const string ShowGraphSystemName = "ShowGraphSystem";
#if UNITY_EDITOR
        public static ShowGraphDataSO LoadGraphDataFromSO(string assetName, string path = "Assets")
        {
            string fullPath = $"{path.Trim('/', ' ')}/{assetName}.asset";
            var g = AssetDatabase.LoadAssetAtPath<ShowGraphDataSO>(fullPath);
            return g;
        }

        //public static void SaveGraphToAsset(ShowGraphView showGraphView, string path = "Assets")
        //{
        //    // Create Folders
        //    CreateDefaultFolders();

        //    var so = (ShowGraphDataSO)showGraphView;

        //    // Save The Graph
        //    AssetDatabase.CreateAsset(so, $"{path.Trim('/', ' ')}/{so.Name}.asset");

        //}

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
