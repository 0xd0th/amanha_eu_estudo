using AmanhaEuEstudo.Core;
using UnityEditor;
using UnityEngine;

namespace AmanhaEuEstudo.EditorTools
{
    // Shows the stat name ("Saúde", "Inteligência"...) as the label of each array element instead of
    // "Element 0", so StatDelta[] and StatFactor[] are readable in the Inspector.
    internal static class StatEntryDrawer
    {
        public static void Draw(Rect position, SerializedProperty property, GUIContent label, string valueField)
        {
            var stat = property.FindPropertyRelative("stat");
            var value = property.FindPropertyRelative(valueField);

            string title = label.text;
            int index = stat.enumValueIndex;
            if (index >= 0 && index < EnumExtensions.StatCount) title = DisplayNames.Of((StatType)index);

            EditorGUI.BeginProperty(position, label, property);
            var rest = EditorGUI.PrefixLabel(position, new GUIContent(title));

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            const float gap = 4f;
            float statWidth = rest.width * 0.55f;
            var statRect = new Rect(rest.x, rest.y, statWidth - gap, rest.height);
            var valueRect = new Rect(rest.x + statWidth, rest.y, rest.width - statWidth, rest.height);
            EditorGUI.PropertyField(statRect, stat, GUIContent.none);
            EditorGUI.PropertyField(valueRect, value, GUIContent.none);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(StatDelta))]
    public class StatDeltaDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            StatEntryDrawer.Draw(position, property, label, "amount");
        }
    }

    [CustomPropertyDrawer(typeof(StatFactor))]
    public class StatFactorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            StatEntryDrawer.Draw(position, property, label, "factor");
        }
    }
}
