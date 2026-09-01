using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// The unified Control Center and configuration window for Hierarchify.
    /// Combines folder creation, settings toggle, advanced offset controls, and quick tasks.
    /// Supports docking and full responsiveness.
    /// </summary>
    public class HierarchifyWindow : EditorWindow
    {
        private int _currentTab = 0;
        private readonly string[] _tabNames = new string[] { "Presets", "Folders", "Icons", "Filters", "Settings", "Offsets" };

        // Scroll state for responsiveness
        private Vector2 _scrollPosition;

        // Folder Creator Data
        private string _folderName = "NEW CATEGORY";
        private Color _folderColor = new Color(0.18f, 0.49f, 0.96f, 1f); // Default Accent Blue
        private GameObject _targetContext;

        /// <summary>
        /// Opens the control window.
        /// </summary>
        public static void Open(GameObject targetContext = null, int initialTab = 0)
        {
            // utility = false allows docking and standard resizing
            HierarchifyWindow window = GetWindow<HierarchifyWindow>(false, "Hierarchify", true);
            window.minSize = new Vector2(400, 320);
            window.maxSize = new Vector2(1600, 1200); // Massive allowance for full layout freedom

            if (targetContext != null)
            {
                Selection.activeGameObject = targetContext;
            }
            window._currentTab = initialTab;
            window.Show();

            // Set dimensions AFTER Show() to override Unity's cached layout state.
            // Opens with 480 width and 520 height to fit all settings and toolbars comfortably without scrollbars.
            if (!window.docked)
            {
                window.position = new Rect(window.position.x, window.position.y, 480, 520);
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void OnDisable()
        {
            HierarchyFilterManager.ClearFilter();
        }

        private void OnGUI()
        {
            // Centering layout on wide screens to prevent infinite stretching
            float contentWidth = Mathf.Min(position.width - 24f, 500f);
            float sidePadding = (position.width - contentWidth) / 2f;

            // Header panel (Static)
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(sidePadding);
            EditorGUILayout.BeginVertical(GUILayout.Width(contentWidth), GUILayout.MaxWidth(contentWidth), GUILayout.ExpandWidth(false));

            GUILayout.Label("HIERARCHIFY CONTROL CENTER", new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = new Color(0.18f, 0.49f, 0.96f, 1f) }
            });

            EditorGUILayout.Space(6);

            // Tab selection toolbar (stretches dynamically and wraps responsively on narrow windows)
            if (position.width < 450f)
            {
                string[] tabNamesRow1 = new string[] { "Presets", "Folders", "Icons" };
                string[] tabNamesRow2 = new string[] { "Filters", "Settings", "Offsets" };

                int selectedRow1 = _currentTab < 3 ? _currentTab : -1;
                int selectedRow2 = _currentTab >= 3 ? _currentTab - 3 : -1;

                EditorGUILayout.BeginVertical();
                int newRow1 = GUILayout.Toolbar(selectedRow1, tabNamesRow1);
                int newRow2 = GUILayout.Toolbar(selectedRow2, tabNamesRow2);
                EditorGUILayout.EndVertical();

                if (newRow1 != selectedRow1 && newRow1 != -1) _currentTab = newRow1;
                if (newRow2 != selectedRow2 && newRow2 != -1) _currentTab = newRow2 + 3;
            }
            else
            {
                _currentTab = GUILayout.Toolbar(_currentTab, _tabNames);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
            GUILayout.Space(sidePadding);
            EditorGUILayout.EndHorizontal();

            // Responsive Scroll View Content (automatically expands to fill available vertical space)
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(sidePadding);
            EditorGUILayout.BeginVertical(GUILayout.Width(contentWidth), GUILayout.MaxWidth(contentWidth), GUILayout.ExpandWidth(false));

            // Set fixed label width based on active tab to ensure readability and legibility
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            if (_currentTab == 1 || _currentTab == 2 || _currentTab == 3)
            {
                EditorGUIUtility.labelWidth = 100f;
            }
            else
            {
                EditorGUIUtility.labelWidth = 195f; // Gives ample room for Settings and Offset labels
            }

            switch (_currentTab)
            {
                case 0:
                    DrawPresets();
                    break;
                case 1:
                    DrawFolderCreator();
                    break;
                case 2:
                    DrawIconPicker();
                    break;
                case 3:
                    HierarchyFilterManager.DrawFilterGUI();
                    break;
                case 4:
                    DrawSettings();
                    break;
                case 5:
                    DrawOffsets();
                    break;
            }

            EditorGUIUtility.labelWidth = originalLabelWidth;

            EditorGUILayout.EndVertical();
            GUILayout.Space(sidePadding);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawFolderCreator()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(6);

            _folderName = EditorGUILayout.TextField("Folder Name", _folderName);
            EditorGUILayout.Space(6);
            _folderColor = EditorGUILayout.ColorField("Folder Color", _folderColor);
            EditorGUILayout.Space(8);

            GUILayout.Label("Quick Palette:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int i = 0; i < HierarchySettings.PresetColors.Length; i++)
            {
                Color color = HierarchySettings.PresetColors[i];
                GUI.backgroundColor = color;
                if (GUILayout.Button("", GUILayout.Width(24), GUILayout.Height(24)))
                {
                    _folderColor = color;
                    GUI.FocusControl(null);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Live Preview Box (fully responsive width layout matching hierarchy visual style)
            GUILayout.Label("Live Hierarchy Preview:", EditorStyles.miniLabel);
            Rect previewRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            Rect previewBoxRect = new Rect(previewRect.x, previewRect.y + 1, previewRect.width, previewRect.height - 2);

            Color previewBgColor;
            Color previewTextColor;

            if (HierarchySettings.ColorEntireRow)
            {
                previewBgColor = _folderColor;
                float luminance = (0.299f * _folderColor.r) + (0.587f * _folderColor.g) + (0.114f * _folderColor.b);
                previewTextColor = luminance > 0.6f ? new Color(0.1f, 0.1f, 0.1f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
            }
            else
            {
                previewBgColor = new Color(0.22f, 0.22f, 0.22f, 1f);
                previewTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            }

            // Paint the background
            EditorGUI.DrawRect(previewBoxRect, previewBgColor);
            Rect bottomBorder = new Rect(previewBoxRect.x, previewBoxRect.yMax - 1, previewBoxRect.width, 1);
            EditorGUI.DrawRect(bottomBorder, new Color(0.16f, 0.16f, 0.16f, HierarchySettings.ColorEntireRow ? 0.15f : 1f));

            if (!HierarchySettings.ColorEntireRow)
            {
                // Draw 4px left accent bar
                Rect previewAccentBar = new Rect(previewRect.x, previewRect.y + 1, 4, previewRect.height - 2);
                EditorGUI.DrawRect(previewAccentBar, _folderColor);
            }

            GUIStyle previewLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = HierarchySettings.FolderFontSize,
            };

            // Draw label with outline
            Color previewOutlineColor = (previewTextColor.r < 0.5f) 
                ? new Color(0.95f, 0.95f, 0.95f, 0.6f) 
                : new Color(0.1f, 0.1f, 0.1f, 0.6f);
            DrawTextWithOutline(previewBoxRect, _folderName.ToUpper().Trim(), previewLabelStyle, previewTextColor, previewOutlineColor, 1.2f);

            EditorGUILayout.Space(14);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            if (GUILayout.Button("Create Styled Folder", btnStyle, GUILayout.Height(34)))
            {
                string hexColor = ColorUtility.ToHtmlStringRGB(_folderColor);
                string finalName = $"--- {_folderName.ToUpper().Trim()} #{hexColor} ---";

                Undo.IncrementCurrentGroup();
                int undoGroupID = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Create Custom Folder");

                GameObject folder = new GameObject(finalName);
                folder.transform.position = Vector3.zero;
                folder.transform.rotation = Quaternion.identity;
                folder.transform.localScale = Vector3.one;

                if (_targetContext != null)
                {
                    GameObjectUtility.SetParentAndAlign(folder, _targetContext);
                }
                else if (Selection.activeGameObject != null)
                {
                    GameObjectUtility.SetParentAndAlign(folder, Selection.activeGameObject);
                }

                Undo.RegisterCreatedObjectUndo(folder, "Create " + _folderName);
                Undo.CollapseUndoOperations(undoGroupID);

                Selection.activeGameObject = folder;
                EditorGUIUtility.PingObject(folder);
                Close();
            }
        }

        private void DrawSettings()
        {
            bool isPtBr = HierarchySentinel.IsPortuguese();

            // --- SECTION 1: COMPONENT ICONS ---
            GUILayout.Label(isPtBr ? "Ícones de Componentes" : "Component Icons Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            bool showIcons = EditorGUILayout.Toggle(isPtBr ? "Mostrar Ícones de Componentes" : "Show Component Icons", HierarchySettings.ShowComponentIcons);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowComponentIcons = showIcons;
                EditorApplication.RepaintHierarchyWindow();
            }

            if (HierarchySettings.ShowComponentIcons)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(2);

                EditorGUI.BeginChangeCheck();
                ComponentIconsVisibility mode = (ComponentIconsVisibility)EditorGUILayout.EnumPopup(isPtBr ? "Modo de Visibilidade" : "Visibility Mode", HierarchySettings.ComponentIconsMode);
                if (EditorGUI.EndChangeCheck())
                {
                    HierarchySettings.ComponentIconsMode = mode;
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUILayout.Space(2);

                EditorGUI.BeginChangeCheck();
                bool allowToggle = EditorGUILayout.Toggle(isPtBr ? "Alternar Ativo/Inativo ao Clicar" : "Allow Icon Toggling", HierarchySettings.AllowComponentToggling);
                if (EditorGUI.EndChangeCheck())
                {
                    HierarchySettings.AllowComponentToggling = allowToggle;
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // --- SECTION 2: FOLDER STYLING & GUIDELINES ---
            GUILayout.Label(isPtBr ? "Estilo & Visual das Pastas" : "Folder Styling & Visuals", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            bool colorEntire = EditorGUILayout.Toggle(isPtBr ? "Colorir Linha Inteira (Pastas)" : "Color Entire Folder Row", HierarchySettings.ColorEntireRow);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ColorEntireRow = colorEntire;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            bool customFolderIcons = EditorGUILayout.Toggle(isPtBr ? "Mostrar Ícones de Pasta" : "Show Custom Folder Icons", HierarchySettings.ShowCustomFolderIcons);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowCustomFolderIcons = customFolderIcons;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            bool showTree = EditorGUILayout.Toggle(isPtBr ? "Linhas de Relação (Árvore)" : "Show Relation Guidelines", HierarchySettings.ShowTreeLines);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowTreeLines = showTree;
                EditorApplication.RepaintHierarchyWindow();
            }

            if (HierarchySettings.ShowTreeLines)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(2);

                EditorGUI.BeginChangeCheck();
                bool highlightTree = EditorGUILayout.Toggle(isPtBr ? "Destacar Guias do Objeto Ativo" : "Highlight Active Guidelines", HierarchySettings.HighlightActiveTreeLines);
                if (EditorGUI.EndChangeCheck())
                {
                    HierarchySettings.HighlightActiveTreeLines = highlightTree;
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // --- SECTION 3: FEATURES & DIAGNOSTICS ---
            GUILayout.Label(isPtBr ? "Recursos & Diagnósticos" : "Features & Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            bool showEye = EditorGUILayout.Toggle(isPtBr ? "Olho de Ativação (Active Eye)" : "Show Active Toggle Eye", HierarchySettings.ShowGameObjectActiveToggle);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowGameObjectActiveToggle = showEye;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            bool showSentinel = EditorGUILayout.Toggle(isPtBr ? "Alertas do Sentinel (Linter/Erros)" : "Show Sentinel Warnings", HierarchySettings.ShowHierarchySentinel);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowHierarchySentinel = showSentinel;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            bool showVertex = EditorGUILayout.Toggle(isPtBr ? "Contagem de Vértices 3D" : "Show Mesh Vertex Count", HierarchySettings.ShowVertexCount);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowVertexCount = showVertex;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            bool showInspectorBar = EditorGUILayout.Toggle(isPtBr ? "Mostrar Barra do Inspector" : "Show Inspector Bar", HierarchySettings.ShowInspectorBar);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ShowInspectorBar = showInspectorBar;
                var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                if (inspectorType != null)
                {
                    var inspectors = Resources.FindObjectsOfTypeAll(inspectorType);
                    foreach (var inspector in inspectors)
                    {
                        if (inspector is EditorWindow win) win.Repaint();
                    }
                }
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12);

            HierarchyUpdater.DrawUpdaterGUI();
        }

        private void DrawOffsets()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(6);

            GUILayout.Label("Spacings & Offsets", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            float minComponents = HierarchySettings.SentinelOffset + 20f;
            EditorGUI.BeginChangeCheck();
            float compOffset = EditorGUILayout.Slider("Component Icons Offset", Mathf.Max(HierarchySettings.ComponentIconsOffset, minComponents), minComponents, 200f);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ComponentIconsOffset = compOffset;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            float compSize = EditorGUILayout.Slider("Component Icon Size", HierarchySettings.ComponentIconSize, 8f, 24f);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ComponentIconSize = compSize;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            float compSpacing = EditorGUILayout.Slider("Icon Spacing", HierarchySettings.ComponentIconSpacing, 0f, 8f);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ComponentIconSpacing = compSpacing;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            float eyeOffset = EditorGUILayout.Slider("Active Eye Offset", HierarchySettings.ActiveToggleOffset, 10f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.ActiveToggleOffset = eyeOffset;
                if (HierarchySettings.SentinelOffset < eyeOffset + 17f)
                {
                    HierarchySettings.SentinelOffset = eyeOffset + 17f;
                }
                if (HierarchySettings.ComponentIconsOffset < HierarchySettings.SentinelOffset + 20f)
                {
                    HierarchySettings.ComponentIconsOffset = HierarchySettings.SentinelOffset + 20f;
                }
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(4);

            float minSentinel = HierarchySettings.ActiveToggleOffset + 17f;
            EditorGUI.BeginChangeCheck();
            float sentinelOffset = EditorGUILayout.Slider("Sentinel Icon Offset", Mathf.Max(HierarchySettings.SentinelOffset, minSentinel), minSentinel, 150f);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.SentinelOffset = sentinelOffset;
                if (HierarchySettings.ComponentIconsOffset < sentinelOffset + 20f)
                {
                    HierarchySettings.ComponentIconsOffset = sentinelOffset + 20f;
                }
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.Space(12);
            GUILayout.Label("Folder & Guideline Style", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            int fontSize = EditorGUILayout.IntSlider("Folder Font Size", HierarchySettings.FolderFontSize, 8, 16);
            if (EditorGUI.EndChangeCheck())
            {
                HierarchySettings.FolderFontSize = fontSize;
                EditorApplication.RepaintHierarchyWindow();
            }

            using (new EditorGUI.DisabledScope(!HierarchySettings.ShowTreeLines))
            {
                EditorGUILayout.Space(4);

                EditorGUI.BeginChangeCheck();
                float opacity = EditorGUILayout.Slider("Guideline Opacity", HierarchySettings.TreeLineOpacity, 0.1f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    HierarchySettings.TreeLineOpacity = opacity;
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUILayout.Space(4);

                EditorGUI.BeginChangeCheck();
                float dashSize = EditorGUILayout.Slider("Guideline Dash Size", HierarchySettings.TreeLineDashSize, 1f, 10f);
                if (EditorGUI.EndChangeCheck())
                {
                    HierarchySettings.TreeLineDashSize = dashSize;
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            EditorGUILayout.Space(14);

            if (GUILayout.Button("Reset Offsets & Styling"))
            {
                HierarchySettings.ComponentIconsOffset = 72f;
                HierarchySettings.ComponentIconSize = 16f;
                HierarchySettings.ComponentIconSpacing = 2f;
                HierarchySettings.ActiveToggleOffset = 35f;
                HierarchySettings.SentinelOffset = 52f;
                HierarchySettings.FolderFontSize = 11;
                HierarchySettings.TreeLineOpacity = 0.5f;
                HierarchySettings.TreeLineDashSize = 3f;
                EditorApplication.RepaintHierarchyWindow();
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        private void DrawPresets()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(6);

            GUILayout.Label("Structure Spawning", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Standard Folder Structure", GUILayout.Height(26)))
            {
                HierarchyBuilder.CreateFolderStructure(null);
            }

            EditorGUILayout.Space(12);

            GUILayout.Label("Safety Actions", EditorStyles.boldLabel);
            
            bool hasSeparatorSelected = false;
            GameObject selectedObj = Selection.activeGameObject;
            if (selectedObj != null && selectedObj.name.StartsWith("---") && selectedObj.name.EndsWith("---"))
            {
                hasSeparatorSelected = true;
            }

            using (new EditorGUI.DisabledScope(!hasSeparatorSelected))
            {
                if (GUILayout.Button("Safe Delete Selected Separator", GUILayout.Height(26)))
                {
                    List<GameObject> targets = new List<GameObject> { selectedObj };
                    HierarchySafeDeleter.DeleteSeparatorsWithConfirmation(targets, new GameObject[] { selectedObj });
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            // Reserve layout space for the footer (18px high)
            Rect footerRect = EditorGUILayout.GetControlRect(false, 18f);
            
            // Draw footer background
            EditorGUI.DrawRect(footerRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            // Draw a subtle top border/separator line for the footer
            Rect topBorder = new Rect(footerRect.x, footerRect.y, footerRect.width, 1f);
            EditorGUI.DrawRect(topBorder, new Color(0.1f, 0.1f, 0.1f, 0.4f));

            GUIStyle footerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
            };

            // Draw the label with right padding
            Rect textRect = new Rect(footerRect.x, footerRect.y, footerRect.width - 8f, footerRect.height);
            EditorGUI.LabelField(textRect, "Hierarchify © NakeDev - All rights reserved", footerStyle);
        }

        private static void DrawTextWithOutline(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, float outlineWidth = 1f)
        {
            Color originalColor = style.normal.textColor;
            style.normal.textColor = outlineColor;

            // Draw outline in 8 directions
            EditorGUI.LabelField(new Rect(rect.x - outlineWidth, rect.y, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x + outlineWidth, rect.y, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x, rect.y - outlineWidth, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x, rect.y + outlineWidth, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x - outlineWidth, rect.y - outlineWidth, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x + outlineWidth, rect.y - outlineWidth, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x - outlineWidth, rect.y + outlineWidth, rect.width, rect.height), text, style);
            EditorGUI.LabelField(new Rect(rect.x + outlineWidth, rect.y + outlineWidth, rect.width, rect.height), text, style);

            // Draw center text
            style.normal.textColor = textColor;
            EditorGUI.LabelField(rect, text, style);

            style.normal.textColor = originalColor;
        }
        // --- Icon Picker Integration ---

        private static readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();
        private bool _pickingCustom = false;
        private GameObject _customPickerTarget;

        private void DrawIconPicker()
        {
            GameObject target = Selection.activeGameObject;
            bool isPtBr = HierarchySentinel.IsPortuguese();

            if (target == null)
            {
                string emptyMsg = isPtBr 
                    ? "Selecione um GameObject na Hierarquia para definir seu ícone." 
                    : "Select a GameObject in the Hierarchy to set its icon.";
                EditorGUILayout.HelpBox(emptyMsg, MessageType.Info);
                return;
            }

            // Check if category folder
            bool isCategory = target.name.StartsWith("---") && target.name.EndsWith("---");
            if (isCategory)
            {
                string catMsg = isPtBr
                    ? "Categorias não suportam customização de ícones (apenas cores)."
                    : "Categories do not support icon customization (colors only).";
                EditorGUILayout.HelpBox(catMsg, MessageType.Warning);
                return;
            }

            // Target Name header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string targetLabel = isPtBr ? "Alvo" : "Target";
            GUILayout.Label($"{targetLabel}: {target.name}", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // 1. Labels Section
            string labelsTitle = isPtBr ? "Etiquetas / Labels" : "Labels";
            GUILayout.Label(labelsTitle, EditorStyles.boldLabel);
            DrawIconGrid(target, new string[] {
                "sv_label_0", "sv_label_1", "sv_label_2", "sv_label_3",
                "sv_label_4", "sv_label_5", "sv_label_6", "sv_label_7"
            }, 8);

            GUILayout.Space(10);

            // 2. Dots Section
            string dotsTitle = isPtBr ? "Pontos Coloridos" : "Dots";
            GUILayout.Label(dotsTitle, EditorStyles.boldLabel);
            DrawIconGrid(target, new string[] {
                "sv_icon_dot0_sml", "sv_icon_dot1_sml", "sv_icon_dot2_sml", "sv_icon_dot3_sml",
                "sv_icon_dot4_sml", "sv_icon_dot5_sml", "sv_icon_dot6_sml", "sv_icon_dot7_sml"
            }, 8);

            GUILayout.Space(10);

            // 3. Common Section
            string commonTitle = isPtBr ? "Componentes & Objetos" : "Common Components & Objects";
            GUILayout.Label(commonTitle, EditorStyles.boldLabel);
            DrawIconGrid(target, new string[] {
                "cs Script Icon", "Prefab Icon", "Camera Icon", "Light Icon",
                "AudioSource Icon", "SettingsIcon", "Folder Icon", "Canvas Icon",
                "MeshRenderer Icon", "BoxCollider Icon", "Animator Icon", "ParticleSystem Icon",
                "Favorite Icon", "scenevis_visible", "LockIcon", "console.infoicon"
            }, 8);

            GUILayout.Space(15);

            // Footer Actions
            EditorGUILayout.BeginHorizontal();
            
            string clearLabel = isPtBr ? "Limpar Ícone" : "Clear Icon";
            if (GUILayout.Button(clearLabel, GUILayout.Height(26)))
            {
                Undo.RecordObject(target, isPtBr ? "Remover Ícone do GameObject" : "Clear GameObject Icon");
                EditorGUIUtility.SetIconForObject(target, null);
                EditorUtility.SetDirty(target);
                EditorApplication.RepaintHierarchyWindow();
            }

            string customLabel = isPtBr ? "Textura Customizada..." : "Custom Texture...";
            if (GUILayout.Button(customLabel, GUILayout.Height(26)))
            {
                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", controlID);
                _pickingCustom = true;
                _customPickerTarget = target;
            }

            EditorGUILayout.EndHorizontal();

            // Handle custom picker selection
            if (_pickingCustom && Event.current.commandName == "ObjectSelectorUpdated")
            {
                Texture2D selected = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
                if (_customPickerTarget != null)
                {
                    Undo.RecordObject(_customPickerTarget, isPtBr ? "Definir Ícone Customizado" : "Set Custom Icon");
                    EditorGUIUtility.SetIconForObject(_customPickerTarget, selected);
                    EditorUtility.SetDirty(_customPickerTarget);
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
            else if (_pickingCustom && Event.current.commandName == "ObjectSelectorClosed")
            {
                _pickingCustom = false;
                _customPickerTarget = null;
            }
        }

        private void DrawIconGrid(GameObject target, string[] iconNames, int columns)
        {
            Texture2D currentIcon = EditorGUIUtility.GetIconForObject(target);

            GUILayout.BeginHorizontal();
            int drawnCount = 0;
            for (int i = 0; i < iconNames.Length; i++)
            {
                string iconName = iconNames[i];
                Texture2D tex = GetIconTexture(iconName);
                if (tex == null) continue;

                if (drawnCount > 0 && drawnCount % columns == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                bool isActive = (currentIcon == tex);

                // Highlight active selection
                if (isActive)
                {
                    GUI.backgroundColor = new Color(0.18f, 0.49f, 0.96f, 1f); // Accent Blue
                }
                else
                {
                    GUI.backgroundColor = Color.white;
                }

                if (GUILayout.Button(new GUIContent(tex, iconName), GUILayout.Width(28), GUILayout.Height(28)))
                {
                    bool isPtBr = HierarchySentinel.IsPortuguese();
                    Undo.RecordObject(target, isPtBr ? "Alterar Ícone do GameObject" : "Set GameObject Icon");
                    EditorGUIUtility.SetIconForObject(target, tex);
                    EditorUtility.SetDirty(target);
                    EditorApplication.RepaintHierarchyWindow();
                }

                drawnCount++;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private Texture2D GetIconTexture(string name)
        {
            if (!_iconCache.TryGetValue(name, out Texture2D tex))
            {
                try
                {
                    var content = EditorGUIUtility.IconContent(name);
                    tex = content != null ? content.image as Texture2D : null;
                }
                catch
                {
                    tex = null;
                }
                _iconCache[name] = tex;
            }
            return tex;
        }
    }
}
