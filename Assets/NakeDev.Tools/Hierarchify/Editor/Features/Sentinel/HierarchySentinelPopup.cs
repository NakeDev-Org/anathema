using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Custom interactive popup GUI for the Hierarchy Sentinel.
    /// Lists diagnostics with severe warning indicators and connects back to the Smart Console.
    /// </summary>
    public class HierarchySentinelPopup : PopupWindowContent
    {
        private readonly GameObject _targetObject;
        private readonly List<string> _errors;
        private Vector2 _scrollPos;

        public HierarchySentinelPopup(GameObject target)
        {
            _targetObject = target;
            _errors = HierarchySentinel.GetErrors(target);
            if (_errors.Count == 0)
            {
                _errors = HierarchySentinel.GetDescendantErrorsFormatted(target);
            }
        }

        public override Vector2 GetWindowSize()
        {
            float width = 420f;
            float height = Mathf.Min(350f, 60f + (_errors.Count * 28f) + 40f);
            return new Vector2(width, height);
        }

        public override void OnGUI(Rect rect)
        {
            // Draw clean dark background
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));

            GUILayout.BeginArea(new Rect(10, 10, rect.width - 20, rect.height - 20));

            // Determine context
            bool isDescendants = HierarchySentinel.GetErrors(_targetObject).Count == 0;
            bool isPtBr = HierarchySentinel.IsPortuguese();
            
            bool hasRuntimeErrors = false;
            if (isDescendants)
            {
                HierarchySentinel.HasErrorsInDescendants(_targetObject, out hasRuntimeErrors);
            }
            else
            {
                hasRuntimeErrors = HierarchySentinel.HasRuntimeErrors(_targetObject);
            }

            // --- HEADER ---
            GUILayout.BeginHorizontal();
            Texture2D alertIcon = hasRuntimeErrors 
                ? (EditorGUIUtility.IconContent("console.erroricon").image as Texture2D)
                : (EditorGUIUtility.IconContent("console.warnicon").image as Texture2D);

            if (alertIcon != null)
            {
                GUILayout.Label(alertIcon, GUILayout.Width(24), GUILayout.Height(24));
            }

            GUILayout.BeginVertical();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            string titleText = "";
            if (isDescendants)
            {
                titleText = isPtBr 
                    ? $"{_errors.Count} Problemas nos Filhos" 
                    : $"{_errors.Count} Child Issues Detected";
            }
            else
            {
                titleText = isPtBr 
                    ? $"{_errors.Count} Problemas Detectados" 
                    : $"{_errors.Count} Issues Detected";
            }
            GUILayout.Label(titleText, titleStyle);
            
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            GUILayout.Label($"GameObject: {_targetObject.name}", subtitleStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Splitter line
            Rect splitter = GUILayoutUtility.GetRect(rect.width - 20, 1);
            EditorGUI.DrawRect(splitter, new Color(0.3f, 0.3f, 0.3f, 1f));

            GUILayout.Space(8);

            // --- DIAGNOSTICS LIST ---
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            GUIStyle errorLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            for (int i = 0; i < _errors.Count; i++)
            {
                string err = _errors[i];
                bool isRuntime = err.StartsWith("[Runtime]");

                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                // Icon for row based on severity
                Texture2D rowIcon = isRuntime
                    ? (EditorGUIUtility.IconContent("console.erroricon.sml").image as Texture2D)
                    : (EditorGUIUtility.IconContent("console.warnicon.sml").image as Texture2D);

                if (rowIcon != null)
                {
                    GUILayout.Label(rowIcon, GUILayout.Width(16), GUILayout.Height(16));
                }

                // Render error text
                string displayMsg = isRuntime ? err.Replace("[Runtime] ", "") : err;
                GUILayout.Label($"{i + 1}) {displayMsg}", errorLabelStyle, GUILayout.ExpandWidth(true));

                // Actions for runtime errors
                if (isRuntime)
                {
                    GUIStyle inspectBtnStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.18f, 0.49f, 0.96f) }
                    };
                    if (GUILayout.Button(isPtBr ? "Inspecionar 🔍" : "Inspect 🔍", inspectBtnStyle, GUILayout.Width(80), GUILayout.Height(18)))
                    {
                        FocusInSmartConsole(err);
                        editorWindow.Close();
                    }
                }
                
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            // --- FOOTER BUTTON ---
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isPtBr ? "Fechar" : "Dismiss", GUILayout.Width(80), GUILayout.Height(22)))
            {
                editorWindow.Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Safely opens the Smart Console window, searches the log, selects it, and highlights the entry.
        /// </summary>
        private void FocusInSmartConsole(string errorText)
        {
            System.Type windowType = System.Type.GetType("Nakemo.SmartConsole.SmartConsoleWindow, Nakemo.SmartConsole.Editor");
            if (windowType == null)
            {
                windowType = System.Type.GetType("Nakemo.SmartConsole.SmartConsoleWindow, Assembly-CSharp-Editor");
            }
            if (windowType == null) return;

            // Open the Smart Console window
            var openMethod = windowType.GetMethod("Open", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (openMethod != null)
            {
                openMethod.Invoke(null, null);
            }

            var windowInstance = EditorWindow.GetWindow(windowType);
            if (windowInstance != null)
            {
                System.Type coreType = System.Type.GetType("Nakemo.SmartConsole.SmartConsoleCore, Nakemo.SmartConsole.Editor");
                if (coreType == null)
                {
                    coreType = System.Type.GetType("Nakemo.SmartConsole.SmartConsoleCore, Assembly-CSharp-Editor");
                }
                if (coreType != null)
                {
                    var allLogsProp = coreType.GetProperty("AllLogs", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (allLogsProp != null)
                    {
                        var allLogs = allLogsProp.GetValue(null) as System.Collections.IEnumerable;
                        if (allLogs != null)
                        {
                            object foundEntry = null;
                            string cleanErr = errorText.Replace("[Runtime] ", "").Trim();

                            foreach (var item in allLogs)
                            {
                                var rawProp = item.GetType().GetProperty("RawMessage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if (rawProp == null) continue;

                                string rawMsg = rawProp.GetValue(item) as string ?? "";
                                if (rawMsg.Contains(cleanErr))
                                {
                                    foundEntry = item;
                                    break;
                                }
                            }

                            if (foundEntry != null)
                            {
                                var selectedProp = windowType.GetField("_selectedEntry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (selectedProp != null)
                                {
                                    selectedProp.SetValue(windowInstance, foundEntry);
                                    windowInstance.Repaint();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
