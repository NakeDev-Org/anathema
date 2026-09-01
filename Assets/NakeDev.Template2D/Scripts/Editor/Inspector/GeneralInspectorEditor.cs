using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NakeDev.Attributes;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NakeDev.Editor.Inspector
{
    /// <summary>
    /// Editor de fallback global (Regra 8-like: nenhum script precisa herdar nada nem ganhar
    /// um Editor customizado próprio para se beneficiar disso). "isFallback = true" garante
    /// que, se algum script já tiver um [CustomEditor] específico, aquele continua tendo
    /// prioridade — isso aqui só entra quando não existe um editor mais específico.
    ///
    /// O que entrega automaticamente pra QUALQUER MonoBehaviour do projeto:
    /// - Arrays/List&lt;T&gt; desenhados como ReorderableList (arrastar pra reordenar) em vez do
    ///   array padrão feio da Unity.
    /// - Métodos marcados com [Button] viram botões clicáveis no fim do Inspector.
    /// - [InspectorLine] e [ReadOnly] em campos funcionam automaticamente via PropertyDrawer/
    ///   DecoratorDrawer (ver InspectorLineDrawer.cs / ReadOnlyDrawer.cs) — não dependem
    ///   desta classe, funcionam mesmo sem ela.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class GeneralMonoBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GeneralInspectorDrawer.DrawDefaultWithLists(serializedObject, _lists);
            GeneralInspectorDrawer.DrawButtons(target, serializedObject.targetObjects);
        }

        private readonly Dictionary<string, ReorderableList> _lists = new();
    }

    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class GeneralScriptableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GeneralInspectorDrawer.DrawDefaultWithLists(serializedObject, _lists);
            GeneralInspectorDrawer.DrawButtons(target, serializedObject.targetObjects);
        }

        private readonly Dictionary<string, ReorderableList> _lists = new();
    }

    /// <summary>
    /// Lógica compartilhada entre os dois editores de fallback acima — evita duplicar o loop
    /// de desenho e a busca por [Button] entre MonoBehaviour e ScriptableObject.
    /// </summary>
    internal static class GeneralInspectorDrawer
    {
        public static void DrawDefaultWithLists(SerializedObject serializedObject, Dictionary<string, ReorderableList> listCache)
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            // Campo "Script" no topo, desabilitado — mesmo comportamento do inspector padrão.
            if (iterator.NextVisible(enterChildren) && iterator.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(iterator, true);
            }

            enterChildren = false;
            while (iterator.NextVisible(enterChildren))
            {
                bool isReorderableArray = iterator.isArray
                    && iterator.propertyType == SerializedPropertyType.Generic
                    && iterator.arrayElementType != "char"; // exclui string (string também reporta isArray)

                if (isReorderableArray)
                {
                    EditorGUILayout.Space(4f);
                    GetOrCreateList(serializedObject, listCache, iterator.propertyPath, iterator).DoLayoutList();
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static ReorderableList GetOrCreateList(
            SerializedObject serializedObject,
            Dictionary<string, ReorderableList> cache,
            string propertyPath,
            SerializedProperty property)
        {
            if (cache.TryGetValue(propertyPath, out var existing))
            {
                existing.serializedProperty = serializedObject.FindProperty(propertyPath);
                return existing;
            }

            var list = new ReorderableList(serializedObject, serializedObject.FindProperty(propertyPath), true, true, true, true);

            list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, list.serializedProperty.displayName);

            list.elementHeightCallback = index =>
                EditorGUI.GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index), true) + 4f;

            list.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUI.GetPropertyHeight(element, true);
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };

            cache[propertyPath] = list;
            return list;
        }

        public static void DrawButtons(UnityEngine.Object target, UnityEngine.Object[] targets)
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetParameters().Length == 0 && m.GetCustomAttribute<ButtonAttribute>() != null)
                .ToList();

            if (methods.Count == 0) return;

            EditorGUILayout.Space(8f);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ButtonAttribute>();
                string label = string.IsNullOrEmpty(attr.Label)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : attr.Label;

                if (GUILayout.Button(label))
                {
                    foreach (var obj in targets)
                        method.Invoke(obj, null);
                }
            }
        }
    }
}
