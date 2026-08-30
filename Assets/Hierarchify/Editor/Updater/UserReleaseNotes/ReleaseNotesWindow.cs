using UnityEngine;
using UnityEditor;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// A premium popup EditorWindow that displays the latest updates and release notes
    /// loaded dynamically from the external HierarchifyReleaseNotes.txt file.
    /// Features a sleek, modern UI matching Unity's design language.
    /// </summary>
    public class ReleaseNotesWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _releaseNotesText = "";
        private int _activeTab = 0;

        /// <summary>
        /// Opens the Release Notes popup window.
        /// </summary>
        public static void Open()
        {
            ReleaseNotesWindow window = GetWindow<ReleaseNotesWindow>(true, "Hierarchify Update", true);
            window.minSize = new Vector2(560, 550);
            window.maxSize = new Vector2(560, 550);
            window.ShowUtility();
            window.LoadReleaseNotes();
        }

        private void OnEnable()
        {
            LoadReleaseNotes();
        }

        private void LoadReleaseNotes()
        {
            string[] guids = AssetDatabase.FindAssets("HierarchifyReleaseNotes t:TextAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                TextAsset txtAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (txtAsset != null)
                {
                    _releaseNotesText = txtAsset.text;
                }
            }

            if (string.IsNullOrEmpty(_releaseNotesText))
            {
                _releaseNotesText = "<b><size=16><color=#2E7DF5>HIERARCHIFY ATUALIZADO</color></size></b>\n\nNenhum arquivo de notas de lançamento encontrado.";
            }
        }

        private void OnGUI()
        {
            Event evt = Event.current;

            // Background & Border colors based on Unity Theme
            bool isDark = EditorGUIUtility.isProSkin;
            Color bgColor = isDark ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.94f, 0.94f, 0.94f, 1f);
            Color headerColor = isDark ? new Color(0.14f, 0.14f, 0.14f, 1f) : new Color(0.88f, 0.88f, 0.88f, 1f);
            Color borderColor = isDark ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);

            // Draw full background
            Rect fullRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(fullRect, bgColor);

            // Draw Top Header
            Rect headerRect = new Rect(0, 0, position.width, 76f);
            EditorGUI.DrawRect(headerRect, headerColor);
            EditorGUI.DrawRect(new Rect(0, 75f, position.width, 1f), borderColor);

            // Header Content: Icon & Titles
            Texture2D infoIcon = EditorGUIUtility.IconContent("d_console.infoicon").image as Texture2D;
            if (infoIcon != null)
            {
                GUI.DrawTexture(new Rect(20f, 18f, 40f, 40f), infoIcon, ScaleMode.ScaleToFit);
            }

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.18f, 0.49f, 0.96f, 1f) }
            };
            GUI.Label(new Rect(72f, 16f, position.width - 92f, 22f), "HIERARCHIFY ATUALIZADO!", titleStyle);

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                normal = { textColor = isDark ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.4f, 0.4f, 0.4f) }
            };
            GUI.Label(new Rect(72f, 38f, position.width - 92f, 18f), "Sua experiência de organização foi aprimorada com sucesso.", subtitleStyle);

            // Tab bar (Novidades & Comunidade, Patch Notes)
            float tabY = 76f;
            float tabHeight = 30f;
            Rect tabRect = new Rect(0, tabY, position.width, tabHeight);
            EditorGUI.DrawRect(tabRect, isDark ? new Color(0.16f, 0.16f, 0.16f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f));
            EditorGUI.DrawRect(new Rect(0, tabY + tabHeight - 1f, position.width, 1f), borderColor);

            GUILayout.BeginArea(new Rect(10f, tabY + 2f, position.width - 20f, tabHeight - 4f));
            string[] tabs = new string[] { "Novidades & Comunidade", "Patch Notes" };
            _activeTab = GUILayout.Toolbar(_activeTab, tabs, GUILayout.Height(24));
            GUILayout.EndArea();

            // Main Scrollable Area
            float contentY = tabY + tabHeight + 10f;
            float contentHeight = position.height - contentY - 64f; // Top header + tabs + bottom footer
            Rect contentRect = new Rect(20f, contentY, position.width - 40f, contentHeight);

            GUILayout.BeginArea(contentRect);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUIStyle bodyStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 12,
                normal = { textColor = isDark ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.2f, 0.2f, 0.2f) }
            };
            bodyStyle.margin = new RectOffset(0, 0, 4, 8);

            if (_activeTab == 0)
            {
                DrawWelcomeTab(bodyStyle, borderColor);
            }
            else
            {
                GUILayout.Label(_releaseNotesText, bodyStyle);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // Bottom Footer
            float footerY = position.height - 64f;
            EditorGUI.DrawRect(new Rect(0, footerY, position.width, 1f), borderColor);
            EditorGUI.DrawRect(new Rect(0, footerY + 1f, position.width, 63f), headerColor);

            // Premium Rounded Action Button
            Rect btnRect = new Rect((position.width - 180f) / 2f, footerY + 18f, 180f, 28f);
            bool isBtnHover = btnRect.Contains(evt.mousePosition);

            // Draw button background with hover states
            Color btnColor = new Color(0.18f, 0.49f, 0.96f, 1f); // Blue
            if (isBtnHover)
            {
                btnColor = new Color(0.25f, 0.55f, 1f, 1f); // Lighter blue
                Repaint(); // Repaint instantly for smooth hover
            }
            EditorGUI.DrawRect(btnRect, btnColor);

            // Draw button outline
            DrawRectOutline(btnRect, new Color(0.18f, 0.49f, 0.96f, 0.6f));

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = null, textColor = Color.white },
                hover = { background = null, textColor = Color.white },
                active = { background = null, textColor = Color.white },
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            if (GUI.Button(btnRect, "Perfeito, Vamos Lá!", btnStyle))
            {
                Close();
            }

            // Window border outline
            DrawRectOutline(fullRect, borderColor);
        }

        private void DrawWelcomeTab(GUIStyle bodyStyle, Color borderColor)
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("<b><size=15><color=#2E7DF5>Obrigado por fazer parte da nossa jornada! 🚀</color></size></b>", new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true });
            EditorGUILayout.Space(8);
            
            GUILayout.Label(
                "Temos o orgulho de anunciar que implementamos uma importante <b>Atualização de Segurança</b> em nossos servidores de atualização, blindando as chaves de licença e protegendo a integridade de todos os nossos usuários.\n\n" +
                "Com esta consolidação, o Hierarchify entra oficialmente em <b>Fase Beta com autorização de divulgação</b>! Você está livre para compartilhar vídeos, screenshots, postagens e falar sobre a ferramenta com sua comunidade.\n\n" +
                "Como forma de agradecimento pelo seu apoio constante e para celebrar essa nova fase:",
                bodyStyle
            );

            EditorGUILayout.Space(10);

            // Discount highlight card (Safe rendering using BeginVertical layout rect)
            Rect boxRect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                Color cardBgColor = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.28f, 0.18f, 0.4f) : new Color(0.85f, 0.95f, 0.85f, 1f);
                Color cardBorderColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.6f, 0.3f, 0.5f) : new Color(0.3f, 0.7f, 0.3f, 0.5f);
                EditorGUI.DrawRect(boxRect, cardBgColor);
                DrawRectOutline(boxRect, cardBorderColor);
            }

            // Internal padding
            GUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            GUILayout.BeginVertical();
            GUILayout.Space(10f);

            GUIStyle cardTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                richText = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.1f, 0.5f, 0.2f) }
            };
            GUILayout.Label("🎉 CUPOM DE 5% DE DESCONTO EXCLUSIVO", cardTitleStyle);
            EditorGUILayout.Space(4f);

            GUIStyle cardBodyStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.9f, 0.8f) : new Color(0.15f, 0.3f, 0.15f) }
            };
            GUILayout.Label("Entre em contato direto conosco para resgatar o seu código de desconto especial e convidar seus amigos e conhecidos para conhecer o Hierarchify!", cardBodyStyle);

            GUILayout.Space(10f);
            GUILayout.EndVertical();
            GUILayout.Space(12f);
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(14);

            GUILayout.Label("<b>Como entrar em contato:</b>\n" +
                            "• Discord Oficial da Nakemo\n" +
                            "• E-mail de suporte: <b>contactnakemo@gmail.com</b>", bodyStyle);

            GUILayout.EndVertical();
        }

        private static void DrawRectOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color); // Top
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color); // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color); // Right
        }
    }
}
