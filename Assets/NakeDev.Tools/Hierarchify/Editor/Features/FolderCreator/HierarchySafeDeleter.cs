using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Utility class that handles bulk, reference-safe deletion for custom hierarchy category folders.
    /// Ensures nested child GameObjects are preserved and cleanly extracted instead of accidentally lost.
    /// </summary>
    public static class HierarchySafeDeleter
    {
        /// <summary>
        /// Prompts the user with a confirmation dialog on how to handle child GameObjects,
        /// then executes the chosen deletion strategy.
        /// </summary>
        public static void DeleteSeparatorsWithConfirmation(List<GameObject> separators, GameObject[] entireSelection)
        {
            if (separators == null || separators.Count == 0) return;

            int totalChildCount = 0;
            foreach (GameObject folder in separators)
            {
                if (folder != null)
                {
                    totalChildCount += folder.transform.childCount;
                }
            }

            if (totalChildCount > 0)
            {
                bool isPtBr = HierarchySentinel.IsPortuguese();
                string title = isPtBr ? "Hierarchify - Exclusão Segura" : "Hierarchify - Safe Delete";
                
                string message;
                if (isPtBr)
                {
                    message = separators.Count == 1
                        ? $"Você está prestes a excluir a pasta de categoria '{ExtractBaseName(separators[0].name)}' que contém {totalChildCount} GameObjects filhos."
                        : $"Você está prestes a excluir {separators.Count} pastas de categoria que contêm no total {totalChildCount} GameObjects filhos.";
                    message += "\n\nO que você deseja fazer com os objetos filhos?";
                }
                else
                {
                    message = separators.Count == 1
                        ? $"You are about to delete the category folder '{ExtractBaseName(separators[0].name)}' containing {totalChildCount} nested GameObjects."
                        : $"You are about to delete {separators.Count} category folders containing a total of {totalChildCount} nested GameObjects.";
                    message += "\n\nWhat would you like to do with the nested children?";
                }

                string btnExtract = isPtBr ? "Extrair e Salvar filhos" : "Extract and Save all children";
                string btnCancel = isPtBr ? "Cancelar Operação" : "Cancel Operation";
                string btnDeleteAll = isPtBr ? "Excluir Tudo (Pastas e Filhos)" : "Delete Everything (Folders and Children)";

                int choice = EditorUtility.DisplayDialogComplex(title, message, btnExtract, btnCancel, btnDeleteAll);

                if (choice == 0) // Extract and Save
                {
                    ExtractChildrenAndDestroyBulk(separators, entireSelection);
                }
                else if (choice == 2) // Delete Everything
                {
                    DestroySelectionBulk(entireSelection);
                }
                // choice == 1 is Cancel, do nothing
            }
            else
            {
                DestroySelectionBulk(entireSelection);
            }
        }

        private static string ExtractBaseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Replace("-", "").Trim();
        }

        /// <summary>
        /// Performs a bulk extraction of nested child objects to their direct parent or scene root, 
        /// and securely destroys the parent folder separators with full Undo support.
        /// </summary>
        /// <param name="separators">List of selected separator folders to delete safely.</param>
        /// <param name="entireSelection">Complete active selection array from the Editor window.</param>
        public static void ExtractChildrenAndDestroyBulk(List<GameObject> separators, GameObject[] entireSelection)
        {
            if (separators == null || separators.Count == 0) return;

            Undo.IncrementCurrentGroup();
            int undoGroupID = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Safe Delete Separators Bulk");

            int totalExtracted = 0;

            // 1. Traverse and safely detach nested children from category folders
            foreach (GameObject folder in separators)
            {
                if (folder == null) continue;

                int childCount = folder.transform.childCount;
                if (childCount == 0) continue;

                Transform folderTransform = folder.transform;
                Transform parentTransform = folderTransform.parent;

                List<Transform> children = new List<Transform>();
                for (int i = 0; i < childCount; i++)
                {
                    children.Add(folderTransform.GetChild(i));
                }

                foreach (Transform child in children)
                {
                    Undo.SetTransformParent(child, parentTransform, "Extract " + child.name);
                    totalExtracted++;
                }
            }

            // 2. Safely destroy all target parent separators and accompanying selection
            foreach (GameObject obj in entireSelection)
            {
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            Undo.CollapseUndoOperations(undoGroupID);
            
            if (totalExtracted > 0)
            {
                Debug.Log($"<color=#1ea3ff><b>[NakeDev Scene Organizer]</b></color> Safe delete completed! {totalExtracted} child GameObjects extracted to root/parent.");
            }
        }

        /// <summary>
        /// Securely destroys all objects in the selection array with single-operation undo registration.
        /// </summary>
        /// <param name="selection">Array of GameObjects to destroy immediately.</param>
        public static void DestroySelectionBulk(GameObject[] selection)
        {
            if (selection == null || selection.Length == 0) return;

            Undo.IncrementCurrentGroup();
            int undoGroupID = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Delete Selection Bulk");

            foreach (GameObject obj in selection)
            {
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            Undo.CollapseUndoOperations(undoGroupID);
        }
    }
}
