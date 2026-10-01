using System.Collections.Generic;
using UnityEngine;

namespace MagicSave
{
    public class SaveableEntity : MonoBehaviour
    {
        [SerializeField] private string _entityId;

        private ISaveableState[] _saveables;

        public string EntityId => _entityId;

        private void Awake() => EnsureInit();

        private void OnDestroy()
        {
            SaveableEntityRegistry.Unregister(this);
        }

        public void EnsureInit()
        {
            if (_saveables != null) return;

            var found = new List<ISaveableState>();
            Collect(transform, found);
            _saveables = found.ToArray();

            SaveableEntityRegistry.Register(this);
        }

        private void Collect(Transform t, List<ISaveableState> into)
        {
            into.AddRange(t.GetComponents<ISaveableState>());

            foreach (Transform child in t)
            {
                if (child.GetComponent<SaveableEntity>() != null) continue;

                Collect(child, into);
            }
        }

        public void Capture(Dictionary<string, object> entityStates)
        {
            EnsureInit();
            foreach (var s in _saveables)
            {
                entityStates[$"{_entityId}/{s.SaveKey}"] = s.CaptureState();
            }
        }

        public void Restore(Dictionary<string, object> entityStates)
        {
            EnsureInit();
            foreach (var s in _saveables)
            {
                if (entityStates.TryGetValue($"{_entityId}/{s.SaveKey}", out object state))
                {
                    s.RestoreState(state);
                }
            }
        }

#if UNITY_EDITOR
        public void GenerateEntityId()
        {
            _entityId = $"{gameObject.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
            MarkDirty();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            if (!IsSceneInstance())
            {
                if (!string.IsNullOrEmpty(_entityId))
                {
                    _entityId = string.Empty;
                    MarkDirty();
                }

                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (Application.isPlaying) return;
                if (!IsSceneInstance()) return;

                if (string.IsNullOrEmpty(_entityId) || IsDuplicateInScene())
                    GenerateEntityId();
            };
        }

        private bool IsSceneInstance()
        {
            if (UnityEditor.EditorUtility.IsPersistent(this)) return false;

            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
            if (stage != null) return false;

            return gameObject.scene.IsValid();
        }

        private void MarkDirty()
        {
            UnityEditor.EditorUtility.SetDirty(this);

            if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private bool IsDuplicateInScene()
        {
            foreach (var other in FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include))
                if (other != this && other._entityId == _entityId)
                    return true;

            return false;
        }
#endif
    }
}