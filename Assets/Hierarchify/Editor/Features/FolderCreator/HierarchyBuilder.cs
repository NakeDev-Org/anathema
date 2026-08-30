using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Static class responsible for creating and spawning the scene category structure.
    /// Handles name uniqueness, multi-scene environments, and standard positioning parameters.
    /// </summary>
    public static class HierarchyBuilder
    {
        /// <summary>
        /// Generates the standard folder structure with custom category names, ignoring duplicate folders.
        /// </summary>
        /// <param name="menuCommand">The contextual menu command details when executed via right-click context menu.</param>
        public static void CreateFolderStructure(MenuCommand menuCommand)
        {
            List<GameObject> createdObjects = new List<GameObject>();
            List<GameObject> existingObjects = new List<GameObject>();

            Undo.IncrementCurrentGroup();
            int undoGroupID = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Hierarchy Folders");

            foreach (string name in HierarchySettings.FolderNames)
            {
                string baseNameToCheck = HierarchySettings.ExtractBaseName(name);
                GameObject existing = FindFolderIgnoringHex(baseNameToCheck);
                
                if (existing != null)
                {
                    existingObjects.Add(existing);
                    continue;
                }

                GameObject folder = new GameObject(name);
                folder.transform.position = Vector3.zero;
                folder.transform.rotation = Quaternion.identity;
                folder.transform.localScale = Vector3.one;

                if (menuCommand != null && menuCommand.context != null)
                {
                    GameObjectUtility.SetParentAndAlign(folder, menuCommand.context as GameObject);
                }

                Undo.RegisterCreatedObjectUndo(folder, "Create " + name);
                createdObjects.Add(folder);
            }

            if (createdObjects.Count > 0)
            {
                Debug.Log($"<color=#1ea3ff><b>[NakeDev Scene Organizer]</b></color> Spawned {createdObjects.Count} new category folders!");
                Selection.objects = createdObjects.ToArray();
            }

            if (existingObjects.Count > 0)
            {
                string msg = "The following folders already existed in the active scene(s) and were not duplicated:\n";
                foreach (var obj in existingObjects)
                {
                    msg += $"- {HierarchySettings.ExtractBaseName(obj.name)}\n";
                }
                EditorUtility.DisplayDialog("Scene Organizer", msg + "\nYou can use them to organize your items!", "Understood");
                
                if (createdObjects.Count == 0)
                {
                    Selection.objects = existingObjects.ToArray();
                }
            }

            Undo.CollapseUndoOperations(undoGroupID);
        }

        /// <summary>
        /// Utility to find an existing folder in any active scene by matching its name, ignoring the hex code.
        /// </summary>
        private static GameObject FindFolderIgnoringHex(string baseName)
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (var obj in rootObjects)
                    {
                        if (obj.name.StartsWith("---") && obj.name.EndsWith("---"))
                        {
                            if (HierarchySettings.ExtractBaseName(obj.name) == baseName)
                            {
                                return obj;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
