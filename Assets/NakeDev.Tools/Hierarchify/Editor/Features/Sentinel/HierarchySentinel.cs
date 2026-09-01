using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Proactive linter and diagnostics broker for Hierarchify.
    /// Runs physical health checks and safely communicates with Smart Console via a reflection bridge.
    /// </summary>
    public static class HierarchySentinel
    {
        private struct SentinelResult
        {
            public List<string> allErrors;
            public bool hasRuntimeErrors;
        }

        private static readonly Dictionary<int, SentinelResult> _sentinelCache = new Dictionary<int, SentinelResult>();
        private static readonly Dictionary<int, double> _sentinelCacheTime = new Dictionary<int, double>();
        private const double CacheDuration = 1.0; // Cache linter results for 1 second for perfect 60+ FPS performance

        /// <summary>
        /// Clears the Sentinel linter cache.
        /// </summary>
        public static void ClearCache()
        {
            _sentinelCache.Clear();
            _sentinelCacheTime.Clear();
        }

        /// <summary>
        /// Returns all physical linter errors and runtime log entries associated with a GameObject.
        /// </summary>
        public static List<string> GetErrors(GameObject obj)
        {
            if (obj == null) return new List<string>();
            return GetSentinelData(obj).allErrors;
        }

        /// <summary>
        /// Returns whether the GameObject has active runtime errors/warnings captured by the console.
        /// </summary>
        public static bool HasRuntimeErrors(GameObject obj)
        {
            if (obj == null) return false;
            return GetSentinelData(obj).hasRuntimeErrors;
        }

        /// <summary>
        /// Check if any of the descendants of a GameObject have errors.
        /// Uses the 1-second cached data for maximum performance.
        /// </summary>
        public static bool HasErrorsInDescendants(GameObject obj, out bool hasRuntime)
        {
            hasRuntime = false;
            if (obj == null) return false;

            foreach (Transform child in obj.transform)
            {
                var errors = GetErrors(child.gameObject);
                if (errors.Count > 0)
                {
                    hasRuntime = HasRuntimeErrors(child.gameObject);
                    return true;
                }

                if (HasErrorsInDescendants(child.gameObject, out bool childRuntime))
                {
                    hasRuntime = childRuntime;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns all physical errors and runtime errors from descendants, formatted with GameObject names.
        /// </summary>
        public static List<string> GetDescendantErrorsFormatted(GameObject obj)
        {
            List<string> list = new List<string>();
            if (obj == null) return list;

            GetDescendantErrorsRecursive(obj, list);
            return list;
        }

        private static void GetDescendantErrorsRecursive(GameObject obj, List<string> list)
        {
            foreach (Transform child in obj.transform)
            {
                var errors = GetErrors(child.gameObject);
                foreach (var err in errors)
                {
                    list.Add($"[{child.gameObject.name}] {err}");
                }
                GetDescendantErrorsRecursive(child.gameObject, list);
            }
        }

        /// <summary>
        /// Checks if Portuguese language is currently active in the Smart Console Core via reflection.
        /// </summary>
        public static bool IsPortuguese()
        {
            InitializeReflection();
            if (_smartConsoleCoreType == null) return false;

            var langProp = _smartConsoleCoreType.GetProperty("Language", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (langProp != null)
            {
                var langVal = langProp.GetValue(null);
                if (langVal != null)
                {
                    return langVal.ToString() == "Portuguese";
                }
            }
            return false;
        }

        private static SentinelResult GetSentinelData(GameObject obj)
        {
            int id = obj.GetInstanceID();
            double currentTime = EditorApplication.timeSinceStartup;

            if (_sentinelCache.TryGetValue(id, out var cached) && _sentinelCacheTime.TryGetValue(id, out double cachedTime))
            {
                if (currentTime - cachedTime < CacheDuration)
                {
                    return cached;
                }
            }

            SentinelResult result = new SentinelResult();
            
            // 1. Physical health checks (Linter)
            List<string> physicalErrors = RunSentinelScan(obj);
            
            // 2. Runtime log diagnostics (Smart Console bridge)
            List<string> runtimeErrors = GetSmartConsoleLogs(obj, out bool hasRuntimeError);

            result.allErrors = new List<string>();
            result.allErrors.AddRange(physicalErrors);
            result.allErrors.AddRange(runtimeErrors);
            result.hasRuntimeErrors = hasRuntimeError;

            _sentinelCache[id] = result;
            _sentinelCacheTime[id] = currentTime;

            return result;
        }

        /// <summary>
        /// Performs deep physical structure analysis of a GameObject and its components.
        /// </summary>
        private static List<string> RunSentinelScan(GameObject obj)
        {
            List<string> errors = new List<string>();
            if (obj == null) return errors;

            bool isSelected = Selection.Contains(obj);

            Component[] components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                // Detect missing scripts
                if (comp == null)
                {
                    errors.Add("Missing Script: A MonoBehaviour script slot is empty/missing.");
                    continue;
                }

                // Check standard properties & broken asset links using serialized properties
                // ONLY DO THIS EXPENSIVE CHECK IF THE OBJECT IS SELECTED
                if (isSelected)
                {
                    try
                    {
                        SerializedObject so = new SerializedObject(comp);
                        SerializedProperty prop = so.GetIterator();
                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                // A non-zero instance ID with a null reference means a missing/destroyed asset link
                                if (prop.objectReferenceInstanceIDValue != 0 && prop.objectReferenceValue == null)
                                {
                                    errors.Add($"Missing Link: {comp.GetType().Name}.{prop.displayName} reference is broken.");
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Safe block for components that can't be parsed
                    }
                }

                // Special linter rules for standard components
                if (comp is Renderer renderer)
                {
                    var materials = renderer.sharedMaterials;
                    if (materials != null)
                    {
                        for (int m = 0; m < materials.Length; m++)
                        {
                            if (materials[m] == null)
                            {
                                errors.Add($"Renderer: Material slot [{m}] is empty or missing.");
                            }
                        }
                    }
                }
                else if (comp is AudioSource audioSource)
                {
                    if (audioSource.playOnAwake && audioSource.clip == null)
                    {
                        errors.Add("AudioSource: Play On Awake is enabled, but no AudioClip is assigned.");
                    }
                }
                else if (comp is Light light)
                {
                    if (light.intensity <= 0)
                    {
                        errors.Add("Light: Intensity is 0. Light will emit no actual illumination.");
                    }
                }
            }

            return errors;
        }

        private static System.Type _smartConsoleCoreType;
        private static System.Reflection.PropertyInfo _allLogsProp;
        private static bool _reflectionInitialized = false;

        private static System.Type _logEntryType;
        private static System.Reflection.PropertyInfo _rawMsgProp;
        private static System.Reflection.PropertyInfo _typeProp;
        private static System.Reflection.PropertyInfo _transProp;
        
        private static System.Type _translatedErrorType;
        private static System.Reflection.PropertyInfo _isTransProp;
        private static System.Reflection.PropertyInfo _titleProp;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            // Updated path to reflect the new .asmdef module "Nakemo.SmartConsole.Editor"
            _smartConsoleCoreType = System.Type.GetType("Nakemo.SmartConsole.SmartConsoleCore, Nakemo.SmartConsole.Editor");
            if (_smartConsoleCoreType == null)
            {
                // Fallback for projects that haven't compiled the asmdef yet or older versions
                _smartConsoleCoreType = System.Type.GetType("Nakemo.SmartConsole.SmartConsoleCore, Assembly-CSharp-Editor");
            }

            if (_smartConsoleCoreType != null)
            {
                _allLogsProp = _smartConsoleCoreType.GetProperty("AllLogs", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            }
        }

        /// <summary>
        /// Queries the Smart Console log system via a decoupled reflection bridge.
        /// </summary>
        private static List<string> GetSmartConsoleLogs(GameObject obj, out bool hasRuntimeError)
        {
            hasRuntimeError = false;
            List<string> logs = new List<string>();

            InitializeReflection();
            if (_smartConsoleCoreType == null || _allLogsProp == null) return logs;

            var allLogsValue = _allLogsProp.GetValue(null);
            if (allLogsValue is System.Collections.IEnumerable enumerable)
            {
                string searchName = obj.name;
                string searchToken1 = $"'{searchName}'";
                string searchToken2 = $"({searchName})";
                string searchToken3 = $"'{searchName}')";

                foreach (var item in enumerable)
                {
                    if (item == null) continue;

                    if (_logEntryType == null || _logEntryType != item.GetType())
                    {
                        _logEntryType = item.GetType();
                        _rawMsgProp = _logEntryType.GetProperty("RawMessage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        _typeProp = _logEntryType.GetProperty("Type", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        _transProp = _logEntryType.GetProperty("Translation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    }

                    if (_rawMsgProp == null || _typeProp == null) continue;

                    string rawMsg = _rawMsgProp.GetValue(item) as string ?? "";
                    LogType type = (LogType)_typeProp.GetValue(item);

                    // We only care about warnings, errors, and exceptions
                    if (type == LogType.Log) continue;

                    bool match = rawMsg.Contains(searchToken1) || 
                                 rawMsg.Contains(searchToken2) || 
                                 rawMsg.Contains(searchToken3) ||
                                 (searchName.Length > 2 && rawMsg.Contains(searchName));

                    if (match)
                    {
                        // Check if it is a severe exception or error
                        bool isError = (type == LogType.Error || type == LogType.Exception || type == LogType.Assert);
                        if (isError)
                        {
                            hasRuntimeError = true;
                        }

                        // Try to get simplified translation
                        string title = rawMsg.Split('\n')[0];
                        if (_transProp != null)
                        {
                            var translation = _transProp.GetValue(item);
                            if (translation != null)
                            {
                                if (_translatedErrorType == null || _translatedErrorType != translation.GetType())
                                {
                                    _translatedErrorType = translation.GetType();
                                    _isTransProp = _translatedErrorType.GetProperty("IsTranslated", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    _titleProp = _translatedErrorType.GetProperty("FriendlyTitle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                }

                                if (_isTransProp != null && (bool)_isTransProp.GetValue(translation))
                                {
                                    if (_titleProp != null)
                                    {
                                        title = _titleProp.GetValue(translation) as string ?? title;
                                    }
                                }
                            }
                        }

                        logs.Add($"[Runtime] {title}");
                    }
                }
            }

            return logs;
        }
    }
}
