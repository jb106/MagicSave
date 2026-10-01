using System.Collections.Generic;
using UnityEngine;

namespace MagicSave
{
    public static class SaveableEntityGroup
    {
        public static void Capture(SaveableEntity[] entities, Dictionary<string, object> into)
        {
            foreach (var e in entities) e.Capture(into);
        }

        public static void Restore(SaveableEntity[] entities, Dictionary<string, object> from)
        {
            foreach (var e in entities) e.EnsureInit();
            foreach (var e in entities) e.Restore(from);
        }

        public static void WarnOnDuplicateIds(SaveableEntity[] entities)
        {
            var seen = new Dictionary<string, SaveableEntity>();

            foreach (var e in entities)
            {
                if (!seen.TryAdd(e.EntityId, e))
                    Debug.LogError(
                        $"[SaveableEntity] id duplicated {e.EntityId} : '{e.name}' | '{seen[e.EntityId].name}'");
            }
        }
    }
}