using NakeDev.Attributes;
using UnityEditor;
using UnityEngine;

namespace NakeDev.Editor.Inspector
{
    [CustomPropertyDrawer(typeof(InspectorLineAttribute))]
    public class InspectorLineDrawer : DecoratorDrawer
    {
        private const float TopPadding = 8f;
        private const float LineGap = 2f;
        private const float BottomPadding = 4f;

        private InspectorLineAttribute Attr => (InspectorLineAttribute)attribute;

        public override float GetHeight()
        {
            return TopPadding + EditorGUIUtility.singleLineHeight + LineGap + BottomPadding;
        }

        public override void OnGUI(Rect position)
        {
            var color = new Color(Attr.R, Attr.G, Attr.B);

            var labelRect = new Rect(position.x, position.y + TopPadding, position.width, EditorGUIUtility.singleLineHeight);
            var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = color } };
            EditorGUI.LabelField(labelRect, Attr.Title, style);

            var lineRect = new Rect(
                position.x,
                labelRect.yMax + LineGap,
                position.width,
                1f);
            EditorGUI.DrawRect(lineRect, new Color(color.r, color.g, color.b, 0.5f));
        }
    }
}
