using UnityEngine;
using UnityEditor;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Light, dynamic popup window that allows changing a separator folder's color scheme 
    /// directly from a color selector field or clicking on a harmonic palette preset.
    /// </summary>
    public class ColorPickerPopup : PopupWindowContent
    {
        private GameObject _targetFolder;
        private Color _currentColor;

        /// <summary>
        /// Instantiates a new color picker popup targeting a specific folder.
        /// </summary>
        public ColorPickerPopup(GameObject targetFolder, Color currentColor)
        {
            _targetFolder = targetFolder;
            _currentColor = currentColor;
        }

        /// <summary>
        /// Declares the window dimensions.
        /// </summary>
        public override Vector2 GetWindowSize()
        {
            return new Vector2(175, 110);
        }

        /// <summary>
        /// Renders the GUI and handles interaction.
        /// </summary>
        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginArea(new Rect(8, 8, rect.width - 16, rect.height - 16));
            
            GUILayout.Label("Change Color", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            _currentColor = EditorGUILayout.ColorField(_currentColor);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyColor(_currentColor);
            }

            EditorGUILayout.Space(6);

            GUILayout.Label("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < HierarchySettings.PresetColors.Length; i++)
            {
                Color color = HierarchySettings.PresetColors[i];
                GUI.backgroundColor = color;
                if (GUILayout.Button("", GUILayout.Width(22), GUILayout.Height(22)))
                {
                    _currentColor = color;
                    ApplyColor(_currentColor);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Commits the selected color parameter to the target separator, renaming the GameObject with the new Hex string.
        /// </summary>
        private void ApplyColor(Color color)
        {
            if (_targetFolder == null) return;

            string name = _targetFolder.name;
            string cleanName = name.Replace("-", "").Trim();

            int hashIndex = cleanName.LastIndexOf('#');
            if (hashIndex != -1)
            {
                cleanName = cleanName.Substring(0, hashIndex).Trim();
            }

            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            string finalName = $"--- {cleanName.ToUpper().Trim()} #{hexColor} ---";

            Undo.RecordObject(_targetFolder, "Change Folder Color");
            _targetFolder.name = finalName;

            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
