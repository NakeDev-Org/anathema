using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Manages the dynamic hierarchy filtering system, scanning active scene components
    /// and providing fading/dimming filters for clean scene navigation.
    /// </summary>
    public static class HierarchyFilterManager
    {
        private static Type _selectedType = null;
        private static bool _filterActive = false;

        // Cache lists to avoid GC allocations during search / GUI
        private static readonly List<Type> _uniqueTypes = new List<Type>();
        private static readonly Dictionary<string, List<Type>> _categorizedTypes = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Component> _tempComponents = new List<Component>();

        public static bool IsFilterActive => _filterActive && _selectedType != null;
        public static Type SelectedType => _selectedType;

        /// <summary>
        /// Clears the active hierarchy filter.
        /// </summary>
        public static void ClearFilter()
        {
            _selectedType = null;
            _filterActive = false;
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// Scans all active GameObjects in the scene to build a list of unique component types.
        /// </summary>
        private static void ScanSceneComponents()
        {
            _uniqueTypes.Clear();
            _categorizedTypes.Clear();

            // Find all active GameObjects in the current scene context
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (GameObject go in allObjects)
            {
                if (go == null) continue;

                _tempComponents.Clear();
                go.GetComponents<Component>(_tempComponents);

                foreach (Component comp in _tempComponents)
                {
                    if (comp == null) continue;
                    
                    Type type = comp.GetType();
                    if (type == typeof(Transform)) continue; // Ignore transform as every GameObject has it

                    if (!_uniqueTypes.Contains(type))
                    {
                        _uniqueTypes.Add(type);
                    }
                }
            }

            // Categorize the unique types
            foreach (Type type in _uniqueTypes)
            {
                string category = GetTypeCategory(type);
                if (!_categorizedTypes.ContainsKey(category))
                {
                    _categorizedTypes[category] = new List<Type>();
                }
                _categorizedTypes[category].Add(type);
            }

            // Sort types inside each category
            foreach (var kvp in _categorizedTypes)
            {
                kvp.Value.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Groups types into categories based on namespace.
        /// </summary>
        private static string GetTypeCategory(Type type)
        {
            string ns = type.Namespace;
            if (string.IsNullOrEmpty(ns))
            {
                return "Scripts (Default Namespace)";
            }

            if (ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor"))
            {
                return "Unity Native";
            }

            if (ns.StartsWith("nadena.plotg.modular_avatar"))
            {
                return "Modular Avatar";
            }

            if (ns.StartsWith("VRC"))
            {
                return "VRChat SDK";
            }

            // Group by top-level namespace name for clean structure
            int firstDot = ns.IndexOf('.');
            if (firstDot != -1)
            {
                return ns.Substring(0, firstDot);
            }

            return ns;
        }

        /// <summary>
        /// Evaluates if a GameObject should be visually dimmed out in the hierarchy.
        /// </summary>
        public static bool ShouldDim(GameObject obj)
        {
            if (!IsFilterActive) return false;
            if (obj == null) return false;

            // Categories/Folders themselves shouldn't be dimmed if they contain any matching children,
            // or we can keep them visible for structure representation.
            bool isCategory = obj.name.StartsWith("---") && obj.name.EndsWith("---");
            if (isCategory)
            {
                // Check if any descendant of this folder contains the target component
                return !HasDescendantWithComponent(obj, _selectedType);
            }

            // Check if this object contains the selected component
            return obj.GetComponent(_selectedType) == null;
        }

        private static bool HasDescendantWithComponent(GameObject folder, Type targetType)
        {
            Transform[] children = folder.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.gameObject == folder) continue;
                if (child.GetComponent(targetType) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Renders the Hierarchy Filters user interface.
        /// </summary>
        public static void DrawFilterGUI()
        {
            bool isPtBr = HierarchySentinel.IsPortuguese();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(6);

            GUILayout.Label(isPtBr ? "Filtros Rápidos Dinâmicos" : "Dynamic Quick Filters", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                isPtBr 
                    ? "Selecione um componente para esmaecer visualmente todos os objetos da hierarquia que não o possuem."
                    : "Select a component to visually dim all hierarchy objects that do not contain it.", 
                EditorStyles.wordWrappedMiniLabel
            );

            EditorGUILayout.Space(8);

            // Filter status box
            EditorGUILayout.BeginHorizontal();

            string dropdownLabel = _selectedType != null 
                ? $"{GetTypeCategory(_selectedType)} / {_selectedType.Name}"
                : (isPtBr ? "Selecionar Componente..." : "Select Component...");

            if (GUILayout.Button(dropdownLabel, EditorStyles.popup, GUILayout.Height(20)))
            {
                ScanSceneComponents();
                ShowFilterDropdownMenu();
            }

            if (_selectedType != null)
            {
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    ClearFilter();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Toggle activation
            if (_selectedType != null)
            {
                EditorGUI.BeginChangeCheck();
                _filterActive = EditorGUILayout.Toggle(isPtBr ? "Filtro Ativo" : "Filter Active", _filterActive);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorApplication.RepaintHierarchyWindow();
                }

                if (_filterActive)
                {
                    EditorGUILayout.HelpBox(
                        isPtBr
                            ? "Filtro Ativo! Objetos sem este componente estão com 15% de opacidade na Hierarchy."
                            : "Filter Active! Objects without this component are drawn at 15% opacity in the Hierarchy.",
                        MessageType.Info
                    );
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    isPtBr 
                        ? "Nenhum componente selecionado. Clique acima para buscar e selecionar."
                        : "No component selected. Click above to search and select one.",
                    MessageType.None
                );
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        private static void ShowFilterDropdownMenu()
        {
            GenericMenu menu = new GenericMenu();
            bool isPtBr = HierarchySentinel.IsPortuguese();

            if (_categorizedTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent(isPtBr ? "Nenhum componente na cena" : "No components found in scene"));
                menu.ShowAsContext();
                return;
            }

            foreach (var category in _categorizedTypes)
            {
                string categoryName = category.Key;
                foreach (Type type in category.Value)
                {
                    // Build path e.g. "Unity Native/Camera"
                    string path = $"{categoryName}/{type.Name}";
                    menu.AddItem(new GUIContent(path), _selectedType == type, () =>
                    {
                        _selectedType = type;
                        _filterActive = true;
                        EditorApplication.RepaintHierarchyWindow();
                    });
                }
            }

            menu.ShowAsContext();
        }
    }
}
