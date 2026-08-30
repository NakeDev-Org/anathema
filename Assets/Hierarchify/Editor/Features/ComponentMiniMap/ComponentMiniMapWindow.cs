using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// A premium floating Mini-Map Inspector window that allows users to view and edit
    /// properties of a specific component without losing their current selection or Inspector focus.
    /// </summary>
    public class ComponentMiniMapWindow : EditorWindow
    {
        private static readonly Dictionary<Component, ComponentMiniMapWindow> _openWindows = new Dictionary<Component, ComponentMiniMapWindow>();

        public static bool IsWindowOpen(Component component)
        {
            if (component == null) return false;
            return _openWindows.TryGetValue(component, out var window) && window != null;
        }

        public static bool HasAnyOpenWindowForGameObject(GameObject go, out List<Component> openComponents)
        {
            openComponents = new List<Component>();
            if (go == null) return false;
            foreach (var kvp in _openWindows)
            {
                if (kvp.Key != null && kvp.Key.gameObject == go && kvp.Value != null)
                {
                    openComponents.Add(kvp.Key);
                }
            }
            return openComponents.Count > 0;
        }

        private Component targetComponent;
        private UnityEditor.Editor componentEditor;
        private Vector2 scrollPos;

        /// <summary>
        /// Displays the floating inspector for the specified component.
        /// If a window is already open for this component, it will be focused.
        /// </summary>
        public static void ShowWindow(Component component, Vector2 mouseScreenPos)
        {
            if (component == null) return;

            // Clean up missing/null entries from open windows dictionary defensively
            var keysToRemove = new List<Component>();
            foreach (var kvp in _openWindows)
            {
                if (kvp.Key == null || kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _openWindows.Remove(key);
            }

            // Focus if already open to avoid redundant windows
            if (_openWindows.TryGetValue(component, out var existingWindow) && existingWindow != null)
            {
                existingWindow.Focus();
                return;
            }

            ComponentMiniMapWindow window = CreateInstance<ComponentMiniMapWindow>();
            window.targetComponent = component;
            
            // Register window instance
            _openWindows[component] = window;

            // Set window title content matching component type icon
            Texture icon = EditorGUIUtility.ObjectContent(component, component.GetType()).image;
            window.titleContent = new GUIContent($"{component.GetType().Name}", icon);

            // Position the window next to the mouse cursor
            float width = 360f;
            float height = 450f;
            window.position = new Rect(mouseScreenPos.x + 20f, mouseScreenPos.y - 150f, width, height);
            
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (targetComponent == null)
            {
                if (componentEditor != null)
                {
                    DestroyImmediate(componentEditor);
                    componentEditor = null;
                }
                
                Component keyToRemove = null;
                foreach (var kvp in _openWindows)
                {
                    if (kvp.Value == this)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }
                if (keyToRemove != null)
                {
                    _openWindows.Remove(keyToRemove);
                }

                Close();
                return;
            }

            // Lazily create component editor in OnGUI to ensure valid serialization context
            if (componentEditor == null)
            {
                try
                {
                    // Defensively test if a SerializedObject can be successfully created for the component
                    // to prevent internal exceptions in custom editors (like VolumeEditor) that access
                    // serializedObject in OnEnable and cause Unity to log exceptions natively.
                    using (var tempSO = new SerializedObject(targetComponent))
                    {
                        // SerializedObject is successfully creatable
                    }
                    componentEditor = UnityEditor.Editor.CreateEditor(targetComponent);
                }
                catch (System.Exception)
                {
                    // Safe fallback if the component editor cannot be initialized (e.g. Volume components in certain pipelines)
                    componentEditor = null;
                }
            }

            // Header styling
            Color headerBg = EditorGUIUtility.isProSkin 
                ? new Color(0.18f, 0.18f, 0.18f, 1.0f) 
                : new Color(0.76f, 0.76f, 0.76f, 1.0f);

            Color borderBg = EditorGUIUtility.isProSkin 
                ? new Color(0.12f, 0.12f, 0.12f, 1.0f) 
                : new Color(0.6f, 0.6f, 0.6f, 1.0f);

            // Draw header background
            Rect headerRect = new Rect(0, 0, position.width, 46f);
            EditorGUI.DrawRect(headerRect, headerBg);
            
            // Draw header separation border
            Rect borderRect = new Rect(0, 45f, position.width, 1f);
            EditorGUI.DrawRect(borderRect, borderBg);

            // Draw header content
            GUILayout.BeginArea(new Rect(10f, 6f, position.width - 20f, 35f));
            GUILayout.BeginHorizontal();
            
            // Component Icon
            Texture icon = EditorGUIUtility.ObjectContent(targetComponent, targetComponent.GetType()).image;
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(24f), GUILayout.Height(24f));
            }

            GUILayout.BeginVertical();
            // GameObject Name
            GUILayout.Label(targetComponent.gameObject.name, EditorStyles.boldLabel);
            // Component Type Name
            GUILayout.Label(targetComponent.GetType().Name, EditorStyles.miniLabel);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Active / Enabled Toggle inside the header
            System.Reflection.PropertyInfo enabledProp = null;
            bool isToggleable = IsComponentToggleable(targetComponent, out enabledProp);
            if (isToggleable && enabledProp != null)
            {
                bool isEnabled = (bool)enabledProp.GetValue(targetComponent);
                EditorGUI.BeginChangeCheck();
                isEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20f));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetComponent, "Toggle Component");
                    enabledProp.SetValue(targetComponent, isEnabled);
                    EditorUtility.SetDirty(targetComponent);
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Scrollable inspector body
            GUILayout.Space(52f);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            
            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 5, 10) });

            if (componentEditor != null)
            {
                EditorGUI.BeginChangeCheck();
                componentEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(targetComponent);
                }
            }
            else
            {
                GUILayout.Label("No inspector available.", EditorStyles.miniLabel);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            if (componentEditor != null)
            {
                DestroyImmediate(componentEditor);
            }

            if (targetComponent != null)
            {
                _openWindows.Remove(targetComponent);
            }
            EditorApplication.RepaintHierarchyWindow();
        }

        private static bool IsComponentToggleable(Component comp, out System.Reflection.PropertyInfo enabledProp)
        {
            enabledProp = null;
            if (comp == null) return false;

            if (comp is Behaviour)
            {
                enabledProp = typeof(Behaviour).GetProperty("enabled");
                return true;
            }
            if (comp is Collider)
            {
                enabledProp = typeof(Collider).GetProperty("enabled");
                return true;
            }
            if (comp is Renderer)
            {
                enabledProp = typeof(Renderer).GetProperty("enabled");
                return true;
            }

            System.Type type = comp.GetType();
            enabledProp = type.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return enabledProp != null && enabledProp.PropertyType == typeof(bool) && enabledProp.CanWrite;
        }
    }
}
