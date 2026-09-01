using UnityEngine;

namespace NakeDev.Attributes
{
    /// <summary>
    /// Header estilizado (título colorido + linha de separação) acima do campo — o mesmo
    /// papel do [Header] nativo, só que visualmente mais próximo do que ferramentas
    /// profissionais de Inspector (Odin, NaughtyAttributes) entregam. Puramente decorativo:
    /// não precisa de PropertyDrawer completo, só um DecoratorDrawer (ver
    /// Scripts/Editor/Inspector/InspectorLineDrawer.cs).
    /// </summary>
    public class InspectorLineAttribute : PropertyAttribute
    {
        public readonly string Title;
        public readonly float R;
        public readonly float G;
        public readonly float B;

        public InspectorLineAttribute(string title, float r = 0.65f, float g = 0.8f, float b = 0.75f)
        {
            Title = title;
            R = r;
            G = g;
            B = b;
        }
    }
}
