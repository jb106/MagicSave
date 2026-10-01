using UnityEditor;
using UnityEngine;

namespace MagicSave
{
    [CustomEditor(typeof(SaveableEntity)), CanEditMultipleObjects]
    public class SaveableEntityEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Read only
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_entityId"));
            }

            if (GUILayout.Button("Generate Entity ID"))
            {
                foreach (var t in targets)
                {
                    Undo.RecordObject(t, "Generate Entity ID");
                    ((SaveableEntity)t).GenerateEntityId();
                }
            }
        }
    }
}