using UnityEditor;
using UnityEngine;

// Keep every set at exactly sixteen slots, while allowing individual entries to be null.
[CustomPropertyDrawer(typeof(DualGridTerrainTile.TileSet))]
public class DualGridTileSetDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight
            + (property.isExpanded ? 16 * (EditorGUIUtility.singleLineHeight + Spacing) : 0);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var tiles = property.FindPropertyRelative("tiles");
        if (tiles.arraySize != 16) tiles.arraySize = 16;
        var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var title = property.propertyPath.EndsWith("[0]") ? "Base set" : label.text + " (variation)";
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, title, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            for (var i = 0; i < 16; i++)
            {
                row.y += EditorGUIUtility.singleLineHeight + Spacing;
                EditorGUI.PropertyField(row, tiles.GetArrayElementAtIndex(i),
                    new GUIContent("Slot " + i + " (row " + (i / 4 + 1) + ", col " + (i % 4 + 1) + ")"));
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
