using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Static class responsible for registering all menu items and context menu integration points
    /// in the Unity Editor for Hierarchify.
    /// </summary>
    public static class HierarchyMenus
    {
        [MenuItem("Tools/NakeDev/Hierarchify... %&h", false, 0)]
        public static void OpenHierarchifyPanelMenu()
        {
            HierarchifyWindow.Open(null, 0);
        }

        [MenuItem("GameObject/NakeDev/Hierarchify...", false, 10)]
        public static void OpenHierarchifyPanelContextMenu(MenuCommand menuCommand)
        {
            HierarchifyWindow.Open(menuCommand != null ? menuCommand.context as GameObject : null, 0);
        }

        [MenuItem("GameObject/NakeDev/Change Color %&c", false, 15)]
        public static void ChangeColorContextMenu(MenuCommand menuCommand)
        {
            GameObject obj = Selection.activeGameObject;
            if (obj != null && obj.name.StartsWith("---") && obj.name.EndsWith("---"))
            {
                HierarchyRenderer.RequestColorPicker(obj);
            }
        }

        [MenuItem("GameObject/NakeDev/Change Color %&c", true)]
        public static bool ValidateChangeColorContextMenu()
        {
            GameObject active = Selection.activeGameObject;
            return active != null && active.name.StartsWith("---") && active.name.EndsWith("---");
        }

        [MenuItem("GameObject/NakeDev/Delete Separator (Safe)", false, 20)]
        public static void DeleteSeparatorSafeContextMenu(MenuCommand menuCommand)
        {
            if (menuCommand == null) return;
            GameObject obj = menuCommand.context as GameObject;
            if (obj != null && obj.name.StartsWith("---") && obj.name.EndsWith("---"))
            {
                List<GameObject> targetList = new List<GameObject> { obj };
                HierarchySafeDeleter.DeleteSeparatorsWithConfirmation(targetList, new GameObject[] { obj });
            }
        }

        [MenuItem("GameObject/NakeDev/Delete Separator (Safe)", true)]
        public static bool ValidateDeleteSeparatorSafeContextMenu()
        {
            GameObject active = Selection.activeGameObject;
            return active != null && active.name.StartsWith("---") && active.name.EndsWith("---");
        }
    }
}
