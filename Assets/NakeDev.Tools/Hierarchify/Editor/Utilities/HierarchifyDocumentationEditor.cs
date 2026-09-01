using UnityEngine;
using UnityEditor;

namespace NakeDev.EditorTools
{
    /// <summary>
    /// Custom Inspector Editor for the HierarchifyDocumentation ScriptableObject.
    /// Provides a premium, styled, and tabbed documentation experience directly in the Unity Inspector.
    /// </summary>
    [CustomEditor(typeof(HierarchifyDocumentation))]
    public class HierarchifyDocumentationEditor : UnityEditor.Editor
    {
        private int _activeTab = 0;
        private Vector2 _scrollPos;

        // Styling cache
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _shortcutStyle;
        private GUIStyle _accentBoxStyle;
        private GUIStyle _tabButtonStyle;
        private bool _stylesInitialized = false;

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.64f, 1f) : new Color(0f, 0.4f, 0.8f);

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 10, 5)
            };
            _titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            _subHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 10)
            };

            _bodyStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11,
                richText = true
            };

            _cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(2, 2, 5, 5)
            };

            _shortcutStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(0, 0, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            _shortcutStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.75f, 0.2f) : new Color(0.7f, 0.4f, 0f);

            _accentBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(2, 2, 5, 5)
            };

            _tabButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                fixedHeight = 25
            };

            _stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();

            // Header Section
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Label("HIERARCHIFY", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("v1.1.0", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            GUILayout.Label("Manual de Organização e Produtividade para Unity", _subHeaderStyle);

            // Divider
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            // Tabs Selector
            string[] tabNames = new string[] { "Início", "Pastas & Visual", "Sentinel & Ícones", "Recursos & Testes" };
            _activeTab = GUILayout.Toolbar(_activeTab, tabNames, GUILayout.Height(26));
            EditorGUILayout.Space();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(600));

            switch (_activeTab)
            {
                case 0:
                    DrawTabGeneral();
                    break;
                case 1:
                    DrawTabFolders();
                    break;
                case 2:
                    DrawTabSentinel();
                    break;
                case 3:
                    DrawTabActions();
                    break;
            }

            EditorGUILayout.EndScrollView();

            // Footer Quick Actions
            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Abrir Control Center", GUILayout.Height(30)))
            {
                HierarchifyWindow.Open(null, 0);
            }
            if (GUILayout.Button("Estrutura Padrão", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Hierarchify", "Deseja criar a estrutura padrão de pastas organizadoras na cena atual?", "Sim", "Não"))
                {
                    HierarchifyWindow.Open(null, 0); // Opens presets tab
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawTabGeneral()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Bem-vindo ao Hierarchify!", _titleStyle);
            GUILayout.Label(
                "O Hierarchify é um conjunto de ferramentas completo para a janela <b>Hierarchy</b> da Unity. Ele ajuda você a manter a estrutura de suas cenas impecável, proativa e livre de erros.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.Label("Primeiros Passos", _titleStyle);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "1. <b>Crie a Estrutura Base:</b> Clique no botão <b>'Estrutura Padrão'</b> abaixo para criar as pastas organizadoras fundamentais na sua cena (Management, Environment, Gameplay, etc.).\n\n" +
                "2. <b>Organize Seus Objetos:</b> Arraste e solte seus GameObjects sob a pasta correspondente. O Hierarchify criará automaticamente linhas guia visuais e uma sutil cor de fundo para agrupar visualmente o conteúdo.\n\n" +
                "3. <b>Configure ao seu Gosto:</b> Abra o <b>Control Center</b> (Ctrl + Alt + H) para personalizar as linhas guia, visibilidade dos ícones de componentes, tamanho da fonte e muito mais.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.Label("Teclas de Atalho & Cliques Rápidos", _titleStyle);

            GUILayout.BeginVertical(_accentBoxStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ctrl + Alt + H", _shortcutStyle, GUILayout.Width(95));
            GUILayout.Label("<b>Control Center:</b> Abre a janela central de configurações globais e predefinições do Hierarchify.", _bodyStyle);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ctrl + Alt + C", _shortcutStyle, GUILayout.Width(95));
            GUILayout.Label("<b>Troca de Cor:</b> Abre o seletor de cores flutuante para a pasta organizadora selecionada.", _bodyStyle);
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ctrl + Clique", _shortcutStyle, GUILayout.Width(95));
            GUILayout.Label("<b>Menu de Componentes:</b> Segure Ctrl e clique em qualquer parte da linha de um objeto para ver a lista flutuante de componentes com ícones.", _bodyStyle);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Clique no '...'", _shortcutStyle, GUILayout.Width(95));
            GUILayout.Label("<b>Overflow de Componentes:</b> Quando um objeto tem mais de 4 componentes, clique nas reticências para abrir a mesma lista flutuante.", _bodyStyle);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Alt + Clique", _shortcutStyle, GUILayout.Width(95));
            GUILayout.Label("<b>Atalho do Inspector:</b> Clique na linha de um GameObject (fora da zona de componentes) segurando Alt para abrir o painel de ícones rápidos.", _bodyStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawTabFolders()
        {
            // Title with folder icon
            Texture2D folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (folderIcon != null) GUILayout.Label(new GUIContent(folderIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Pastas Organizadoras (Separadores)", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "<b>Como Criar:</b>\n" +
                "Qualquer GameObject nomeado com o padrão <color=#ffaa00>--- SEPARADOR #HEX ---</color> se torna automaticamente uma pasta organizadora.\n\n" +
                "• O <b>#HEX</b> no final (ex: #FF0000) define a cor de acentuação do fundo e da barra lateral.\n" +
                "• <b>Personalização de Cor:</b> Selecione a pasta e pressione <b>Ctrl + Alt + C</b> para escolher uma cor do nosso seletor interativo.\n" +
                "• <b>Aparência Flexível:</b> A pasta pode colorir toda a linha com um gradiente suave ou mostrar apenas uma barra vertical de 4px na lateral esquerda. Altere essa opção na aba <i>Settings</i>.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Title with tree lines visual representation icon
            Texture2D hierarchyIcon = EditorGUIUtility.IconContent("d_TreeView").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (hierarchyIcon != null) GUILayout.Label(new GUIContent(hierarchyIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Linhas de Relação (Guidelines)", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "O Hierarchify desenha linhas tracejadas conectando GameObjects pais e filhos, ajudando a visualizar a estrutura tridimensional da cena rapidamente.\n\n" +
                "<b>Realce de Seleção Ativa:</b>\n" +
                "Ao selecionar um GameObject que está no fundo de uma árvore complexa, todo o caminho dele até a pasta raiz é destacado com uma linha sólida na cor acentuada da pasta, tornando a localização visual imediata.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Title with UI representation icon
            Texture2D canvasIcon = EditorGUIUtility.IconContent("Canvas Icon").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (canvasIcon != null) GUILayout.Label(new GUIContent(canvasIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Smart Folders (Auto-UI Healing)", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "Em projetos Unity, criar objetos vazios normais dentro de um <b>Canvas</b> de UI pode quebrar o layout dos elementos filhos.\n\n" +
                "O Hierarchify possui um sistema inteligente de correção de layout: se você arrastar uma pasta organizadora para dentro de um Canvas, ou colocar elementos de interface como filhos dela, o sistema <b>automaticamente injeta e configura um componente RectTransform</b> na pasta. Isso preserva as âncoras e o posicionamento correto da sua UI, sem quebras ou perda de referências.",
                _bodyStyle
            );
            GUILayout.EndVertical();
        }

        private void DrawTabSentinel()
        {
            GUILayout.Label("Hierarchy Sentinel (Linter e Diagnósticos)", _titleStyle);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "O Sentinel monitora constantemente a cena atual, sinalizando em tempo real pequenos descuidos que podem gerar erros em tempo de execução ou quebrar builds:\n\n" +
                "• <b>Monobehaviours Ausentes (Missing Scripts):</b> Slots de scripts vazios que geram avisos clássicos da Unity.\n" +
                "• <b>Referências Quebradas (Missing References):</b> Campos expostos no Inspector que perderam seus links de assets.\n" +
                "• <b>Renderizadores com Erro:</b> Mesh Renderers ativos sem material ou com slots de material vazios.\n" +
                "• <b>Áudios Incompletos:</b> Audio Sources configurados como 'Play On Awake' mas sem clipe de som designado.\n" +
                "• <b>Luzes Inativas de Fato:</b> Fontes de luz ativas mas com intensidade de iluminação definida como zero (0).\n\n" +
                "<b>Alerta Visual:</b>\n" +
                "Ícones de alerta (amarelo para aviso, vermelho para erro grave) aparecem à direita da linha do GameObject. <b>Basta clicar no ícone de alerta</b> para abrir uma tela de diagnóstico que descreve exatamente o problema e indica como corrigi-lo.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.Label("Ícones de Componentes", _titleStyle);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "Exibe pequenos ícones no canto direito representando cada componente do GameObject, servindo como uma pré-visualização rápida e passiva.\n\n" +
                "<b>Modos de Exibição (Configuráveis em Settings):</b>\n" +
                "• <b>Always:</b> Sempre exibe os ícones de todos os GameObjects.\n" +
                "• <b>On Hover (Recomendado):</b> Mantém a hierarquia limpa e exibe os ícones apenas na linha sob o ponteiro do mouse.\n" +
                "• <b>On Ctrl Pressed:</b> Mostra os ícones apenas quando a tecla Ctrl estiver pressionada.\n\n" +
                "<b>Mini-Map Inspector Hub:</b>\n" +
                "Dando Ctrl + Clique na linha do GameObject (ou clicando no ícone `...` de overflow), abre-se a lista de componentes. Clique em qualquer componente dessa lista para abrir uma **Mini-Map Window** flutuante dedicada, permitindo alterar propriedades daquele componente em tempo real de forma isolada. É possível manter múltiplos inspetores flutuantes abertos simultaneamente.",
                _bodyStyle
            );
            GUILayout.EndVertical();
        }

        private void DrawTabActions()
        {
            // NEW FEATURE: Barra do Inspector (Em Fase De Teste)
            Texture2D bookmarkIcon = EditorGUIUtility.IconContent("Favorite Icon").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (bookmarkIcon != null) GUILayout.Label(new GUIContent(bookmarkIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Barra do Inspector (Bookmarks & Navegação) <color=#e67e22>(Em Fase De Teste)</color>", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "O Hierarchify introduz uma barra de favoritos e navegação histórica no topo da janela do Inspector:\n\n" +
                "• <b>Fixar Favoritos (Bookmarks):</b> Basta <b>arrastar e soltar (Drag & Drop)</b> qualquer GameObject da Hierarchy para cima da barra no topo do Inspector.\n" +
                "• <b>Navegação Histórica:</b> Use as setas '<' e '>' para navegar facilmente pelo histórico recente de objetos selecionados no editor.\n" +
                "• <b>Persistência Inteligente:</b> Seus favoritos são salvos de forma independente para cada cena e são restaurados automaticamente ao alternar de cena.\n" +
                "• <b>Gerenciamento Rápido:</b> Clique com o <b>botão direito</b> sobre qualquer ícone de favorito na barra para abrir o menu de contexto (opções para mover para os lados, focar/ping ou remover o bookmark).",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Title for dynamic filters (Fase de Teste)
            Texture2D filterIcon = EditorGUIUtility.IconContent("Search Icon").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (filterIcon != null) GUILayout.Label(new GUIContent(filterIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Filtros Rápidos Dinâmicos <color=#3498db>(Em Fase De Teste)</color>", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "Uma ferramenta inteligente de busca e isolamento de componentes na aba <b>Filters</b> do Control Center:\n\n" +
                "• <b>Mapeamento em Tempo Real:</b> O sistema varre a cena ativamente e agrupa todos os componentes por categoria (ex: Unity Nativo, Modular Avatar, VRChat SDK, Scripts Customizados, etc.).\n" +
                "• <b>Esmaecimento Visual (Dimming):</b> Ao selecionar e ativar o filtro de um componente, todos os GameObjects da cena que não o contêm são esmaecidos para 15% de opacidade. Isso permite focar visualmente apenas nos objetos importantes sem quebrar a árvore estrutural da hierarquia.\n" +
                "• <b>Auto-Limpeza de Segurança:</b> O filtro é desativado automaticamente ao fechar o Control Center para evitar que a sua Hierarchy permaneça esmaecida acidentalmente.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Title for Safe Delete
            Texture2D deleteIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (deleteIcon != null) GUILayout.Label(new GUIContent(deleteIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Fluxo de Exclusão Segura (Safe Delete)", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "Ao tentar deletar uma pasta organizadora que possui GameObjects filhos, o Hierarchify intercepta a tecla Delete e exibe uma caixa de diálogo segura para evitar perda acidental de trabalho:\n\n" +
                "• <b>Extract & Save Children:</b> Remove o parentesco de todos os objetos filhos e os move com segurança para a raiz (ou para o pai da pasta deletada) antes de remover a pasta organizadora.\n" +
                "• <b>Delete All:</b> Apaga a pasta e todos os seus filhos recursivamente.\n" +
                "• <b>Cancel:</b> Aborta a operação de exclusão.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Title for Scene Selector
            Texture2D sceneIcon = EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (sceneIcon != null) GUILayout.Label(new GUIContent(sceneIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Seletor Rápido de Cenas (Scene Selector)", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "A seta <b>▶</b> renderizada ao lado do nome da cena atual na hierarquia abre um menu de atalho para gerenciamento de cenas do projeto:\n\n" +
                "• <b>Fixar Cenas:</b> Favorita cenas acessadas com frequência para que elas fiquem fixadas no topo da lista do Scene Selector.\n" +
                "• <b>Alternância Rápida:</b> Carregue ou adicione cenas de forma aditiva ao projeto instantaneamente com um clique simples.",
                _bodyStyle
            );
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            // Title for Performance
            Texture2D speedIcon = EditorGUIUtility.IconContent("d_Profiler.CPU").image as Texture2D;
            GUILayout.BeginHorizontal();
            if (speedIcon != null) GUILayout.Label(new GUIContent(speedIcon), GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label("Otimização de Performance", _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(
                "O Hierarchify foi desenvolvido com foco total em performance no editor:\n\n" +
                "• <b>Verificação por Sessão:</b> O linter de integridade e checagens de atualizações rodam em cache, evitando acessos constantes ao disco ou requisições desnecessárias a cada compilação de script.\n" +
                "• <b>Desenho Otimizado:</b> Todas as renderizações usam estruturas de cache para que a taxa de quadros (FPS) da Unity permaneça estável mesmo em cenas com milhares de GameObjects.",
                _bodyStyle
            );
            GUILayout.EndVertical();
        }
    }
}
