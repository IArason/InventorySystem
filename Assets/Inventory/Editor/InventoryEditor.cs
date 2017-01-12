using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    SerializedProperty array;
    SerializedProperty sizeX;
    SerializedProperty sizeY;
    SerializedProperty arraySizeProp;

    int x, y;

    void OnEnable()
    {
        array = serializedObject.FindProperty("activeNodes");
        arraySizeProp = array.FindPropertyRelative("Array.size");
        
        sizeX = serializedObject.FindProperty("inventorySizeX");
        sizeY = serializedObject.FindProperty("inventorySizeY");

        x = sizeX.intValue;
        y = sizeY.intValue;
        
        arraySizeProp.intValue = sizeX.intValue * sizeY.intValue;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        base.OnInspectorGUI();
        
        if (sizeX.intValue <= 0) sizeX.intValue = 1;
        if (sizeY.intValue <= 0) sizeY.intValue = 1;


        EditorGUILayout.Space();

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Inventory grid", EditorStyles.boldLabel);

        
        EditorGUIUtility.labelWidth = 30;
        EditorGUILayout.BeginHorizontal();
        x = EditorGUILayout.IntField("x", x, GUILayout.ExpandWidth(false), GUILayout.Width(55));
        y = EditorGUILayout.IntField("y", y, GUILayout.ExpandWidth(false), GUILayout.Width(55));
        GUILayout.Space(20);
        if (GUILayout.Button("Set", GUILayout.Width(75)))
        {
            sizeX.intValue = x;
            sizeY.intValue = y;
            arraySizeProp.intValue = x * y;
            for(int i = 0; i < x* y; i++)
            {
                array.GetArrayElementAtIndex(i).boolValue = true;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        arraySizeProp.intValue = sizeY.intValue * sizeX.intValue;

        var controlRect = EditorGUILayout.GetControlRect(false, sizeY.intValue * 25 + 15);
        
        for (int i = 0; i < sizeY.intValue * sizeX.intValue; i++)
        {
            array.GetArrayElementAtIndex(i).boolValue = 
                EditorGUI.Toggle(new Rect(
                    controlRect.position.x + (i%sizeX.intValue) * 25,
                    controlRect.position.y + Mathf.FloorToInt(i / (float)sizeX.intValue) * 25,
                    30,
                    20),
                    array.GetArrayElementAtIndex(i).boolValue
                );
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}