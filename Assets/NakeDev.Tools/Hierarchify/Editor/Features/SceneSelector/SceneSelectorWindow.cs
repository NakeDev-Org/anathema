using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// A sleek, borderless, floating Scene Selector window that supports quick search,
    /// instant load on click, and drag-and-drop favorite reordering.
    /// </summary>
    public class SceneSelectorWindow : EditorWindow
    {
        private string _searchFilter = "";
        private readonly List<string> _cachedScenePaths = new List<string>();
        private Vector2 _scrollPos;

        // Drag and Drop state
        private int _draggedIndex = -1;
        private bool _isDragging = false;
        private readonly List<Rect> _pinnedRowRects = new List<Rect>();
        private bool _needsFocus = true;
        private string _clickedScenePath = null;

        /// <summary>
        /// Opens the selector positioned directly under the hierarchy dropdown arrow.
        /// </summary>
        public static void ShowWindow(Rect buttonRect)
        {
            SceneSelectorWindow window = CreateInstance<SceneSelectorWindow>();
            
            float width = 240f;
            float height = 320f;
            
            // Convert button rect to screen space coordinates and show dropdown
            Rect screenRect = GUIUtility.GUIToScreenRect(buttonRect);
            window.ShowAsDropDown(screenRect, new Vector2(width, height));
        }

        private void OnGUI()
        {
            if (_cachedScenePaths.Count == 0)
            {
                RefreshSceneCache();
            }

            Event e = Event.current;
            bool isPtBr = HierarchySentinel.IsPortuguese();
            bool canReorder = string.IsNullOrEmpty(_searchFilter);

            // Clear row rects list for recalculation this frame
            _pinnedRowRects.Clear();

            // Draw clean background & 1px border
            Rect borderRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(borderRect, EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.55f, 0.55f, 0.55f, 1f));
            
            Rect bgRect = new Rect(1, 1, position.width - 2, position.height - 2);
            EditorGUI.DrawRect(bgRect, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.88f, 0.88f, 0.88f, 1f));

            GUILayout.BeginVertical();
            GUILayout.Space(8);

            // Modern, simple search bar layout
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            
            Rect searchBoxRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            
            // Draw background & 1px border
            EditorGUI.DrawRect(searchBoxRect, EditorGUIUtility.isProSkin ? new Color(0.14f, 0.14f, 0.14f, 1f) : new Color(0.96f, 0.96f, 0.96f, 1f));
            Color searchBorderColor = EditorGUIUtility.isProSkin ? new Color(0.24f, 0.24f, 0.24f, 1f) : new Color(0.75f, 0.75f, 0.75f, 1f);
            EditorGUI.DrawRect(new Rect(searchBoxRect.x, searchBoxRect.y, searchBoxRect.width, 1), searchBorderColor);
            EditorGUI.DrawRect(new Rect(searchBoxRect.x, searchBoxRect.yMax - 1, searchBoxRect.width, 1), searchBorderColor);
            EditorGUI.DrawRect(new Rect(searchBoxRect.x, searchBoxRect.y, 1, searchBoxRect.height), searchBorderColor);
            EditorGUI.DrawRect(new Rect(searchBoxRect.xMax - 1, searchBoxRect.y, 1, searchBoxRect.height), searchBorderColor);
            
            // Search magnifying glass icon
            Texture2D searchIcon = EditorGUIUtility.IconContent("Search Icon").image as Texture2D;
            if (searchIcon != null)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                GUI.DrawTexture(new Rect(searchBoxRect.x + 6, searchBoxRect.y + 4, 16, 16), searchIcon);
                GUI.color = Color.white;
            }

            // Sleek borderless text input
            GUIStyle searchStyle = new GUIStyle(EditorStyles.textField);
            searchStyle.normal.background = null;
            searchStyle.focused.background = null;
            searchStyle.hover.background = null;
            searchStyle.active.background = null;
            searchStyle.border = new RectOffset(0, 0, 0, 0);
            searchStyle.fontSize = 11;

            GUI.SetNextControlName("ModernSearchField");
            _searchFilter = EditorGUI.TextField(new Rect(searchBoxRect.x + 26, searchBoxRect.y + 4, searchBoxRect.width - 44, 16), _searchFilter, searchStyle);

            if (string.IsNullOrEmpty(_searchFilter))
            {
                string placeholderText = isPtBr ? "Buscar cenas..." : "Search scenes...";
                GUIStyle placeholderStyle = new GUIStyle(EditorStyles.label);
                placeholderStyle.fontSize = 11;
                placeholderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                
                if (GUI.GetNameOfFocusedControl() != "ModernSearchField")
                {
                    GUI.Label(new Rect(searchBoxRect.x + 26, searchBoxRect.y + 4, searchBoxRect.width - 44, 16), placeholderText, placeholderStyle);
                }
            }

            if (_needsFocus && e.type == EventType.Repaint)
            {
                GUI.FocusControl("ModernSearchField");
                _needsFocus = false;
            }

            // Clear search button
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                Rect cancelRect = new Rect(searchBoxRect.xMax - 20, searchBoxRect.y + 4, 16, 16);
                if (GUI.Button(cancelRect, "", GUIStyle.none))
                {
                    _searchFilter = "";
                    GUI.FocusControl(null);
                }
                Texture2D cancelIcon = EditorGUIUtility.IconContent("d_winbtn_win_close_h").image as Texture2D;
                if (cancelIcon != null)
                {
                    GUI.DrawTexture(cancelRect, cancelIcon);
                }
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Subtle separator line below header
            Rect lineRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, EditorGUIUtility.isProSkin ? new Color(0.14f, 0.14f, 0.14f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f));

            // Scroll View for Scene Items
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            List<string> pinnedScenes = HierarchySettings.GetPinnedScenes();
            List<string> filteredPinned = new List<string>();
            List<string> filteredUnpinned = new List<string>();

            string searchLower = _searchFilter.ToLower();

            foreach (string path in _cachedScenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(searchLower) && !sceneName.ToLower().Contains(searchLower))
                {
                    continue;
                }

                if (pinnedScenes.Contains(path))
                {
                    filteredPinned.Add(path);
                }
                else
                {
                    filteredUnpinned.Add(path);
                }
            }

            // Preserve saved order
            filteredPinned.Sort((a, b) => pinnedScenes.IndexOf(a).CompareTo(pinnedScenes.IndexOf(b)));

            // Render Pinned List
            if (filteredPinned.Count > 0)
            {
                for (int i = 0; i < filteredPinned.Count; i++)
                {
                    DrawSceneItem(filteredPinned[i], true, i);
                }

                if (filteredUnpinned.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    Rect sepRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(sepRect, EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f));
                    EditorGUILayout.Space(4);
                }
            }

            // Render Unpinned List
            for (int i = 0; i < filteredUnpinned.Count; i++)
            {
                DrawSceneItem(filteredUnpinned[i], false, i);
            }

            if (filteredPinned.Count == 0 && filteredUnpinned.Count == 0)
            {
                GUILayout.Space(12);
                GUILayout.Label(isPtBr ? "Nenhuma cena." : "No scenes found.", EditorStyles.centeredGreyMiniLabel);
            }

            // Handle Drag mouse events within the scroll view context (fixes coordinate offset)
            if (_draggedIndex != -1 && canReorder)
            {
                if (e.type == EventType.MouseDrag)
                {
                    _isDragging = true;
                    List<string> pinned = HierarchySettings.GetPinnedScenes();
                    if (_draggedIndex >= 0 && _draggedIndex < pinned.Count && _pinnedRowRects.Count == pinned.Count)
                    {
                        for (int j = 0; j < _pinnedRowRects.Count; j++)
                        {
                            if (j != _draggedIndex && _pinnedRowRects[j].Contains(e.mousePosition))
                            {
                                string item = pinned[_draggedIndex];
                                pinned.RemoveAt(_draggedIndex);
                                pinned.Insert(j, item);

                                HierarchySettings.SetPinnedScenes(pinned);
                                _draggedIndex = j;
                                break;
                            }
                        }
                    }
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
                {
                    _isDragging = false;
                    _draggedIndex = -1;
                    _clickedScenePath = null;
                    e.Use();
                    Repaint();
                }
            }
            else
            {
                _isDragging = false;
                _draggedIndex = -1;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Clear clicked state globally if mouse is released outside of the scroll view items
            if (e.type == EventType.MouseUp)
            {
                _clickedScenePath = null;
            }
        }

        private void DrawSceneItem(string path, bool isPinned, int index)
        {
            string sceneName = Path.GetFileNameWithoutExtension(path);
            string activeScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            bool isActive = (activeScenePath == path);
            bool canReorder = string.IsNullOrEmpty(_searchFilter);

            // Reserve space for the entire row using GUILayoutUtility.GetRect (guarantees accurate coords in all EventTypes)
            Rect rowRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            
            if (isPinned && canReorder)
            {
                _pinnedRowRects.Add(rowRect);
            }

            Event e = Event.current;
            bool isHovered = rowRect.Contains(e.mousePosition);

            // Compute sub-element dimensions relative to rowRect
            float gripWidth = 20f;
            float starWidth = 28f;
            float iconSize = 16f;

            Rect gripRect = new Rect(rowRect.x, rowRect.y, gripWidth, rowRect.height);
            Rect starRect = new Rect(rowRect.xMax - starWidth, rowRect.y, starWidth, rowRect.height);
            bool isStarHovered = starRect.Contains(e.mousePosition);

            float iconX = rowRect.x + (isPinned ? gripWidth : 8f);
            Rect iconRect = new Rect(iconX, rowRect.y + (rowRect.height - iconSize) / 2f, iconSize, iconSize);

            float textX = iconRect.xMax + 6f;
            float textWidth = starRect.x - textX - 4f;
            Rect textRect = new Rect(textX, rowRect.y, textWidth, rowRect.height);

            // Draw Background selection & hover highlights (Repaint only)
            if (e.type == EventType.Repaint)
            {
                Color bgColor = Color.clear;
                if (_isDragging && _draggedIndex == index && isPinned && canReorder)
                {
                    bgColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.12f, 0.58f, 0.95f, 0.22f) 
                        : new Color(0.12f, 0.58f, 0.95f, 0.16f);
                }
                else if (isActive)
                {
                    bgColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.12f, 0.58f, 0.95f, 0.12f) 
                        : new Color(0.12f, 0.58f, 0.95f, 0.08f);
                }
                else if (isHovered)
                {
                    bgColor = EditorGUIUtility.isProSkin 
                        ? new Color(1f, 1f, 1f, 0.04f) 
                        : new Color(0f, 0f, 0f, 0.04f);
                }

                if (bgColor != Color.clear)
                {
                    EditorGUI.DrawRect(rowRect, bgColor);
                }

                // Left active indicator
                if (isActive)
                {
                    Rect activeIndicator = new Rect(rowRect.x, rowRect.y + 4, 3, rowRect.height - 8);
                    EditorGUI.DrawRect(activeIndicator, new Color(0.12f, 0.58f, 0.95f, 1f));
                }

                // 1. Draw Grip Handle (pinned items only)
                if (isPinned && canReorder)
                {
                    Color gripColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.35f) : new Color(0.3f, 0.3f, 0.3f, 0.35f);
                    
                    float dotX1 = gripRect.x + 6;
                    float dotX2 = gripRect.x + 10;
                    float dotYStart = gripRect.y + (gripRect.height - 12) / 2f;

                    EditorGUI.DrawRect(new Rect(dotX1, dotYStart, 2, 2), gripColor);
                    EditorGUI.DrawRect(new Rect(dotX1, dotYStart + 5, 2, 2), gripColor);
                    EditorGUI.DrawRect(new Rect(dotX1, dotYStart + 10, 2, 2), gripColor);
                    EditorGUI.DrawRect(new Rect(dotX2, dotYStart, 2, 2), gripColor);
                    EditorGUI.DrawRect(new Rect(dotX2, dotYStart + 5, 2, 2), gripColor);
                    EditorGUI.DrawRect(new Rect(dotX2, dotYStart + 10, 2, 2), gripColor);
                }

                // 2. Draw Scene Icon
                var sceneIconContent = EditorGUIUtility.IconContent("SceneAsset Icon");
                Texture2D sceneIcon = sceneIconContent != null ? sceneIconContent.image as Texture2D : null;
                if (sceneIcon != null)
                {
                    GUI.DrawTexture(iconRect, sceneIcon);
                }

                // 3. Draw Scene Name
                GUIStyle textStyle = new GUIStyle(isActive ? EditorStyles.boldLabel : EditorStyles.label);
                textStyle.fontSize = 11;
                
                float textHeight = textStyle.CalcHeight(new GUIContent(sceneName), textWidth);
                Rect textDrawRect = new Rect(textRect.x, textRect.y + (textRect.height - textHeight) / 2f, textRect.width, textHeight);

                if (isActive)
                {
                    textStyle.normal.textColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.35f, 0.75f, 1f, 1f) 
                        : new Color(0.1f, 0.4f, 0.8f, 1f);
                }
                else
                {
                    textStyle.normal.textColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.85f, 0.85f, 0.85f, 1f) 
                        : new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                
                GUI.Label(textDrawRect, new GUIContent(sceneName, path), textStyle);

                // 4. Draw Pin Button
                if (isPinned || isHovered)
                {
                    Texture2D starIcon = isPinned 
                        ? EditorGUIUtility.IconContent("Favorite Icon").image as Texture2D 
                        : EditorGUIUtility.IconContent("d_Favorite Icon").image as Texture2D;

                    if (starIcon != null)
                    {
                        Color starColor = isPinned 
                            ? new Color(1f, 0.8f, 0f, 1f) 
                            : (isStarHovered ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.7f, 0.7f, 0.7f, 0.4f));

                        Rect drawRect = new Rect(
                            starRect.x + (starRect.width - 14) / 2f,
                            starRect.y + (starRect.height - 14) / 2f,
                            14, 14
                        );

                        GUI.color = starColor;
                        GUI.DrawTexture(drawRect, starIcon);
                        GUI.color = Color.white;
                    }
                }
            }

            // Mouse Events handling
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (isStarHovered)
                {
                    List<string> pinnedList = HierarchySettings.GetPinnedScenes();
                    if (isPinned)
                    {
                        pinnedList.Remove(path);
                    }
                    else
                    {
                        pinnedList.Add(path);
                    }
                    HierarchySettings.SetPinnedScenes(pinnedList);
                    e.Use();
                    Repaint();
                }
                else if (isHovered)
                {
                    _clickedScenePath = path;
                    if (isPinned && canReorder)
                    {
                        _draggedIndex = index;
                    }
                    else
                    {
                        _draggedIndex = -1;
                    }
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (_clickedScenePath == path && isHovered && !_isDragging)
                {
                    _clickedScenePath = null;
                    e.Use();
                    
                    bool additive = e.control || e.shift;
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);

                    if (scene.isLoaded)
                    {
                        if (additive)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                        }
                        else if (!isActive)
                        {
                            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                        }
                    }
                    else
                    {
                        if (additive)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                        }
                        else
                        {
                            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                            }
                        }
                    }
                    Close();
                }
            }

            // Set cursor states dynamically
            if (isHovered && !_isDragging)
            {
                if (starRect.Contains(e.mousePosition))
                {
                    EditorGUIUtility.AddCursorRect(starRect, MouseCursor.Link);
                }
                else if (isPinned && canReorder)
                {
                    if (gripRect.Contains(e.mousePosition))
                    {
                        EditorGUIUtility.AddCursorRect(gripRect, MouseCursor.Pan);
                    }
                    else
                    {
                        Rect restOfRow = new Rect(gripRect.xMax, rowRect.y, starRect.x - gripRect.xMax, rowRect.height);
                        EditorGUIUtility.AddCursorRect(restOfRow, MouseCursor.Link);
                    }
                }
                else
                {
                    EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);
                }
            }
        }

        private void RefreshSceneCache()
        {
            _cachedScenePaths.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/"))
                {
                    _cachedScenePaths.Add(path);
                }
            }
        }
    }
}
