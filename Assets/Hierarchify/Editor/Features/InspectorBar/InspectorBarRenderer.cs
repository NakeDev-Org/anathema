using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Renders the UI for the custom Navigation and Bookmarks Bar (Inspector Bar) at the absolute top of the Inspector window.
    /// Integrated with modern UI Toolkit containers, drawing the layout and managing interactions.
    /// </summary>
    [InitializeOnLoad]
    public static class InspectorBarRenderer
    {
        private static double _lastCheckTime = 0;

        static InspectorBarRenderer()
        {
            // Initialize the manager logic and hooks
            InspectorBarManager.Initialize();

            // Render container hooks
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            Selection.selectionChanged -= UpdateInspectorContainers;
            Selection.selectionChanged += UpdateInspectorContainers;

            EditorApplication.hierarchyChanged -= UpdateInspectorContainers;
            EditorApplication.hierarchyChanged += UpdateInspectorContainers;

            EditorApplication.projectChanged -= UpdateInspectorContainers;
            EditorApplication.projectChanged += UpdateInspectorContainers;

            // Perform initial setup
            UpdateInspectorContainers();
        }

        private static void OnUpdate()
        {
            // Throttled check for active Inspector windows
            if (EditorApplication.timeSinceStartup - _lastCheckTime > 0.5)
            {
                _lastCheckTime = EditorApplication.timeSinceStartup;
                UpdateInspectorContainers();
            }
        }

        private static void UpdateInspectorContainers()
        {
            bool shouldShow = HierarchySettings.ShowInspectorBar;

            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType == null) return;

            var inspectors = Resources.FindObjectsOfTypeAll(inspectorType);
            foreach (var inspector in inspectors)
            {
                if (inspector is EditorWindow win)
                {
                    var root = win.rootVisualElement;
                    if (root != null)
                    {
                        var existing = root.Q<IMGUIContainer>("Hierarchify_InspectorBar");
                        if (!HierarchySettings.ShowInspectorBar)
                        {
                            if (existing != null)
                            {
                                root.Remove(existing);
                                win.Repaint();
                            }
                            continue;
                        }

                        if (existing == null)
                        {
                            if (shouldShow)
                            {
                                var container = new IMGUIContainer(DrawInspectorBar);
                                container.name = "Hierarchify_InspectorBar";
                                container.style.display = DisplayStyle.Flex;
                                root.Insert(0, container);
                                win.Repaint();
                            }
                        }
                        else
                        {
                            var currentDisplay = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                            if (existing.style.display != currentDisplay)
                            {
                                existing.style.display = currentDisplay;
                                win.Repaint();
                            }
                        }
                    }
                }
            }
        }

        private static void DrawInspectorBar()
        {
            // Clean destroyed objects from bookmarks
            InspectorBarManager.CleanBookmarks();

            Event evt = Event.current;

            // 1. Get the layout rect for the custom bar
            float barHeight = 24f;
            Rect barRect = GUILayoutUtility.GetRect(0.0f, barHeight, GUILayout.ExpandWidth(true));

            // 2. Draw modern background matching Unity's theme (dark pro skin vs light skin)
            Color bgColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
            EditorGUI.DrawRect(barRect, bgColor);

            // Draw a subtle bottom border
            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.65f, 0.65f, 0.65f, 1f);
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1, barRect.width, 1), borderColor);

            // Request repaint on MouseMove to trigger immediate responsive hover styling
            if (barRect.Contains(evt.mousePosition) && evt.type == EventType.MouseMove)
            {
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

            float currentX = barRect.x + 6f;
            float btnY = barRect.y + (barHeight - 20f) / 2f;

            // Determine navigation bounds based on current bookmark index
            int currentIndex = InspectorBarManager.GetCurrentBookmarkIndex();
            var bookmarks = InspectorBarManager.Bookmarks;
            bool canGoBack = currentIndex == -1 ? bookmarks.Count > 0 : currentIndex > 0;
            bool canGoForward = currentIndex == -1 ? false : currentIndex < bookmarks.Count - 1;

            // 3. Draw Back Button
            Rect backRect = new Rect(currentX, btnY, 20f, 20f);
            if (DrawCustomButton(backRect, new GUIContent("<"), canGoBack))
            {
                InspectorBarManager.NavigateBookmarks(-1);
                evt.Use();
            }
            currentX += 22f;

            // 4. Draw Forward Button
            Rect forwardRect = new Rect(currentX, btnY, 20f, 20f);
            if (DrawCustomButton(forwardRect, new GUIContent(">"), canGoForward))
            {
                InspectorBarManager.NavigateBookmarks(1);
                evt.Use();
            }
            currentX += 24f;

            // 5. Draw Separator
            Rect sepRect = new Rect(currentX, barRect.y + 4f, 1f, barHeight - 8f);
            Color sepColor = EditorGUIUtility.isProSkin ? new Color(0.28f, 0.28f, 0.28f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
            EditorGUI.DrawRect(sepRect, sepColor);
            currentX += 8f;

            // 6. Draw Bookmarks aligned to the right side of the toolbar
            if (bookmarks.Count == 0)
            {
                Rect labelRect = new Rect(currentX, barRect.y, barRect.width - (currentX - barRect.x), barRect.height);
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : new Color(0.4f, 0.4f, 0.4f, 0.7f) }
                };
                GUI.Label(labelRect, " Drag & Drop GameObjects here to create bookmarks", labelStyle);
            }
            else
            {
                float totalBookmarksWidth = bookmarks.Count * 26f - 2f;
                float startX = Mathf.Max(currentX, barRect.xMax - 6f - totalBookmarksWidth);

                for (int i = 0; i < bookmarks.Count; i++)
                {
                    GameObject bookmark = bookmarks[i];
                    if (bookmark == null) continue;

                    // Overflow protection
                    if (startX + 24f > barRect.xMax - 6f) break;

                    // Sync custom icons chosen in Hierarchify, with fallback to standard Unity GameObject icon
                    Texture2D icon = EditorGUIUtility.GetIconForObject(bookmark);
                    if (icon == null)
                    {
                        icon = EditorGUIUtility.ObjectContent(bookmark, typeof(GameObject)).image as Texture2D;
                    }

                    GUIContent content = new GUIContent("", icon, bookmark.name);
                    Rect btnRect = new Rect(startX, btnY, 24f, 20f);

                    bool isSelected = Selection.activeGameObject == bookmark;

                    if (DrawCustomButton(btnRect, content, true, isSelected))
                    {
                        if (evt.button == 0) // Left click
                        {
                            Selection.activeGameObject = bookmark;
                            EditorGUIUtility.PingObject(bookmark);
                            evt.Use();
                        }
                    }

                    // Handle Right Click / Context Menu on MouseDown
                    if (evt.type == EventType.MouseDown && evt.button == 1 && btnRect.Contains(evt.mousePosition))
                    {
                        ShowBookmarkContextMenu(bookmark, i);
                        evt.Use();
                    }

                    startX += 26f;
                }
            }

            // 7. Handle Drag & Drop in the entire bar area
            if (barRect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                }
                else if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject go)
                        {
                            InspectorBarManager.AddBookmark(go);
                        }
                    }
                    evt.Use();
                }
            }

            // Prevent click fall-through to components behind the bar
            if (barRect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.MouseDown || evt.type == EventType.MouseUp)
                {
                    evt.Use();
                }
            }
        }

        private static bool DrawCustomButton(Rect rect, GUIContent content, bool isActive, bool isSelected = false)
        {
            Event evt = Event.current;
            bool isHover = rect.Contains(evt.mousePosition);
            bool clicked = false;

            if (isSelected)
            {
                EditorGUI.DrawRect(rect, new Color(0.18f, 0.49f, 0.96f, 0.25f));
                DrawRectOutline(rect, new Color(0.18f, 0.49f, 0.96f, 0.5f));
            }
            else if (isHover && isActive)
            {
                EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.08f));
                DrawRectOutline(rect, new Color(1f, 1f, 1f, 0.15f));
            }

            if (content.image != null)
            {
                float iconSize = 16f;
                Rect iconRect = new Rect(rect.x + (rect.width - iconSize) / 2f, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);

                Color originalColor = GUI.color;
                if (!isActive) GUI.color = new Color(1f, 1f, 1f, 0.4f);

                GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit);
                GUI.color = originalColor;
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isActive ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.45f, 0.45f, 0.45f) },
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(rect, content, labelStyle);
            }

            if (isActive && isHover)
            {
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    clicked = true;
                }
            }

            if (isHover && !string.IsNullOrEmpty(content.tooltip))
            {
                GUI.tooltip = content.tooltip;
            }

            return clicked;
        }

        private static void DrawRectOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color); // Top
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color); // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color); // Right
        }

        private static void ShowBookmarkContextMenu(GameObject bookmark, int index)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Select"), false, () => {
                Selection.activeGameObject = bookmark;
            });
            menu.AddItem(new GUIContent("Ping"), false, () => {
                EditorGUIUtility.PingObject(bookmark);
            });
            menu.AddSeparator("");

            if (index > 0)
            {
                menu.AddItem(new GUIContent("Move Left"), false, () => {
                    InspectorBarManager.MoveBookmark(index, index - 1);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Left"));
            }

            if (index < InspectorBarManager.Bookmarks.Count - 1)
            {
                menu.AddItem(new GUIContent("Move Right"), false, () => {
                    InspectorBarManager.MoveBookmark(index, index + 1);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Right"));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Remove Bookmark"), false, () => {
                InspectorBarManager.RemoveBookmarkAt(index);
            });

            menu.ShowAsContext();
        }
    }
}
