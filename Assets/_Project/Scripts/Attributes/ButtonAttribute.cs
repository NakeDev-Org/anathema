using System;

namespace NakeDev.Attributes
{
    /// <summary>
    /// Marca um método sem parâmetros para virar um botão no Inspector — útil para testar
    /// ScriptableObjects/Managers sem precisar entrar em Play Mode. Renderizado
    /// automaticamente em qualquer MonoBehaviour/ScriptableObject pelo editor de fallback
    /// geral (ver Scripts/Editor/Inspector/GeneralInspectorEditor.cs), sem precisar de um
    /// Editor customizado próprio por script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        public readonly string Label;

        public ButtonAttribute(string label = null)
        {
            Label = label;
        }
    }
}
