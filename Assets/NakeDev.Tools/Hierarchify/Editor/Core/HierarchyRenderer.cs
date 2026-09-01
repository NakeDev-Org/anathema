using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Core renderer system for the Hierarchify tool.
    /// Intercepts the Unity Hierarchy draw loop to render custom background bars, category borders,
    /// dynamic folder icons, tree relation lines, and custom popups.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyRenderer
    {
        private struct FolderCacheItem
        {
            public string rawName;
            public string cleanName;
            public Color accentColor;
        }

        private static readonly Dictionary<int, FolderCacheItem> _folderRenderCache = new Dictionary<int, FolderCacheItem>();
        private static readonly Dictionary<int, int> _lastRootCache = new Dictionary<int, int>();
        private static double _lastDeleteTime = 0f;
        private static GameObject _requestedColorPickerFolder;
        private static int _lastHoveredInstanceID = 0;
        private static bool _lastCtrlState = false;

        private static readonly System.Collections.Generic.List<Component> _tempComponentsList = new System.Collections.Generic.List<Component>();
        private static readonly System.Collections.Generic.List<Component> _validComponentsList = new System.Collections.Generic.List<Component>();

        /// <summary>
        /// Requests opening the color picker popup for the specified folder on the next render pass.
        /// </summary>
        public static void RequestColorPicker(GameObject folder)
        {
            _requestedColorPickerFolder = folder;
            EditorApplication.RepaintHierarchyWindow();
        }

        // Reflection cache for hierarchy item expansion detection
        private static System.Type _hierarchyWindowType;
        private static System.Reflection.MethodInfo _getExpandedIDsMethod;
        private static bool _reflectionInitialized = false;

        private static System.Reflection.FieldInfo _entityIdField;
        private static bool _entityIdFieldInitialized = false;

        static HierarchyRenderer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawCustomHierarchyItem;
            EditorApplication.hierarchyChanged += ClearRootCache;
        }

        private static void ClearRootCache()
        {
            _lastRootCache.Clear();
        }

        /// <summary>
        /// Initializes the internal Unity Editor types and methods via reflection once.
        /// </summary>
        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            try
            {
                _hierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                if (_hierarchyWindowType != null)
                {
                    _getExpandedIDsMethod = _hierarchyWindowType.GetMethod("GetExpandedIDs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }
                _reflectionInitialized = true;
            }
            catch
            {
                _reflectionInitialized = true;
            }
        }

        /// <summary>
        /// Safely extracts the integer identifier from the Unity 6+ EntityId struct via cached reflection.
        /// Falls back to string parsing if the struct layout is altered.
        /// </summary>
        private static int GetIdFromEntityId(object entityId)
        {
            if (entityId == null) return 0;

            if (!_entityIdFieldInitialized)
            {
                var fields = entityId.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fields.Length > 0)
                {
                    _entityIdField = fields[0];
                }
                _entityIdFieldInitialized = true;
            }

            if (_entityIdField != null)
            {
                var val = _entityIdField.GetValue(entityId);
                if (val is int intVal) return intVal;
                if (val is uint uintVal) return (int)uintVal;
                if (val is long longVal) return (int)longVal;
                if (val is ulong ulongVal) return (int)ulongVal;
            }

            if (int.TryParse(entityId.ToString(), out int parsed))
            {
                return parsed;
            }
            return 0;
        }

        private static Object[] _cachedHierarchyWindows;
        private static readonly HashSet<int> _cachedExpandedIDs = new HashSet<int>();
        private static double _lastExpandedCacheTime = -1.0;

        private static GUIContent _tempSceneContent;
        private static GUIContent _tempTooltipContent;

        private static void InitializeSharedContent()
        {
            if (_tempSceneContent == null)
            {
                _tempSceneContent = new GUIContent();
            }
            if (_tempTooltipContent == null)
            {
                _tempTooltipContent = new GUIContent();
            }
        }

        /// <summary>
        /// Detects whether a specific GameObject is currently expanded (folded out) in the hierarchy tree.
        /// </summary>
        private static bool IsHierarchyItemExpanded(int instanceID)
        {
            InitializeReflection();
            if (_hierarchyWindowType == null || _getExpandedIDsMethod == null)
                return false;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime != _lastExpandedCacheTime)
            {
                _lastExpandedCacheTime = currentTime;
                _cachedExpandedIDs.Clear();
                try
                {
                    if (_cachedHierarchyWindows == null || _cachedHierarchyWindows.Length == 0 || _cachedHierarchyWindows[0] == null)
                    {
                        _cachedHierarchyWindows = Resources.FindObjectsOfTypeAll(_hierarchyWindowType);
                    }

                    if (_cachedHierarchyWindows != null && _cachedHierarchyWindows.Length > 0)
                    {
                        for (int w = 0; w < _cachedHierarchyWindows.Length; w++)
                        {
                            var window = _cachedHierarchyWindows[w];
                            if (window == null) 
                            {
                                _cachedHierarchyWindows = null;
                                break;
                            }

                            var expandedIDsObject = _getExpandedIDsMethod.Invoke(window, null);
                            if (expandedIDsObject != null && expandedIDsObject is System.Collections.IEnumerable enumerable)
                            {
                                foreach (var item in enumerable)
                                {
                                    if (item is int id)
                                    {
                                        _cachedExpandedIDs.Add(id);
                                    }
                                    else if (item != null)
                                    {
                                        int extractedId = GetIdFromEntityId(item);
                                        _cachedExpandedIDs.Add(extractedId);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    _cachedHierarchyWindows = null;
                }
            }

            return _cachedExpandedIDs.Contains(instanceID);
        }

        private static void ToggleHierarchyItemExpanded(int instanceID, bool expand)
        {
            try
            {
                if (_cachedHierarchyWindows == null || _cachedHierarchyWindows.Length == 0 || _cachedHierarchyWindows[0] == null)
                {
                    _cachedHierarchyWindows = Resources.FindObjectsOfTypeAll(_hierarchyWindowType);
                }

                if (_cachedHierarchyWindows != null && _cachedHierarchyWindows.Length > 0)
                {
                    for (int w = 0; w < _cachedHierarchyWindows.Length; w++)
                    {
                        var window = _cachedHierarchyWindows[w];
                        if (window == null)
                        {
                            _cachedHierarchyWindows = null;
                            break;
                        }

                        var method = window.GetType().GetMethod("SetExpanded", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(window, new object[] { instanceID, expand });
                        }
                    }
                }
            }
            catch { }
        }

        private static void SetHierarchyWantsMouseMove()
        {
            InitializeReflection();
            if (_hierarchyWindowType != null)
            {
                if (_cachedHierarchyWindows == null || _cachedHierarchyWindows.Length == 0 || _cachedHierarchyWindows[0] == null)
                {
                    _cachedHierarchyWindows = Resources.FindObjectsOfTypeAll(_hierarchyWindowType);
                }

                if (_cachedHierarchyWindows != null)
                {
                    for (int w = 0; w < _cachedHierarchyWindows.Length; w++)
                    {
                        if (_cachedHierarchyWindows[w] is EditorWindow editorWin && !editorWin.wantsMouseMove)
                        {
                            editorWin.wantsMouseMove = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Intercepts hierarchy GUI rendering to draw custom category rows, folder icons, 
        /// relation lines, and handles custom input capture.
        /// </summary>
        private static void DrawCustomHierarchyItem(int instanceID, Rect selectionRect)
        {
            GUI.color = Color.white;
            GUI.contentColor = Color.white;

            if (HierarchySettings.ShowComponentIcons && HierarchySettings.ComponentIconsMode == ComponentIconsVisibility.OnHover)
            {
                SetHierarchyWantsMouseMove();
            }
            // Intercept scene header to draw a quick scene selector dropdown
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.handle == instanceID)
                {
                    DrawSceneHeaderDropdown(scene, selectionRect);
                    return;
                }
            }

            Event e = Event.current;
            if (e != null)
            {
                // Intercept Delete hotkeys for safe separator deletion logic
                bool isDeleteCommand = (e.type == EventType.ExecuteCommand || e.type == EventType.ValidateCommand) && 
                                       (e.commandName == "SoftDelete" || e.commandName == "Delete");

                if (isDeleteCommand)
                {
                    GameObject[] selectedObjects = Selection.gameObjects;
                    if (selectedObjects != null && selectedObjects.Length > 0)
                    {
                        List<GameObject> selectedSeparators = new List<GameObject>();
                        foreach (GameObject selObj in selectedObjects)
                        {
                            if (selObj != null && selObj.name.StartsWith("---") && selObj.name.EndsWith("---"))
                            {
                                selectedSeparators.Add(selObj);
                            }
                        }

                        if (selectedSeparators.Count > 0)
                        {
                            e.Use();

                            if (e.type == EventType.ExecuteCommand)
                            {
                                double currentTime = EditorApplication.timeSinceStartup;
                                if (currentTime - _lastDeleteTime > 0.05) // Debounce actual executions only
                                {
                                    _lastDeleteTime = currentTime;
                                    HierarchySafeDeleter.DeleteSeparatorsWithConfirmation(selectedSeparators, selectedObjects);
                                }
                            }
                        }
                    }
                }
            }

#if UNITY_6000_0_OR_NEWER
            GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
#else
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif
            if (obj == null) return;

            // Apply visual dimming if a dynamic component filter is active
            if (HierarchyFilterManager.ShouldDim(obj))
            {
                Color fadeColor = EditorGUIUtility.isProSkin 
                    ? new Color(0.22f, 0.22f, 0.22f, 0.72f) 
                    : new Color(0.76f, 0.76f, 0.76f, 0.72f);
                EditorGUI.DrawRect(selectionRect, fadeColor);
                
                GUI.color = new Color(1f, 1f, 1f, 0.22f);
                GUI.contentColor = new Color(1f, 1f, 1f, 0.22f);
            }
            
            // Intercept ALT + LMB to open the Hierarchify Control Center on the Icon Picker tab (ignore for category folders)
            bool isCategory = obj.name.StartsWith("---") && obj.name.EndsWith("---");
            float componentIconsWidth = HierarchySettings.ShowComponentIcons ? HierarchySettings.ComponentIconsOffset : 0f;
            Rect mainClickRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width - componentIconsWidth, selectionRect.height);

            if (e != null && e.type == EventType.MouseDown && e.button == 0 && e.alt && !isCategory)
            {
                if (mainClickRect.Contains(e.mousePosition))
                {
                    e.Use();
                    HierarchifyWindow.Open(obj, 2);
                    return;
                }
            }

            // 1. Calculate depth and full row rect spanning the entire width (including left indentation)
            int depth = 1;
            Transform tempT = obj.transform;
            while (tempT.parent != null)
            {
                depth++;
                tempT = tempT.parent;
            }
            float startX = selectionRect.x - (depth - 1) * 14f - 16f;
            float rowWidth = selectionRect.xMax - startX;
            Rect fullRowRect = new Rect(startX, selectionRect.y + 1, rowWidth, selectionRect.height - 2);

            // Repaint when hover state changes for OnHover mode
            if (HierarchySettings.ShowComponentIcons && HierarchySettings.ComponentIconsMode == ComponentIconsVisibility.OnHover)
            {
                if (e != null && (e.type == EventType.MouseMove || e.type == EventType.Repaint || e.type == EventType.Layout))
                {
                    bool isHovered = fullRowRect.Contains(e.mousePosition);
                    if (isHovered && _lastHoveredInstanceID != instanceID)
                    {
                        _lastHoveredInstanceID = instanceID;
                        EditorApplication.RepaintHierarchyWindow();
                    }
                    else if (!isHovered && _lastHoveredInstanceID == instanceID)
                    {
                        _lastHoveredInstanceID = 0;
                        EditorApplication.RepaintHierarchyWindow();
                    }
                }
            }

            // Repaint when Ctrl key state changes for OnCtrlPressed mode
            if (HierarchySettings.ShowComponentIcons && HierarchySettings.ComponentIconsMode == ComponentIconsVisibility.OnCtrlPressed)
            {
                if (e != null && e.control != _lastCtrlState)
                {
                    _lastCtrlState = e.control;
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            // Intercept CTRL + LMB click on the GameObject row to open the Component selection list popup
            if (e != null && e.control && !isCategory)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (fullRowRect.Contains(e.mousePosition))
                    {
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (fullRowRect.Contains(e.mousePosition))
                    {
                        e.Use();
                        Vector2 screenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                        Rect anchor = new Rect(e.mousePosition.x, e.mousePosition.y, 1, 1);
                        PopupWindow.Show(anchor, new ComponentSelectorPopup(obj, screenPos));
                        return;
                    }
                }
            }

            string name = obj.name;

            // 2. Cache category information (so it is always available for event processing and drawing)
            FolderCacheItem cacheItem = default;
            if (isCategory)
            {
                if (!_folderRenderCache.TryGetValue(instanceID, out cacheItem) || cacheItem.rawName != name)
                {
                    cacheItem.rawName = name;
                    cacheItem.cleanName = name.Replace("-", "").Trim();
                    cacheItem.accentColor = HierarchySettings.ColorDefault;

                    // --- SMART FOLDERS: AUTO-UI HEALING ---
                    // If a folder is placed in a UI Canvas or receives UI children, it must become a RectTransform 
                    // to prevent layout destruction and reference loss.
                    if (obj.GetComponent<RectTransform>() == null)
                    {
                        bool needsRectTransform = false;
                        if (obj.transform.parent != null && obj.transform.parent.GetComponent<RectTransform>() != null)
                        {
                            needsRectTransform = true;
                        }
                        else if (obj.transform.childCount > 0)
                        {
                            foreach (Transform child in obj.transform)
                            {
                                if (child.GetComponent<RectTransform>() != null)
                                {
                                    needsRectTransform = true;
                                    break;
                                }
                            }
                        }

                        if (needsRectTransform)
                        {
                            Undo.RecordObject(obj, "Smart Folder Auto-UI");
                            obj.AddComponent<RectTransform>();
                            RectTransform rt = obj.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                rt.anchorMin = Vector2.zero;
                                rt.anchorMax = Vector2.one;
                                rt.offsetMin = Vector2.zero;
                                rt.offsetMax = Vector2.zero;
                            }
                            Debug.Log($"<color=#1ea3ff><b>[NakeDev Smart Folders]</b></color> Auto-injected RectTransform into folder '{cacheItem.cleanName}' to preserve UI references!");
                        }
                    }

                    int hashIndex = cacheItem.cleanName.LastIndexOf('#');
                    if (hashIndex != -1 && hashIndex + 7 <= cacheItem.cleanName.Length)
                    {
                        string hexString = cacheItem.cleanName.Substring(hashIndex, 7);
                        if (ColorUtility.TryParseHtmlString(hexString, out Color parsedColor))
                        {
                            cacheItem.accentColor = parsedColor;
                        }
                        cacheItem.cleanName = cacheItem.cleanName.Substring(0, hashIndex).Trim();
                    }
                    else
                    {
                        cacheItem.accentColor = HierarchySettings.GetAccentColorForFolder(name);
                    }

                    _folderRenderCache[instanceID] = cacheItem;
                }

                // Check if color picker was requested via context menu/shortcut for this category folder
                if (_requestedColorPickerFolder != null && obj == _requestedColorPickerFolder)
                {
                    _requestedColorPickerFolder = null;
                    Rect popupAnchor = new Rect(selectionRect.x + 8f, selectionRect.y + selectionRect.height, 1, 1);
                    PopupWindow.Show(popupAnchor, new ColorPickerPopup(obj, cacheItem.accentColor));
                }

                // Check if Ctrl + Alt + C hotkey is pressed while this category folder is selected
                if (e != null && e.type == EventType.KeyDown && e.control && e.alt && e.keyCode == KeyCode.C && Selection.activeGameObject == obj)
                {
                    e.Use();
                    Rect popupAnchor = new Rect(selectionRect.x + 8f, selectionRect.y + selectionRect.height, 1, 1);
                    PopupWindow.Show(popupAnchor, new ColorPickerPopup(obj, cacheItem.accentColor));
                }
            }

            // 3. Draw backgrounds (only during Repaint event)
            if (Event.current.type == EventType.Repaint)
            {
                if (isCategory)
                {
                    // Draw solid theme background first to mask/erase the original GameObject name underneath
                    Color themeBgColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.22f, 0.22f, 0.22f, 1f) 
                        : new Color(0.76f, 0.76f, 0.76f, 1f);

                    if (Selection.Contains(obj))
                    {
                        themeBgColor = EditorGUIUtility.isProSkin 
                            ? new Color(0.1725f, 0.3647f, 0.5294f, 1f) 
                            : new Color(0.2274f, 0.447f, 0.6902f, 1f);
                    }
                    EditorGUI.DrawRect(fullRowRect, themeBgColor);

                    Color drawColor = cacheItem.accentColor;
                    if (Selection.Contains(obj))
                    {
                        drawColor = Color.Lerp(drawColor, Color.white, 0.25f);
                    }

                    // Apply soft opacity (25%) to the category gradient to prevent visual pollution
                    drawColor.a = 0.25f;

                    if (HierarchySettings.ColorEntireRow)
                    {
                        Color oldColor = GUI.color;
                        GUI.color = drawColor;
                        GUI.DrawTexture(fullRowRect, GradientTexture);
                        GUI.color = oldColor;
                    }
                    else
                    {
                        Color bgColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
                        if (Selection.Contains(obj))
                        {
                            bgColor = EditorGUIUtility.isProSkin ? new Color(0.24f, 0.24f, 0.24f, 1f) : new Color(0.68f, 0.68f, 0.68f, 1f);
                        }
                        EditorGUI.DrawRect(fullRowRect, bgColor);
                        Rect accentBarRect = new Rect(selectionRect.x, selectionRect.y + 1, 4, selectionRect.height - 2);
                        EditorGUI.DrawRect(accentBarRect, cacheItem.accentColor);
                    }
                }
                else
                {
                    // Child background tinting under category folders (subtle 3% tint for clean visual grouping)
                    if (GetParentCategoryColor(obj, out Color parentColor))
                    {
                        bool isSelected = Selection.Contains(obj);
                        if (!isSelected)
                        {
                            Color childBgColor = parentColor;
                            childBgColor.a = 0.03f; // subtle 3% tint
                            EditorGUI.DrawRect(fullRowRect, childBgColor);
                        }
                    }
                }
            }

            // 4. Draw relationship connection lines on top of the backgrounds (only during Repaint event)
            if (HierarchySettings.ShowTreeLines && Event.current.type == EventType.Repaint)
            {
                DrawTreeLines(obj, selectionRect);
            }

            // 5. Render item-specific content
            if (isCategory)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    // Draw standard folder icon at foldout position (replacing the foldout arrow)
                    if (HierarchySettings.ShowCustomFolderIcons)
                    {
                        bool isExpanded = IsHierarchyItemExpanded(instanceID);
                        Texture2D folderIcon = isExpanded 
                            ? (EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D) 
                            : (EditorGUIUtility.IconContent("Folder Icon").image as Texture2D);

                        if (folderIcon != null)
                        {
                            Rect iconRect = new Rect(selectionRect.x - 14f, selectionRect.y + (selectionRect.height - 16f) / 2f, 16f, 16f);
                            Color oldGuiColor = GUI.color;
                            GUI.color = Color.white;
                            GUI.DrawTexture(iconRect, folderIcon);
                            GUI.color = oldGuiColor;
                        }
                    }
                    else
                    {
                        // Fallback to standard foldout arrow if folder icons are disabled
                        if (obj.transform.childCount > 0)
                        {
                            bool isExpanded = IsHierarchyItemExpanded(instanceID);
                            Rect foldoutRect = new Rect(selectionRect.x - 14f, selectionRect.y + (selectionRect.height - 16f) / 2f, 16f, 16f);
                            EditorStyles.foldout.Draw(foldoutRect, false, false, isExpanded, false);
                        }
                    }

                    // Draw bold left-aligned category name (always clean text white/charcoal on transparent gradient)
                    Color textColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.92f, 0.92f, 0.92f, 1f) 
                        : new Color(0.15f, 0.15f, 0.15f, 1f);

                    GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = HierarchySettings.FolderFontSize,
                    };

                    float textOffset = HierarchySettings.ShowCustomFolderIcons ? 6f : 4f;
                    Rect textRect = new Rect(selectionRect.x + textOffset, selectionRect.y, selectionRect.width - textOffset, selectionRect.height);

                    Color outlineColor = EditorGUIUtility.isProSkin 
                        ? new Color(0.1f, 0.1f, 0.1f, 0.4f) 
                        : new Color(0.9f, 0.9f, 0.9f, 0.4f);
                    DrawTextWithOutline(textRect, cacheItem.cleanName, labelStyle, textColor, outlineColor, 1f);
                }

                // Handle click events
                if (e != null && e.type == EventType.MouseDown && e.button == 0)
                {
                    // 1. Capture clicks on our shifted custom folder icon to toggle expansion
                    if (HierarchySettings.ShowCustomFolderIcons)
                    {
                        Rect iconRect = new Rect(selectionRect.x - 14f, selectionRect.y + (selectionRect.height - 16f) / 2f, 16f, 16f);
                        if (iconRect.Contains(e.mousePosition) && obj.transform.childCount > 0)
                        {
                            bool isExpanded = IsHierarchyItemExpanded(instanceID);
                            ToggleHierarchyItemExpanded(instanceID, !isExpanded);
                            e.Use();
                            return;
                        }
                    }
                }
            }
            else
            {
                float currentX = selectionRect.xMax - HierarchySettings.ComponentIconsOffset;
                bool shouldDrawIcons = false;
                bool onlyShowOpenWindows = false;

                if (HierarchySettings.ShowComponentIcons)
                {
                    switch (HierarchySettings.ComponentIconsMode)
                    {
                        case ComponentIconsVisibility.Always:
                            shouldDrawIcons = true;
                            break;
                        case ComponentIconsVisibility.OnHover:
                            if (e != null && fullRowRect.Contains(e.mousePosition))
                            {
                                shouldDrawIcons = true;
                            }
                            else if (ComponentMiniMapWindow.HasAnyOpenWindowForGameObject(obj, out _))
                            {
                                shouldDrawIcons = true;
                                onlyShowOpenWindows = true;
                            }
                            break;
                        case ComponentIconsVisibility.OnCtrlPressed:
                            if (e != null && e.control)
                            {
                                shouldDrawIcons = true;
                            }
                            else if (ComponentMiniMapWindow.HasAnyOpenWindowForGameObject(obj, out _))
                            {
                                shouldDrawIcons = true;
                                onlyShowOpenWindows = true;
                            }
                            break;
                    }
                }

                if (shouldDrawIcons)
                {
                    DrawComponentIcons(obj, selectionRect, ref currentX, onlyShowOpenWindows);
                }

                if (Event.current.type == EventType.Repaint)
                {
                    DrawVertexCount(obj, selectionRect, ref currentX);

                    // Draw custom GameObject icon if set (overriding Unity's component icons)
                    Texture2D customIcon = EditorGUIUtility.GetIconForObject(obj);
                    if (customIcon != null)
                    {
                        Rect iconRect = new Rect(selectionRect.x, selectionRect.y + (selectionRect.height - 16f) / 2f, 16f, 16f);
                        
                        // Detect if selected to match background color mask
                        bool isSelected = Selection.Contains(obj);

                        Color maskColor;
                        if (isSelected)
                        {
                            maskColor = EditorGUIUtility.isProSkin 
                                ? new Color(0.1725f, 0.3647f, 0.5294f, 1f) 
                                : new Color(0.2274f, 0.447f, 0.6902f, 1f);
                        }
                        else
                        {
                            maskColor = EditorGUIUtility.isProSkin 
                                ? new Color(0.22f, 0.22f, 0.22f, 1f) 
                                : new Color(0.76f, 0.76f, 0.76f, 1f);
                        }

                        EditorGUI.DrawRect(iconRect, maskColor);
                        GUI.color = Color.white;
                        GUI.DrawTexture(iconRect, customIcon);
                    }
                }
            }

            DrawSentinelIcon(obj, selectionRect);
            DrawActiveToggle(obj, selectionRect);
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }

        private static void DrawSceneHeaderDropdown(UnityEngine.SceneManagement.Scene scene, Rect selectionRect)
        {
            string sceneName = scene.name;
            if (string.IsNullOrEmpty(sceneName)) return;

            // Calculate scene name text width to position the dropdown arrow next to it
            InitializeSharedContent();
            _tempSceneContent.text = sceneName;
            Vector2 labelSize = EditorStyles.boldLabel.CalcSize(_tempSceneContent);

            float arrowX = selectionRect.x + 36f + labelSize.x + 4f;
            Rect arrowRect = new Rect(arrowX, selectionRect.y, 16f, selectionRect.height);

            Event e = Event.current;
            if (e == null) return;

            bool isHovered = arrowRect.Contains(e.mousePosition);

            if (isHovered)
            {
                EditorGUIUtility.AddCursorRect(arrowRect, MouseCursor.Link);
            }

            // Draw dropdown arrow next to name
            if (e.type == EventType.Repaint)
            {
                Color arrowColor = isHovered 
                    ? (EditorGUIUtility.isProSkin ? Color.white : Color.black) 
                    : new Color(0.7f, 0.7f, 0.7f, 0.7f);

                GUIStyle arrowStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                arrowStyle.normal.textColor = arrowColor;
                
                // Draw a small ▾
                GUI.Label(new Rect(arrowRect.x, arrowRect.y - 1f, arrowRect.width, arrowRect.height), "▾", arrowStyle);
            }

            // Handle click to show the interactive SceneSelectorWindow
            if (e.type == EventType.MouseDown && e.button == 0 && isHovered)
            {
                e.Use();
                SceneSelectorWindow.ShowWindow(arrowRect);
            }
        }

        /// <summary>
        /// Renders the Hierarchy Sentinel diagnostic warning/error icon on the right side of the row.
        /// Spawns the diagnostics popup when clicked.
        /// </summary>
        private static void DrawSentinelIcon(GameObject obj, Rect selectionRect)
        {
            if (!HierarchySettings.ShowHierarchySentinel) return;

            // Fetch errors for this GameObject
            var errors = NakeDev.EditorTools.HierarchySentinel.GetErrors(obj);
            bool isParentWarning = false;
            bool hasRuntime = false;

            if (errors.Count == 0)
            {
                // Only bubble up descendant errors if this object is collapsed in the hierarchy!
                // If it is expanded, the warning naturally appears on the actual child down the tree.
                if (!IsHierarchyItemExpanded(obj.GetInstanceID()))
                {
                    if (NakeDev.EditorTools.HierarchySentinel.HasErrorsInDescendants(obj, out hasRuntime))
                    {
                        isParentWarning = true;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return; // Expanded without direct errors -> no icon.
                }
            }
            else
            {
                hasRuntime = NakeDev.EditorTools.HierarchySentinel.HasRuntimeErrors(obj);
            }

            float sentinelOffset = HierarchySettings.SentinelOffset;
            float iconSize = 14f; // Clean, slightly smaller to look crisp
            float iconX = selectionRect.xMax - sentinelOffset + 1f;

            // Stop drawing if it would overlap the GameObject name foldout/text zone
            if (iconX < selectionRect.x + 35f) return;

            Event e = Event.current;
            Rect sentinelRect = new Rect(iconX, selectionRect.y + (selectionRect.height - iconSize) / 2f, iconSize, iconSize);

            // Determine severity icon (Red error icon if severe runtime issues exist, otherwise Yellow warning icon)
            Texture2D icon = hasRuntime
                ? (EditorGUIUtility.IconContent("console.erroricon.sml").image as Texture2D)
                : (EditorGUIUtility.IconContent("console.warnicon.sml").image as Texture2D);

            if (icon != null)
            {
                // Dynamic hover highlight & cursor link
                if (e != null && sentinelRect.Contains(e.mousePosition))
                {
                    EditorGUI.DrawRect(sentinelRect, new Color(1f, 1f, 1f, 0.15f));
                    EditorGUIUtility.AddCursorRect(sentinelRect, MouseCursor.Link);
                }

                // Render the alert icon (slightly transparent for parent warning to be less intrusive)
                if (isParentWarning)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f); // 50% opacity
                }

                // If drawing on a bright folder, add a subtle dark contrast plate behind warning/error
                if (HierarchySettings.ColorEntireRow && IsCategoryFolder(obj, out Color folderColor))
                {
                    float folderLuminance = (0.299f * folderColor.r) + (0.587f * folderColor.g) + (0.114f * folderColor.b);
                    if (folderLuminance > 0.6f)
                    {
                        Rect backdrop = new Rect(sentinelRect.x - 1f, sentinelRect.y - 1f, sentinelRect.width + 2f, sentinelRect.height + 2f);
                        EditorGUI.DrawRect(backdrop, new Color(0.1f, 0.1f, 0.1f, 0.65f));
                    }
                }

                GUI.DrawTexture(sentinelRect, icon);
                GUI.color = Color.white;

                // Tooltips based on language and parent warning
                bool isPtBr = NakeDev.EditorTools.HierarchySentinel.IsPortuguese();
                string tooltipText = "";

                if (isParentWarning)
                {
                    tooltipText = isPtBr
                        ? "Um ou mais GameObjects filhos possuem avisos/erros. Clique para diagnosticar."
                        : "One or more child GameObjects have warnings/errors. Click to diagnose.";
                }
                else
                {
                    tooltipText = hasRuntime
                        ? (isPtBr ? $"{errors.Count} Erro(s) de tempo de execução ativo(s)! Clique para ver detalhes." : $"{errors.Count} Active Runtime Errors! Click for details.")
                        : (isPtBr ? $"{errors.Count} Aviso(s) do linter detectado(s). Clique para diagnosticar." : $"{errors.Count} Linter Warnings detected. Click to diagnose.");
                }

                GUI.Box(sentinelRect, new GUIContent("", tooltipText), GUIStyle.none);

                // Intercept Left Mouse Click to spawn the diagnostics popup in-place
                if (e != null && e.type == EventType.MouseDown && e.button == 0)
                {
                    if (sentinelRect.Contains(e.mousePosition))
                    {
                        e.Use();
                        Rect popupAnchor = new Rect(sentinelRect.x + 8f, sentinelRect.y + sentinelRect.height, 1, 1);
                        PopupWindow.Show(popupAnchor, new NakeDev.EditorTools.HierarchySentinelPopup(obj));
                    }
                }
            }
        }

        /// <summary>
        /// Renders a custom eye active toggle icon on the right side of the row and handles click events to enable/disable the GameObject.
        /// </summary>
        private static void DrawActiveToggle(GameObject obj, Rect selectionRect)
        {
            if (!HierarchySettings.ShowGameObjectActiveToggle) return;

            float eyeOffset = HierarchySettings.ActiveToggleOffset;
            float iconSize = 16f;
            float iconX = selectionRect.xMax - eyeOffset;

            // Stop drawing if it would overlap the GameObject foldout/text zone
            if (iconX < selectionRect.x + 15f) return;

            Event e = Event.current;
            Rect eyeRect = new Rect(iconX, selectionRect.y + (selectionRect.height - iconSize) / 2f, iconSize, iconSize);

            bool isActive = obj.activeSelf;
            Texture2D eyeIcon = isActive 
                ? (EditorGUIUtility.IconContent("scenevis_visible").image as Texture2D)
                : (EditorGUIUtility.IconContent("scenevis_hidden").image as Texture2D);

            if (eyeIcon != null)
            {
                // Draw dynamic hover and cursor cues
                if (e != null && eyeRect.Contains(e.mousePosition))
                {
                    EditorGUI.DrawRect(eyeRect, new Color(1f, 1f, 1f, 0.15f));
                    EditorGUIUtility.AddCursorRect(eyeRect, MouseCursor.Link);
                }

                // Apply opacity transparency based on active state and background luminance
                Color baseColor = Color.white;
                if (HierarchySettings.ColorEntireRow && IsCategoryFolder(obj, out Color folderColor))
                {
                    float folderLuminance = (0.299f * folderColor.r) + (0.587f * folderColor.g) + (0.114f * folderColor.b);
                    if (folderLuminance > 0.6f)
                    {
                        baseColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark charcoal for bright background
                    }
                }

                if (!isActive)
                {
                    GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f); // Faded for inactive GameObject
                }
                else
                {
                    GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f); // Solid for active GameObject
                }

                GUI.DrawTexture(eyeRect, eyeIcon);
                GUI.color = Color.white;

                // Tooltip text
                string tooltipText = isActive ? "Click to disable GameObject" : "Click to enable GameObject";
                GUI.Box(eyeRect, new GUIContent("", tooltipText), GUIStyle.none);

                // Handle click event
                if (e != null && e.type == EventType.MouseDown && e.button == 0)
                {
                    if (eyeRect.Contains(e.mousePosition))
                    {
                        Undo.RecordObject(obj, "Toggle GameObject Active State");
                        obj.SetActive(!isActive);
                        EditorUtility.SetDirty(obj);
                        EditorApplication.RepaintHierarchyWindow();
                        e.Use();
                    }
                }
            }
        }

        private static readonly Dictionary<System.Type, System.Reflection.PropertyInfo> _toggleableTypesCache = new Dictionary<System.Type, System.Reflection.PropertyInfo>();
        private static System.Reflection.PropertyInfo _behaviourEnabledProp;
        private static System.Reflection.PropertyInfo _colliderEnabledProp;
        private static System.Reflection.PropertyInfo _rendererEnabledProp;

        private static void InitializeEnabledProperties()
        {
            if (_behaviourEnabledProp != null) return;
            _behaviourEnabledProp = typeof(Behaviour).GetProperty("enabled");
            _colliderEnabledProp = typeof(Collider).GetProperty("enabled");
            _rendererEnabledProp = typeof(Renderer).GetProperty("enabled");
        }

        /// <summary>
        /// Helper to get the 'enabled' property of a component type, caching the result.
        /// </summary>
        private static System.Reflection.PropertyInfo GetEnabledProperty(System.Type type)
        {
            if (!_toggleableTypesCache.TryGetValue(type, out var prop))
            {
                prop = type.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                _toggleableTypesCache[type] = prop;
            }
            return prop;
        }

        /// <summary>
        /// Detects if a component can be toggled (enabled/disabled) and retrieves its 'enabled' property info.
        /// </summary>
        private static bool IsComponentToggleable(Component comp, out System.Reflection.PropertyInfo enabledProp)
        {
            enabledProp = null;
            if (comp == null) return false;

            InitializeEnabledProperties();

            if (comp is Behaviour)
            {
                enabledProp = _behaviourEnabledProp;
                return true;
            }
            if (comp is Collider)
            {
                enabledProp = _colliderEnabledProp;
                return true;
            }
            if (comp is Renderer)
            {
                enabledProp = _rendererEnabledProp;
                return true;
            }

            System.Type type = comp.GetType();
            enabledProp = GetEnabledProperty(type);
            return enabledProp != null && enabledProp.PropertyType == typeof(bool) && enabledProp.CanWrite;
        }

        /// <summary>
        /// Renders component icons for a GameObject and handles click events to toggle their enabled state.
        /// </summary>
        private static void DrawComponentIcons(GameObject obj, Rect selectionRect, ref float currentX, bool onlyShowOpenWindows)
        {
            if (!HierarchySettings.ShowComponentIcons) return;

            _tempComponentsList.Clear();
            obj.GetComponents<Component>(_tempComponentsList);
            if (_tempComponentsList.Count == 0) return;

            // Filter components to build a list of relevant drawable components
            _validComponentsList.Clear();
            for (int idx = 0; idx < _tempComponentsList.Count; idx++)
            {
                Component c = _tempComponentsList[idx];
                if (c == null) continue;
                if (c is Transform || c.GetType().Name == "CanvasRenderer") continue;
                
                // If only showing open windows, check if this component has an active window open
                if (onlyShowOpenWindows && !ComponentMiniMapWindow.IsWindowOpen(c)) continue;

                // Only consider it drawable if it has a valid Unity ObjectContent icon
                Texture icon = EditorGUIUtility.ObjectContent(c, c.GetType()).image;
                if (icon != null)
                {
                    _validComponentsList.Add(c);
                }
            }

            if (_validComponentsList.Count == 0) return;

            Event e = Event.current;
            float iconSize = HierarchySettings.ComponentIconSize;
            float spacing = HierarchySettings.ComponentIconSpacing;
            float minAllowedX = selectionRect.x + 45f;

            int maxIcons = 5;
            // Disable ellipsis overflow if we are only drawing the currently open windows
            bool showOverflow = !onlyShowOpenWindows && (_validComponentsList.Count > maxIcons);
            int iconsToDraw = showOverflow ? maxIcons - 1 : _validComponentsList.Count;

            // Draw overflow ellipsis if there are too many components
            if (showOverflow)
            {
                currentX -= (iconSize + spacing);
                if (currentX >= minAllowedX)
                {
                    Rect ellipsisRect = new Rect(currentX, selectionRect.y + (selectionRect.height - iconSize) / 2f, iconSize, iconSize);

                    Texture ellipsisIcon = EditorGUIUtility.IconContent("d_more").image;
                    if (ellipsisIcon == null) ellipsisIcon = EditorGUIUtility.IconContent("more").image;

                    // Handle click on ellipsis to open ComponentSelectorPopup
                    if (e != null)
                    {
                        if (ellipsisRect.Contains(e.mousePosition))
                        {
                            if (e.type == EventType.MouseDown && e.button == 0)
                            {
                                e.Use();
                            }
                            else if (e.type == EventType.MouseUp && e.button == 0)
                            {
                                e.Use();
                                Vector2 screenPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                                Rect anchor = new Rect(ellipsisRect.x, ellipsisRect.y + ellipsisRect.height, 1, 1);
                                PopupWindow.Show(anchor, new ComponentSelectorPopup(obj, screenPos));
                            }
                        }
                    }

                    if (e != null && e.type == EventType.Repaint)
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.7f);
                        if (ellipsisIcon != null)
                        {
                            GUI.DrawTexture(ellipsisRect, ellipsisIcon);
                        }
                        else
                        {
                            GUIStyle ellipsisStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                fontStyle = FontStyle.Bold
                            };
                            GUI.Label(ellipsisRect, "...", ellipsisStyle);
                        }
                        GUI.color = Color.white;

                        InitializeSharedContent();
                        _tempTooltipContent.text = "";
                        _tempTooltipContent.tooltip = $"And {_validComponentsList.Count - iconsToDraw} other components...\n(Click to open list)";
                        GUI.Box(ellipsisRect, _tempTooltipContent, GUIStyle.none);
                    }
                }
            }

            // Draw the individual component icons (right-to-left loop)
            for (int i = iconsToDraw - 1; i >= 0; i--)
            {
                Component comp = _validComponentsList[i];
                if (comp == null) continue;

                currentX -= (iconSize + spacing);
                if (currentX < minAllowedX) break;

                Rect iconRect = new Rect(currentX, selectionRect.y + (selectionRect.height - iconSize) / 2f, iconSize, iconSize);

                bool isToggleable = IsComponentToggleable(comp, out var enabledProp);
                bool isEnabled = true;

                if (isToggleable && enabledProp != null)
                {
                    isEnabled = (bool)enabledProp.GetValue(comp);
                }

                // Drawing and Hover states (only during Repaint event)
                if (e != null && e.type == EventType.Repaint)
                {
                    // Set transparency and draw icon
                    if (isToggleable && !isEnabled)
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.3f); // Faded out for disabled components
                    }
                    else
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.9f); // Solid for active components
                    }

                    Texture icon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image;
                    if (icon != null)
                    {
                        GUI.DrawTexture(iconRect, icon);
                    }
                    GUI.color = Color.white;

                    // Create tooltip
                    InitializeSharedContent();
                    _tempTooltipContent.text = "";
                    _tempTooltipContent.tooltip = $"Component: {comp.GetType().Name}";
                    GUI.Box(iconRect, _tempTooltipContent, GUIStyle.none);
                }
            }
        }

        private static Texture2D _gradientTexture;
        private static Texture2D GradientTexture
        {
            get
            {
                if (_gradientTexture == null)
                {
                    _gradientTexture = new Texture2D(32, 1);
                    _gradientTexture.wrapMode = TextureWrapMode.Clamp;
                    _gradientTexture.filterMode = FilterMode.Bilinear;
                    Color[] colors = new Color[32];
                    for (int i = 0; i < 32; i++)
                    {
                        float t = (float)i / 31f;
                        colors[i] = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.15f, t));
                    }
                    _gradientTexture.SetPixels(colors);
                    _gradientTexture.Apply();
                }
                return _gradientTexture;
            }
        }

        private static bool GetParentCategoryColor(GameObject obj, out Color parentColor)
        {
            parentColor = Color.clear;
            if (obj == null) return false;

            Transform t = obj.transform.parent;
            while (t != null)
            {
                if (t.name.StartsWith("---") && t.name.EndsWith("---"))
                {
                    int parentID = t.gameObject.GetInstanceID();
                    if (_folderRenderCache.TryGetValue(parentID, out FolderCacheItem cacheItem))
                    {
                        parentColor = cacheItem.accentColor;
                        return true;
                    }
                    parentColor = HierarchySettings.GetAccentColorForFolder(t.name);
                    return true;
                }
                t = t.parent;
            }
            return false;
        }

        private static bool IsAncestorOf(GameObject potentialAncestor, GameObject selected)
        {
            if (selected == null || potentialAncestor == null) return false;
            if (selected == potentialAncestor) return true;

            Transform t = selected.transform;
            while (t.parent != null)
            {
                if (t.parent.gameObject == potentialAncestor)
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static Color GetSeparatorAccentColor(GameObject go)
        {
            Transform t = go.transform;
            while (t != null)
            {
                if (t.name.StartsWith("---") && t.name.EndsWith("---"))
                {
                    if (_folderRenderCache.TryGetValue(t.gameObject.GetInstanceID(), out FolderCacheItem cacheItem))
                    {
                        return cacheItem.accentColor;
                    }
                    return HierarchySettings.GetAccentColorForFolder(t.name);
                }
                t = t.parent;
            }
            return EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f, 0.75f) : new Color(0.15f, 0.15f, 0.15f, 0.80f);
        }

        private static void DrawDashedVerticalLine(float x, float yMin, float yMax, Color color)
        {
            float dashSize = HierarchySettings.TreeLineDashSize;
            float gapSize = dashSize * 0.65f;
            float step = dashSize + gapSize;

            float startY = Mathf.Floor(yMin);
            float endY = Mathf.Floor(yMax);

            for (float y = startY; y < endY; y += step)
            {
                float currentDashHeight = Mathf.Min(dashSize, endY - y);
                Rect dashRect = new Rect(x, y, 1f, currentDashHeight);
                EditorGUI.DrawRect(dashRect, color);
            }
        }

        private static void DrawDashedHorizontalLine(float xMin, float xMax, float y, Color color)
        {
            float dashSize = HierarchySettings.TreeLineDashSize;
            float gapSize = dashSize * 0.5f;
            float step = dashSize + gapSize;

            float startX = Mathf.Floor(xMin);
            float endX = Mathf.Floor(xMax);

            for (float x = startX; x < endX; x += step)
            {
                float currentDashWidth = Mathf.Min(dashSize, endX - x);
                Rect dashRect = new Rect(x, y, currentDashWidth, 1f);
                EditorGUI.DrawRect(dashRect, color);
            }
        }

        /// <summary>
        /// Renders visual hierarchy relationship connection guidelines (tree lines).
        /// </summary>
        private static void DrawTreeLines(GameObject obj, Rect selectionRect)
        {
            int depth = 1;
            Transform t = obj.transform;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }

            GameObject selected = Selection.activeGameObject;
            bool isSelectionPath = HierarchySettings.HighlightActiveTreeLines && IsAncestorOf(obj, selected);

            Color accentColor = GetSeparatorAccentColor(obj);
            
            Color defaultLineColor = EditorGUIUtility.isProSkin 
                ? new Color(0.65f, 0.65f, 0.65f, HierarchySettings.TreeLineOpacity) 
                : new Color(0.35f, 0.35f, 0.35f, HierarchySettings.TreeLineOpacity * 1.1f);

            Color highlightColor = accentColor;
            highlightColor.a = 0.95f;

            float rowHeight = selectionRect.height;
            float midY = selectionRect.y + rowHeight / 2f;

            // Draw horizontal branch segment
            float branchX = selectionRect.x - 7f;
            float endX = selectionRect.x - 1f;
            
            if (isSelectionPath)
            {
                Rect horizRect = new Rect(branchX, Mathf.Round(midY), endX - branchX, 1f);
                EditorGUI.DrawRect(horizRect, highlightColor);
            }
            else
            {
                DrawDashedHorizontalLine(branchX, endX, Mathf.Round(midY), defaultLineColor);
            }

            // Draw vertical hierarchy grid segments per depth level
            for (int i = 0; i < depth; i++)
            {
                Transform ancestor = GetAncestorAtDepth(obj, i);
                if (ancestor == null) continue;

                float x = selectionRect.x - (depth - i) * 14f + 7f;
                bool isLast = IsLastChild(ancestor);
                bool isColSelected = IsAncestorOf(ancestor.gameObject, selected);
                
                Color colColor = isColSelected ? highlightColor : defaultLineColor;

                if (!isLast)
                {
                    if (isColSelected)
                    {
                        Rect vertRect = new Rect(x, selectionRect.y, 1f, rowHeight);
                        EditorGUI.DrawRect(vertRect, colColor);
                    }
                    else
                    {
                        DrawDashedVerticalLine(x, selectionRect.y, selectionRect.y + rowHeight, colColor);
                    }
                }
                else
                {
                    if (i == depth - 1)
                    {
                        if (isColSelected)
                        {
                            Rect vertRect = new Rect(x, selectionRect.y, 1f, rowHeight / 2f + 0.5f);
                            EditorGUI.DrawRect(vertRect, colColor);
                        }
                        else
                        {
                            DrawDashedVerticalLine(x, selectionRect.y, selectionRect.y + rowHeight / 2f + 0.5f, colColor);
                        }
                    }
                }
            }
        }

        private static Transform GetAncestorAtDepth(GameObject obj, int targetDepth)
        {
            Transform t = obj.transform;
            int currentDepth = 0;
            Transform temp = t;
            while (temp.parent != null)
            {
                currentDepth++;
                temp = temp.parent;
            }
            
            int steps = currentDepth - targetDepth;
            Transform ancestor = t;
            for (int s = 0; s < steps; s++)
            {
                if (ancestor.parent != null)
                {
                    ancestor = ancestor.parent;
                }
            }
            return ancestor;
        }

        private static void DrawVertexCount(GameObject obj, Rect selectionRect, ref float currentX)
        {
            if (!HierarchySettings.ShowVertexCount) return;

            Mesh mesh = null;
            var meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.sharedMesh;
            }
            else
            {
                var skinnedRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinnedRenderer != null)
                {
                    mesh = skinnedRenderer.sharedMesh;
                }
            }

            if (mesh != null)
            {
                int vertexCount = mesh.vertexCount;
                string vText = FormatVertexCount(vertexCount);

                GUIStyle vStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 8,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f, 0.8f) : new Color(0.4f, 0.4f, 0.4f, 0.8f) }
                };

                Vector2 size = vStyle.CalcSize(new GUIContent(vText));
                currentX -= (size.x + 4f); // Add a small spacing gap

                // Draw if it fits within the allowed name space
                if (currentX > selectionRect.x + 45f)
                {
                    Rect vRect = new Rect(currentX, selectionRect.y + (selectionRect.height - size.y) / 2f, size.x, size.y);
                    GUI.Label(vRect, vText, vStyle);

                    // Add tooltip
                    GUI.Box(vRect, new GUIContent("", $"Mesh Vertices: {vertexCount:N0}"), GUIStyle.none);
                }
            }
        }

        private static string FormatVertexCount(int count)
        {
            if (count >= 1000000)
                return (count / 1000000f).ToString("F1") + "M v";
            if (count >= 1000)
                return (count / 1000f).ToString("F1") + "k v";
            return count.ToString() + " v";
        }

        private static bool IsLastChild(Transform t)
        {
            if (t.parent == null)
            {
                return IsLastRootObject(t.gameObject);
            }
            return t.parent.GetChild(t.parent.childCount - 1) == t;
        }

        private static bool IsLastRootObject(GameObject obj)
        {
            var scene = obj.scene;
            if (!scene.isLoaded) return true;

            int sceneHandle = scene.handle;
            if (!_lastRootCache.TryGetValue(sceneHandle, out int lastRootID))
            {
                var roots = scene.GetRootGameObjects();
                if (roots.Length > 0)
                {
                    lastRootID = roots[roots.Length - 1].GetInstanceID();
                    _lastRootCache[sceneHandle] = lastRootID;
                }
                else
                {
                    lastRootID = 0;
                }
            }

            return obj.GetInstanceID() == lastRootID;
        }

        private static bool IsCategoryFolder(GameObject obj, out Color folderColor)
        {
            folderColor = Color.white;
            if (obj == null) return false;
            bool isFolder = obj.name.StartsWith("---") && obj.name.EndsWith("---");
            if (isFolder)
            {
                folderColor = HierarchySettings.GetAccentColorForFolder(obj.name);
                if (_folderRenderCache.TryGetValue(obj.GetInstanceID(), out FolderCacheItem cacheItem))
                {
                    folderColor = cacheItem.accentColor;
                }
                return true;
            }
            return false;
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
    }

    /// <summary>
    /// A premium custom popup window showing a list of components with high-resolution icons.
    /// </summary>
    public class ComponentSelectorPopup : PopupWindowContent
    {
        private readonly GameObject _gameObject;
        private readonly Vector2 _screenPos;
        private Vector2 _scrollPos;

        public ComponentSelectorPopup(GameObject gameObject, Vector2 screenPos)
        {
            _gameObject = gameObject;
            _screenPos = screenPos;
        }

        public override Vector2 GetWindowSize()
        {
            if (_gameObject == null) return new Vector2(220f, 50f);
            Component[] comps = _gameObject.GetComponents<Component>();
            int count = 0;
            foreach (var c in comps)
            {
                if (c != null && !(c is Transform) && c.GetType().Name != "CanvasRenderer") count++;
            }
            float height = Mathf.Min(250f, count * 24f + 8f);
            return new Vector2(220f, Mathf.Max(30f, height));
        }

        public override void OnGUI(Rect rect)
        {
            if (_gameObject == null)
            {
                editorWindow.Close();
                return;
            }

            Component[] comps = _gameObject.GetComponents<Component>();

            // Draw dark background matching Unity editor styling
            Color bg = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.88f, 0.88f, 0.88f, 1f);
            EditorGUI.DrawRect(rect, bg);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            GUILayout.Space(4f);

            GUIStyle itemStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 22f,
                fontSize = 11,
                padding = new RectOffset(6, 6, 0, 0)
            };

            foreach (var c in comps)
            {
                if (c == null) continue;
                if (c is Transform || c.GetType().Name == "CanvasRenderer") continue;

                Texture icon = EditorGUIUtility.ObjectContent(c, c.GetType()).image;
                string name = c.GetType().Name;

                Rect itemRect = EditorGUILayout.GetControlRect(true, 22f);
                
                // Hover highlight matching Unity Pro skin selection colors
                bool isHovered = itemRect.Contains(Event.current.mousePosition);
                if (isHovered)
                {
                    Color hoverColor = EditorGUIUtility.isProSkin ? new Color(0.24f, 0.37f, 0.58f, 1f) : new Color(0.7f, 0.8f, 0.95f, 1f);
                    EditorGUI.DrawRect(itemRect, hoverColor);
                    EditorGUIUtility.AddCursorRect(itemRect, MouseCursor.Link);
                }

                // Draw high-resolution component icon
                if (icon != null)
                {
                    GUI.DrawTexture(new Rect(itemRect.x + 4f, itemRect.y + 3f, 16f, 16f), icon);
                }
                
                // Adaptive label colors
                GUI.color = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                GUI.Label(new Rect(itemRect.x + 24f, itemRect.y, itemRect.width - 24f, itemRect.height), name, itemStyle);
                GUI.color = Color.white;

                // Handle item click to open separate window
                if (isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    Component captureComp = c;
                    Vector2 capturePos = _screenPos;
                    EditorApplication.delayCall += () => ComponentMiniMapWindow.ShowWindow(captureComp, capturePos);
                    editorWindow.Close();
                    Event.current.Use();
                }
            }

            GUILayout.Space(4f);
            EditorGUILayout.EndScrollView();
        }
    }
}