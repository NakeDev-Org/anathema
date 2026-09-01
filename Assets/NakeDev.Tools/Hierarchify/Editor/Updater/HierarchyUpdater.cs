using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Handles secure license verification with Gumroad and downloads updates
    /// from the private GitHub repository via a Cloudflare Worker proxy.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyUpdater
    {
        private static string GetWorkerUrl()
        {
            try
            {
                byte[] data = System.Convert.FromBase64String("aHR0cHM6Ly9oaWVyYXJjaGlmeS11cGRhdGVyLm1hdGhldXMtbmFrYXRpLWIyLWNmMC53b3JrZXJzLmRldg==");
                return System.Text.Encoding.UTF8.GetString(data);
            }
            catch
            {
                return "";
            }
        }
        
        // --- RELEASE VERSION CONFIGURATION ---
        private const string CurrentVersion = "1.1.0";
        private const string PrefsLicenseKey = "Hierarchify_LicenseKey";
        private const string PrefsLastCheckTime = "Hierarchify_LastUpdateCheckTime";

        static HierarchyUpdater()
        {
            Initialize();
        }

        private static string _licenseKey = "";
        private static string _statusMessage = "Ready.";
        private static bool _checking = false;
        private static bool _updateAvailable = false;
        private static string _latestVersion = "";
        private static string _downloadProgress = "";
        private static bool _editingLicense = false;

        private static string LoadLicenseKey()
        {
            string projectSettingsPath = "ProjectSettings/HierarchifyLicense.txt";
            if (File.Exists(projectSettingsPath))
            {
                try
                {
                    return File.ReadAllText(projectSettingsPath).Trim();
                }
                catch { }
            }

            return EditorPrefs.GetString(PrefsLicenseKey, "");
        }

        private static void SaveLicenseKey(string key)
        {
            _licenseKey = key.Trim();
            EditorPrefs.SetString(PrefsLicenseKey, _licenseKey);

            string projectSettingsPath = "ProjectSettings/HierarchifyLicense.txt";
            try
            {
                File.WriteAllText(projectSettingsPath, _licenseKey);
            }
            catch { }
        }

        public static void Initialize()
        {
            _licenseKey = LoadLicenseKey();

            // Auto-lock: If loaded from EditorPrefs but the project file doesn't exist, save it to the project file now!
            string projectSettingsPath = "ProjectSettings/HierarchifyLicense.txt";
            if (!string.IsNullOrEmpty(_licenseKey) && !File.Exists(projectSettingsPath))
            {
                SaveLicenseKey(_licenseKey);
            }

            // Popup automático de Release Notes + checagem silenciosa de update (Cloudflare
            // Worker) desativados de propósito — não queremos mais popup nenhum toda vez que
            // o projeto abre. Checagem manual continua disponível via "Check for Updates" na
            // aba de Settings do Hierarchify (DrawUpdaterGUI/CheckForUpdates), sem tocar nisso.
            EditorPrefs.SetString("Hierarchify_LastInstalledVersion", CurrentVersion);
        }

        private static void CheckForUpdatesSilent()
        {
            string url = $"{GetWorkerUrl()}/check";
            string jsonPayload = $"{{\"license\":\"{_licenseKey}\",\"version\":\"{CurrentVersion}\"}}";

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();

            EditorApplication.CallbackFunction updateCheck = null;
            updateCheck = () =>
            {
                if (asyncOp.isDone)
                {
                    EditorApplication.update -= updateCheck;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var data = JsonUtility.FromJson<CheckResponse>(request.downloadHandler.text);
                            if (data != null && data.success)
                            {
                                // Save that we successfully completed a check
                                EditorPrefs.SetString(PrefsLastCheckTime, System.DateTime.UtcNow.ToString());

                                if (data.updateAvailable)
                                {
                                    bool isPtBr = HierarchySentinel.IsPortuguese();
                                    string title = isPtBr ? "Atualização Disponível" : "Update Available";
                                    string message = isPtBr 
                                        ? $"Uma nova versão ({data.latestVersion}) do Hierarchify está disponível!\nDeseja abrir as configurações para atualizar?"
                                        : $"A new version ({data.latestVersion}) of Hierarchify is available!\nWould you like to open the settings to update?";
                                    string ok = isPtBr ? "Sim" : "Yes";
                                    string cancel = isPtBr ? "Não" : "No";

                                    if (EditorUtility.DisplayDialog(title, message, ok, cancel))
                                    {
                                        HierarchifyWindow.Open(null, 4);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore parsing errors silently for automatic background checks
                        }
                    }
                    request.Dispose();
                }
            };
            EditorApplication.update += updateCheck;
        }

        /// <summary>
        /// Renders the updater interface. Called from HierarchifyWindow settings tab.
        /// </summary>
        public static void DrawUpdaterGUI()
        {
            if (string.IsNullOrEmpty(_licenseKey))
            {
                _licenseKey = LoadLicenseKey();
            }

            EditorGUILayout.Space(12);
            GUILayout.Label("Product Updates & Licensing", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(6);

            bool hasKey = !string.IsNullOrEmpty(_licenseKey);

            if (hasKey && !_editingLicense)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Status:", GUILayout.Width(120));
                Color oldColor = GUI.contentColor;
                GUI.contentColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.1f, 0.6f, 0.2f);
                GUILayout.Label("✓ Licença Vinculada", EditorStyles.boldLabel);
                GUI.contentColor = oldColor;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Chave de Licença:", GUILayout.Width(120));
                string maskedKey = _licenseKey.Length > 8 
                    ? $"{_licenseKey.Substring(0, 4)} - •••• - •••• - •••• - {_licenseKey.Substring(_licenseKey.Length - 4)}" 
                    : "••••••••••••••••";
                GUILayout.Label(maskedKey, EditorStyles.boldLabel);
                
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Alterar Chave", GUILayout.Width(100), GUILayout.Height(18)))
                {
                    _editingLicense = true;
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                _licenseKey = EditorGUILayout.TextField("Gumroad License Key", _licenseKey);
                if (EditorGUI.EndChangeCheck())
                {
                    SaveLicenseKey(_licenseKey);
                }

                if (hasKey)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Confirmar", GUILayout.Width(100), GUILayout.Height(18)))
                    {
                        _editingLicense = false;
                        GUI.FocusControl(null);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(6);

            // Version and Actions
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Installed Version: {CurrentVersion}", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_checking || string.IsNullOrEmpty(_licenseKey)))
            {
                if (GUILayout.Button("Check for Updates", GUILayout.Width(130), GUILayout.Height(18)))
                {
                    CheckForUpdates();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Status feedback panel
            if (_checking || _updateAvailable || _statusMessage != "Ready.")
            {
                Color originalColor = GUI.color;
                if (_updateAvailable)
                {
                    GUI.color = new Color(0.85f, 0.95f, 0.85f, 1f); // Soft green tint
                }
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.color = originalColor;

                GUILayout.Label($"Status: {_statusMessage}", EditorStyles.wordWrappedMiniLabel);
                
                if (!string.IsNullOrEmpty(_latestVersion))
                {
                    GUILayout.Label($"Latest Available Version: {_latestVersion}", EditorStyles.miniBoldLabel);
                }
                
                if (!string.IsNullOrEmpty(_downloadProgress))
                {
                    // Draw progress bar
                    Rect rect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                    float progress = 0f;
                    float.TryParse(_downloadProgress.Replace("%", ""), out progress);
                    progress /= 100f;
                    EditorGUI.ProgressBar(rect, progress, $"Downloading: {_downloadProgress}");
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }

            // Download & Install Action
            if (_updateAvailable && !_checking)
            {
                Color originalBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.18f, 0.49f, 0.96f, 1f); // Accent blue button
                
                if (GUILayout.Button("Download & Install Update", GUILayout.Height(28)))
                {
                    DownloadAndInstall();
                }
                GUI.backgroundColor = originalBg;
                EditorGUILayout.Space(6);
            }

            EditorGUILayout.EndVertical();
        }

        private static void CheckForUpdates()
        {
            _checking = true;
            _statusMessage = "Verifying license and checking release database...";
            _latestVersion = "";
            _updateAvailable = false;
            RepaintParent();

            string url = $"{GetWorkerUrl()}/check";
            string jsonPayload = $"{{\"license\":\"{_licenseKey}\",\"version\":\"{CurrentVersion}\"}}";

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();

            EditorApplication.CallbackFunction updateCheck = null;
            updateCheck = () =>
            {
                if (asyncOp.isDone)
                {
                    EditorApplication.update -= updateCheck;
                    _checking = false;
                    ProcessCheckResponse(request);
                    request.Dispose();
                    RepaintParent();
                }
            };
            EditorApplication.update += updateCheck;
        }

        private static void ProcessCheckResponse(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 403)
                {
                    _statusMessage = "Verification failed: Invalid license key.";
                }
                else
                {
                    _statusMessage = $"Error: Connection failed ({request.responseCode}).";
                }
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<CheckResponse>(request.downloadHandler.text);
                if (data != null && data.success)
                {
                    _latestVersion = data.latestVersion;
                    _updateAvailable = data.updateAvailable;
                    _statusMessage = data.message;
                }
                else
                {
                    _statusMessage = "Could not parse database status.";
                }
            }
            catch
            {
                _statusMessage = "Error parsing response from verification server.";
            }
        }

        private static void DownloadAndInstall()
        {
            _checking = true;
            _statusMessage = "Downloading package...";
            _downloadProgress = "0%";
            RepaintParent();

            string url = $"{GetWorkerUrl()}/download";
            string jsonPayload = $"{{\"license\":\"{_licenseKey}\"}}";

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);

            string tempFolder = Path.Combine(Application.dataPath, "../Temp");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            string tempFilePath = Path.Combine(tempFolder, "update.unitypackage");

            request.downloadHandler = new DownloadHandlerFile(tempFilePath);
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();

            EditorApplication.CallbackFunction downloadCheck = null;
            downloadCheck = () =>
            {
                if (!asyncOp.isDone)
                {
                    _downloadProgress = $"{(asyncOp.progress * 100f):0}%";
                    RepaintParent();
                }
                else
                {
                    EditorApplication.update -= downloadCheck;
                    _checking = false;
                    _downloadProgress = "";

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        _statusMessage = $"Download failed. Code {request.responseCode}";
                    }
                    else
                    {
                        _statusMessage = "Download completed! Importing package...";
                        RepaintParent();

                        AssetDatabase.ImportPackage(tempFilePath, true);
                        try
                        {
                            if (File.Exists(tempFilePath))
                            {
                                File.Delete(tempFilePath);
                            }
                        }
                        catch { }
                    }
                    request.Dispose();
                    RepaintParent();
                }
            };
            EditorApplication.update += downloadCheck;
        }

        private static void RepaintParent()
        {
            var windows = Resources.FindObjectsOfTypeAll<HierarchifyWindow>();
            foreach (var win in windows)
            {
                if (win != null)
                {
                    win.Repaint();
                }
            }
        }

        [System.Serializable]
        private class CheckResponse
        {
            public bool success;
            public string latestVersion;
            public bool updateAvailable;
            public string message;
        }
    }
}
