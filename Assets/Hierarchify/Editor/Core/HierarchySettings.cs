using UnityEngine;
using UnityEditor;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Visibility modes for the component icons in the hierarchy.
    /// </summary>
    public enum ComponentIconsVisibility
    {
        Always = 0,
        OnHover = 1,
        OnCtrlPressed = 2
    }

    /// <summary>
    /// Global settings and configuration system for the Hierarchify Editor Tool.
    /// Defines default colors, folder structures, and naming helper utilities.
    /// </summary>
    public static class HierarchySettings
    {
        /// <summary>
        /// Quick preset palette of vibrant, harmonic colors for custom folder styling.
        /// </summary>
        public static Color[] PresetColors
        {
            get
            {
                string data = EditorPrefs.GetString("Hierarchify_Theme_PresetColors", "");
                if (string.IsNullOrEmpty(data))
                {
                    return new Color[]
                    {
                        new Color(0.18f, 0.49f, 0.96f, 1f), // Elegant Blue (Management)
                        new Color(0.15f, 0.68f, 0.37f, 1f), // Emerald Green (Environment)
                        new Color(0.90f, 0.30f, 0.20f, 1f), // Crimson Red (Gameplay)
                        new Color(0.60f, 0.30f, 0.80f, 1f), // Amethyst Purple (UI)
                        new Color(0.95f, 0.70f, 0.10f, 1f), // Amber Yellow (System)
                        new Color(0.50f, 0.50f, 0.50f, 1f)  // Slate Grey (Neutral)
                    };
                }
                string[] parts = data.Split(';');
                System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();
                foreach (var part in parts)
                {
                    if (ColorUtility.TryParseHtmlString(part, out Color col)) colors.Add(col);
                }
                return colors.Count > 0 ? colors.ToArray() : new Color[] { ColorDefault };
            }
        }

        /// <summary>
        /// Default pre-configured category folder structure generated upon initialization.
        /// </summary>
        public static readonly string[] FolderNames = new string[]
        {
            "--- MANAGEMENT #2E7DF5 ---",
            "--- ENVIRONMENT #27AE60 ---",
            "--- GAMEPLAY #E64C33 ---",
            "--- UI / HUD #994CCC ---",
            "--- SYSTEM / LIGHTING #F3B31A ---"
        };

        /// <summary>
        /// The default accent color fallback for uncategorized folders.
        /// </summary>
        public static readonly Color ColorDefault = new Color(0.50f, 0.50f, 0.50f, 1f);

        /// <summary>
        /// Returns the corresponding category accent color based on keywords in the folder name.
        /// </summary>
        public static Color GetAccentColorForFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return ColorDefault;

            string upperName = folderName.ToUpperInvariant();
            if (upperName.Contains("MANAGEMENT")) return new Color(0.18f, 0.49f, 0.96f, 1f);
            if (upperName.Contains("ENVIRONMENT")) return new Color(0.15f, 0.68f, 0.37f, 1f);
            if (upperName.Contains("GAMEPLAY")) return new Color(0.90f, 0.30f, 0.20f, 1f);
            if (upperName.Contains("UI")) return new Color(0.60f, 0.30f, 0.80f, 1f);
            if (upperName.Contains("SYSTEM")) return new Color(0.95f, 0.70f, 0.10f, 1f);

            return ColorDefault;
        }

        /// <summary>
        /// Extracts the raw, human-readable category name from a separator string, removing hashes and hex codes.
        /// </summary>
        public static string ExtractBaseName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return string.Empty;

            string clean = fullName.Replace("-", "").Trim();
            int hashIndex = clean.LastIndexOf('#');
            if (hashIndex != -1)
            {
                clean = clean.Substring(0, hashIndex).Trim();
            }
            return clean;
        }

        private const string PrefsShowComponentIcons = "Hierarchify_ShowComponentIcons";
        private const string PrefsAllowComponentToggling = "Hierarchify_AllowComponentToggling";
        private const string PrefsComponentIconsOffset = "Hierarchify_ComponentIconsOffset";
        private const string PrefsComponentIconSize = "Hierarchify_ComponentIconSize";
        private const string PrefsComponentIconSpacing = "Hierarchify_ComponentIconSpacing";
        private const string PrefsComponentIconsMode = "Hierarchify_ComponentIconsMode";

        /// <summary>
        /// Controls whether the component list/icons are drawn in the hierarchy.
        /// </summary>
        public static bool ShowComponentIcons
        {
            get => EditorPrefs.GetBool(PrefsShowComponentIcons, false);
            set => EditorPrefs.SetBool(PrefsShowComponentIcons, value);
        }

        /// <summary>
        /// Controls when component icons are displayed.
        /// </summary>
        public static ComponentIconsVisibility ComponentIconsMode
        {
            get => (ComponentIconsVisibility)EditorPrefs.GetInt(PrefsComponentIconsMode, (int)ComponentIconsVisibility.OnHover);
            set => EditorPrefs.SetInt(PrefsComponentIconsMode, (int)value);
        }

        /// <summary>
        /// Controls whether clicking the icons enables/disables the components.
        /// </summary>
        public static bool AllowComponentToggling
        {
            get => EditorPrefs.GetBool(PrefsAllowComponentToggling, false);
            set => EditorPrefs.SetBool(PrefsAllowComponentToggling, value);
        }

        /// <summary>
        /// The starting offset in pixels from the right side of the hierarchy row where component icons begin.
        /// </summary>
        public static float ComponentIconsOffset
        {
            get => EditorPrefs.GetFloat(PrefsComponentIconsOffset, 72f);
            set => EditorPrefs.SetFloat(PrefsComponentIconsOffset, value);
        }

        /// <summary>
        /// The size of each component icon in pixels.
        /// </summary>
        public static float ComponentIconSize
        {
            get => EditorPrefs.GetFloat(PrefsComponentIconSize, 16f);
            set => EditorPrefs.SetFloat(PrefsComponentIconSize, value);
        }

        /// <summary>
        /// The spacing between component icons in pixels.
        /// </summary>
        public static float ComponentIconSpacing
        {
            get => EditorPrefs.GetFloat(PrefsComponentIconSpacing, 2f);
            set => EditorPrefs.SetFloat(PrefsComponentIconSpacing, value);
        }

        private const string PrefsShowGameObjectActiveToggle = "Hierarchify_ShowGameObjectActiveToggle";
        private const string PrefsActiveToggleOffset = "Hierarchify_ActiveToggleOffset";

        /// <summary>
        /// Controls whether the active toggle eye icon is shown.
        /// </summary>
        public static bool ShowGameObjectActiveToggle
        {
            get => EditorPrefs.GetBool(PrefsShowGameObjectActiveToggle, false);
            set => EditorPrefs.SetBool(PrefsShowGameObjectActiveToggle, value);
        }

        /// <summary>
        /// The starting offset in pixels from the right side of the hierarchy row where the active toggle eye icon is drawn.
        /// </summary>
        public static float ActiveToggleOffset
        {
            get => EditorPrefs.GetFloat(PrefsActiveToggleOffset, 35f);
            set => EditorPrefs.SetFloat(PrefsActiveToggleOffset, value);
        }

        private const string PrefsShowHierarchySentinel = "Hierarchify_ShowHierarchySentinel";
        private const string PrefsSentinelOffset = "Hierarchify_SentinelOffset";
        private const string PrefsShowVertexCount = "Hierarchify_ShowVertexCount";

        /// <summary>
        /// Controls whether the Hierarchy Sentinel warning/error icons are shown.
        /// </summary>
        public static bool ShowHierarchySentinel
        {
            get => EditorPrefs.GetBool(PrefsShowHierarchySentinel, false);
            set => EditorPrefs.SetBool(PrefsShowHierarchySentinel, value);
        }

        /// <summary>
        /// The starting offset in pixels from the right side of the hierarchy row where the Sentinel icon is drawn.
        /// </summary>
        public static float SentinelOffset
        {
            get => EditorPrefs.GetFloat(PrefsSentinelOffset, 52f);
            set => EditorPrefs.SetFloat(PrefsSentinelOffset, value);
        }

        /// <summary>
        /// Controls whether the mesh vertex count is shown in the hierarchy.
        /// </summary>
        public static bool ShowVertexCount
        {
            get => EditorPrefs.GetBool(PrefsShowVertexCount, false);
            set => EditorPrefs.SetBool(PrefsShowVertexCount, value);
        }

        private const string PrefsColorEntireRow = "Hierarchify_ColorEntireRow";
        private const string PrefsShowInspectorBar = "Hierarchify_ShowInspectorBar";

        /// <summary>
        /// Controls whether the custom Inspector navigation and bookmarking bar is shown.
        /// </summary>
        public static bool ShowInspectorBar
        {
            get => EditorPrefs.GetBool(PrefsShowInspectorBar, true);
            set => EditorPrefs.SetBool(PrefsShowInspectorBar, value);
        }

        /// <summary>
        /// Controls whether the entire folder background row is painted with the category color.
        /// If false, only a small left accent bar is colored.
        /// </summary>
        public static bool ColorEntireRow
        {
            get => EditorPrefs.GetBool(PrefsColorEntireRow, false);
            set => EditorPrefs.SetBool(PrefsColorEntireRow, value);
        }

        private const string PrefsShowTreeLines = "Hierarchify_ShowTreeLines";
        private const string PrefsHighlightActiveTreeLines = "Hierarchify_HighlightActiveTreeLines";
        private const string PrefsShowCustomFolderIcons = "Hierarchify_ShowCustomFolderIcons";
        private const string PrefsTreeLineOpacity = "Hierarchify_TreeLineOpacity";
        private const string PrefsTreeLineDashSize = "Hierarchify_TreeLineDashSize";
        private const string PrefsFolderFontSize = "Hierarchify_FolderFontSize";

        /// <summary>
        /// Controls whether the relation tree guidelines are shown.
        /// </summary>
        public static bool ShowTreeLines
        {
            get => EditorPrefs.GetBool(PrefsShowTreeLines, false);
            set => EditorPrefs.SetBool(PrefsShowTreeLines, value);
        }

        /// <summary>
        /// Controls whether tree guidelines for the selected object path are highlighted with the category accent color.
        /// </summary>
        public static bool HighlightActiveTreeLines
        {
            get => EditorPrefs.GetBool(PrefsHighlightActiveTreeLines, false);
            set => EditorPrefs.SetBool(PrefsHighlightActiveTreeLines, value);
        }

        /// <summary>
        /// Controls whether default foldout arrows are replaced by custom folder/opened-folder icons on categories.
        /// </summary>
        public static bool ShowCustomFolderIcons
        {
            get => EditorPrefs.GetBool(PrefsShowCustomFolderIcons, false);
            set => EditorPrefs.SetBool(PrefsShowCustomFolderIcons, value);
        }

        /// <summary>
        /// Transparency alpha of standard hierarchy relationship guidelines.
        /// </summary>
        public static float TreeLineOpacity
        {
            get => EditorPrefs.GetFloat(PrefsTreeLineOpacity, 0.5f);
            set => EditorPrefs.SetFloat(PrefsTreeLineOpacity, value);
        }

        /// <summary>
        /// The height of dashes in relation guidelines.
        /// </summary>
        public static float TreeLineDashSize
        {
            get => EditorPrefs.GetFloat(PrefsTreeLineDashSize, 3f);
            set => EditorPrefs.SetFloat(PrefsTreeLineDashSize, value);
        }

        /// <summary>
        /// Font size used to render category folder labels.
        /// </summary>
        public static int FolderFontSize
        {
            get => EditorPrefs.GetInt(PrefsFolderFontSize, 11);
            set => EditorPrefs.SetInt(PrefsFolderFontSize, value);
        }

        private const string PrefsPinnedScenes = "Hierarchify_PinnedScenes";

        /// <summary>
        /// Retrieves the list of pinned scene asset paths.
        /// </summary>
        public static System.Collections.Generic.List<string> GetPinnedScenes()
        {
            string data = EditorPrefs.GetString(PrefsPinnedScenes, "");
            if (string.IsNullOrEmpty(data)) return new System.Collections.Generic.List<string>();
            return new System.Collections.Generic.List<string>(data.Split(';'));
        }

        /// <summary>
        /// Saves the list of pinned scene asset paths.
        /// </summary>
        public static void SetPinnedScenes(System.Collections.Generic.List<string> list)
        {
            if (list == null) list = new System.Collections.Generic.List<string>();
            EditorPrefs.SetString(PrefsPinnedScenes, string.Join(";", list));
        }
    }
}
