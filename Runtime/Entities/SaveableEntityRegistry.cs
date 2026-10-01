using System.Collections.Generic;

namespace MagicSave
{
    public static class SaveableEntityRegistry
    {
        private static readonly Dictionary<string, SaveableEntity> _byId = new();

        public static void Register(SaveableEntity entity)
        {
            if (string.IsNullOrEmpty(entity.EntityId)) return;
            _byId[entity.EntityId] = entity;
        }

        public static void Unregister(SaveableEntity entity)
        {
            if (string.IsNullOrEmpty(entity.EntityId)) return;
            if (_byId.TryGetValue(entity.EntityId, out var found) && found == entity)
                _byId.Remove(entity.EntityId);
        }

        public static SaveableEntity Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _byId.TryGetValue(id, out var entity) ? entity : null;
        }
    }
}