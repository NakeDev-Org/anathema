using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Manages data state, scene-based persistence, and business logic for the Inspector Bar feature in Hierarchify.
    /// </summary>
    public static class InspectorBarManager
    {
        private static readonly List<GameObject> _bookmarks = new List<GameObject>();
        
        /// <summary>
        /// Retrieves the list of currently bookmarked GameObjects.
        /// </summary>
        public static List<GameObject> Bookmarks => _bookmarks;

        private static int _lastSelectedBookmarkIndex = 0;
        private static string _lastLoadedScene = "";

        /// <summary>
        /// Initializes the selection change and scene loading listeners.
        /// </summary>
        public static void Initialize()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;

            LoadBookmarks();
        }

        private static void OnActiveSceneChanged(Scene current, Scene next)
        {
            _lastLoadedScene = next.path;
            LoadBookmarks();
        }

        private static void OnSelectionChanged()
        {
            GameObject active = Selection.activeGameObject;
            if (active == null) return;

            CleanBookmarks();

            int index = _bookmarks.IndexOf(active);
            if (index >= 0)
            {
                _lastSelectedBookmarkIndex = index;
            }
        }

        /// <summary>
        /// Returns the index of the currently active selection in the bookmarks list (-1 if not present).
        /// </summary>
        public static int GetCurrentBookmarkIndex()
        {
            if (Selection.activeGameObject == null) return -1;
            return _bookmarks.IndexOf(Selection.activeGameObject);
        }

        /// <summary>
        /// Navigates the active GameObject selection to the next or previous bookmark item.
        /// </summary>
        public static void NavigateBookmarks(int direction)
        {
            if (_bookmarks.Count == 0) return;

            CleanBookmarks();
            if (_bookmarks.Count == 0) return;

            int currentIndex = GetCurrentBookmarkIndex();
            int targetIndex = 0;

            if (currentIndex == -1)
            {
                // If the selection is not in the list, default back to the last selected index
                targetIndex = Mathf.Clamp(_lastSelectedBookmarkIndex, 0, _bookmarks.Count - 1);
            }
            else
            {
                targetIndex = Mathf.Clamp(currentIndex + direction, 0, _bookmarks.Count - 1);
            }

            if (targetIndex >= 0 && targetIndex < _bookmarks.Count)
            {
                _lastSelectedBookmarkIndex = targetIndex;
                Selection.activeGameObject = _bookmarks[targetIndex];
                if (Selection.activeGameObject != null)
                {
                    EditorGUIUtility.PingObject(Selection.activeGameObject);
                }
            }
        }

        /// <summary>
        /// Adds a GameObject to the bookmarks list and triggers saving.
        /// </summary>
        public static void AddBookmark(GameObject go)
        {
            if (go == null) return;

            CleanBookmarks();
            if (!_bookmarks.Contains(go))
            {
                _bookmarks.Add(go);
                SaveBookmarks();
            }
        }

        /// <summary>
        /// Removes a bookmark at a specific index.
        /// </summary>
        public static void RemoveBookmarkAt(int index)
        {
            if (index >= 0 && index < _bookmarks.Count)
            {
                _bookmarks.RemoveAt(index);
                SaveBookmarks();
            }
        }

        /// <summary>
        /// Moves a bookmark from one index position to another.
        /// </summary>
        public static void MoveBookmark(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < _bookmarks.Count && toIndex >= 0 && toIndex < _bookmarks.Count)
            {
                GameObject temp = _bookmarks[fromIndex];
                _bookmarks.RemoveAt(fromIndex);
                _bookmarks.Insert(toIndex, temp);
                SaveBookmarks();
            }
        }

        /// <summary>
        /// Sanitizes the bookmarks list, removing any missing/null GameObjects.
        /// </summary>
        public static void CleanBookmarks()
        {
            bool changed = false;
            for (int i = _bookmarks.Count - 1; i >= 0; i--)
            {
                if (_bookmarks[i] == null)
                {
                    _bookmarks.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed)
            {
                SaveBookmarks();
            }
        }

        /// <summary>
        /// Loads bookmarks for the currently active scene from EditorPrefs.
        /// </summary>
        public static void LoadBookmarks()
        {
            _bookmarks.Clear();
            string scenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath)) return;

            string data = EditorPrefs.GetString("Hierarchify_Bookmarks_" + scenePath, "");
            if (string.IsNullOrEmpty(data)) return;

            string[] ids = data.Split(';');
            foreach (var idStr in ids)
            {
                if (GlobalObjectId.TryParse(idStr, out GlobalObjectId id))
                {
                    Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                    if (obj is GameObject go)
                    {
                        _bookmarks.Add(go);
                    }
                }
            }
        }

        private static void SaveBookmarks()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath)) return;

            List<string> ids = new List<string>();
            foreach (var go in _bookmarks)
            {
                if (go != null)
                {
                    GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(go);
                    ids.Add(id.ToString());
                }
            }
            EditorPrefs.SetString("Hierarchify_Bookmarks_" + scenePath, string.Join(";", ids));
        }
    }
}
