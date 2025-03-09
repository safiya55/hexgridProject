using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
       

        // Extract the x and z values from the serialized property
        int x = property.FindPropertyRelative("x").intValue;
        int z = property.FindPropertyRelative("z").intValue;

        // Create a HexCoordinates instance using these values
        HexCoordinates coordinates = new HexCoordinates(x, z);

        // Draw the label using the HexCoordinates.ToString() method
        EditorGUI.LabelField(position, label.text, coordinates.ToString());
    }
}
