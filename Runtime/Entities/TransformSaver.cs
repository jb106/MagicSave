using UnityEngine;

namespace MagicSave
{
    public class TransformSaver : PendingSaveable<Transform_SaveData>
    {
        public enum SaveSpace
        {
            World,
            Local
        }

        [SerializeField] private string _saveId = "Transform";
        [SerializeField] private Transform _target;
        [SerializeField] private SaveSpace _space = SaveSpace.World;
        [SerializeField] private bool _saveScale;

        private Rigidbody _rb;

        public override string SaveKey => $"Transform_{_saveId}";

        private void Reset() => _target = transform;

        protected override void Start()
        {
            if (_target == null) _target = transform;
            _rb = _target.GetComponent<Rigidbody>();

            base.Start();
        }

        protected override Transform_SaveData Capture()
        {
            var t = _target != null ? _target : transform;

            return new Transform_SaveData
            {
                position = _space == SaveSpace.World ? t.position : t.localPosition,
                rotation = _space == SaveSpace.World ? t.rotation : t.localRotation,
                scale = t.localScale
            };
        }

        protected override void Restore(Transform_SaveData data)
        {
            var t = _target != null ? _target : transform;

            if (_space == SaveSpace.World)
            {
                t.SetPositionAndRotation(data.position, data.rotation);
            }
            else
            {
                t.localPosition = data.position;
                t.localRotation = data.rotation;
            }

            if (_saveScale) t.localScale = data.scale;

            if (_rb != null)
            {
                _rb.position = t.position;
                _rb.rotation = t.rotation;

                if (!_rb.isKinematic)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    [System.Serializable]
    public class Transform_SaveData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale = Vector3.one;
    }
}