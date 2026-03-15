using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
public class MinMaxSliderDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxSliderAttribute slider = (MinMaxSliderAttribute)attribute;

        SerializedProperty minProp = property.FindPropertyRelative("min");
        SerializedProperty maxProp = property.FindPropertyRelative("max");

        float min = minProp.floatValue;
        float max = maxProp.floatValue;

        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, label);

        float fieldWidth = 50f;
        float sliderPadding = 5f;

        Rect minField = new Rect(position.x, position.y, fieldWidth, position.height);
        Rect sliderRect = new Rect(
            position.x + fieldWidth + sliderPadding,
            position.y,
            position.width - (fieldWidth * 2) - (sliderPadding * 2),
            position.height
        );
        Rect maxField = new Rect(
            position.x + position.width - fieldWidth,
            position.y,
            fieldWidth,
            position.height
        );

        min = EditorGUI.FloatField(minField, min);

        EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, slider.Min, slider.Max);

        max = EditorGUI.FloatField(maxField, max);

        min = Mathf.Clamp(min, slider.Min, slider.Max);
        max = Mathf.Clamp(max, slider.Min, slider.Max);

        if (min > max)
            min = max;

        minProp.floatValue = min;
        maxProp.floatValue = max;

        EditorGUI.EndProperty();
    }
}