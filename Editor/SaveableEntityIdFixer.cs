using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MagicSave
{
    public static class SaveableEntityIdFixer
    {
        [MenuItem("Tools/Save/Fix EntityId on opened scenes")]
        private static void FixOpenScenes()
        {
            var entities = Object.FindObjectsByType<SaveableEntity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var seen = new HashSet<string>();
            var fixedCount = 0;

            var idField = typeof(SaveableEntity).GetField("_entityId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var entity in entities)
            {
                var id = (string)idField.GetValue(entity);

                if (!string.IsNullOrEmpty(id) && seen.Add(id)) continue;

                var newId = System.Guid.NewGuid().ToString("N");
                Undo.RecordObject(entity, "Fix EntityId");
                idField.SetValue(entity, newId);
                seen.Add(newId);

                EditorUtility.SetDirty(entity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
                fixedCount++;
            }

            if (fixedCount > 0)
                EditorSceneManager.MarkAllScenesDirty();

            Debug.Log($"[SaveableEntityIdFixer] {entities.Length} entities analysed, {fixedCount} id changed.");
        }

        [MenuItem("Tools/Save/Regenerate ALL EntityId on opened scenes")]
        private static void RegenerateAllOpenScenes()
        {
            if (Application.isPlaying) return;

            var entities = Object.FindObjectsByType<SaveableEntity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (entities.Length == 0) return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Regenerate ALL EntityId",
                $"{entities.Length} entities will get a NEW id.\n\n" +
                "All existing saves become obsolete: their states will be orphaned and ignored on load.\n\n" +
                "Undoable with Ctrl+Z until scenes are saved.",
                "Regenerate all", "Cancel");

            if (!confirmed) return;

            var seen = new HashSet<string>();

            var idField = typeof(SaveableEntity).GetField("_entityId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var entity in entities)
            {
                Undo.RecordObject(entity, "Regenerate EntityId");
                idField.SetValue(entity, BuildUniqueId(entity, seen));

                EditorUtility.SetDirty(entity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
            }

            EditorSceneManager.MarkAllScenesDirty();

            Debug.Log($"[SaveableEntityIdFixer] {entities.Length} entityId regenerated.");
        }

        private static string BuildUniqueId(SaveableEntity entity, HashSet<string> seen)
        {
            string prefix = Sanitize(entity.gameObject.name);

            for (int attempt = 0; attempt < 8; attempt++)
            {
                string candidate = $"{prefix}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
                if (seen.Add(candidate)) return candidate;
            }

            string fallback = $"{prefix}_{System.Guid.NewGuid():N}";
            seen.Add(fallback);
            return fallback;
        }

        private static string Sanitize(string name)
        {
            var sb = new System.Text.StringBuilder(name.Length);

            foreach (char c in name)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return sb.ToString();
        }
    }
}
