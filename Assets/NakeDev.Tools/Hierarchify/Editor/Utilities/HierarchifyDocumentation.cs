using UnityEngine;
using UnityEditor;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// ScriptableObject representing the documentation asset.
    /// Selecting this asset in the Project window displays a rich, interactive user guide in the Inspector.
    /// </summary>
    public class HierarchifyDocumentation : ScriptableObject
    {
        // This class is a sentinel object used to trigger the custom HierarchifyDocumentationEditor.
    }

    /// <summary>
    /// Automatically ensures that a Documentation.asset instance exists in the Assets/Hierarchify folder
    /// during compilation or editor startup.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchifyDocumentationAutoCreator
    {
        static HierarchifyDocumentationAutoCreator()
        {
            EditorApplication.delayCall += CheckAndCreateDocumentationAsset;
        }

        private static void CheckAndCreateDocumentationAsset()
        {
            // Avoid creating it if we are currently building or in runtime to be safe
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string rootFolder = "Assets/Hierarchify";
            string[] guids = AssetDatabase.FindAssets("t:MonoScript HierarchifyDocumentation");
            if (guids != null && guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                int editorIndex = scriptPath.IndexOf("/Editor/", System.StringComparison.OrdinalIgnoreCase);
                if (editorIndex != -1)
                {
                    rootFolder = scriptPath.Substring(0, editorIndex);
                }
            }

            string assetPath = rootFolder + "/Documentation.asset";

            var asset = AssetDatabase.LoadAssetAtPath<HierarchifyDocumentation>(assetPath);
            if (asset == null)
            {
                try
                {
                    // Ensure the parent directory is valid before asset creation
                    if (!AssetDatabase.IsValidFolder(rootFolder))
                    {
                        // Safely create the folder hierarchy if it is somehow missing
                        int lastSlash = rootFolder.LastIndexOf('/');
                        if (lastSlash != -1)
                        {
                            string parent = rootFolder.Substring(0, lastSlash);
                            string child = rootFolder.Substring(lastSlash + 1);
                            if (AssetDatabase.IsValidFolder(parent))
                            {
                                AssetDatabase.CreateFolder(parent, child);
                            }
                        }
                    }

                    var newDoc = ScriptableObject.CreateInstance<HierarchifyDocumentation>();
                    AssetDatabase.CreateAsset(newDoc, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                catch
                {
                    // Fail silently if asset database is locked or busy
                }
            }
        }
    }
}
