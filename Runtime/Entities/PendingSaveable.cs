using UnityEngine;

namespace MagicSave
{
    public abstract class PendingSaveable<T> : MonoBehaviour, ISaveableState where T : class
    {
        private T _pending;
        private bool _started;
        private bool _dataApplied;
        private bool _freshApplied;

        protected T Pending => _pending;

        public abstract string SaveKey { get; }

        /// Snapshot the current state
        protected abstract T Capture();

        /// When save data detected
        protected abstract void Restore(T data);

        /// No save data : default
        protected virtual void OnFresh()
        {
        }

        // ---------------------------------------------------------------------------------

        object ISaveableState.CaptureState() => _pending != null ? _pending : Capture();

        void ISaveableState.RestoreState(object state)
        {
            if (state is not T data) return;
            _pending = data;
            Flush();
        }

        // ------------------------------------------------------------------------------------

        protected virtual void Start()
        {
            _started = true;
            Flush();
        }

        protected virtual void OnEnable()
        {
            Flush();
        }

        private void Flush()
        {
            if (!_started || _dataApplied) return;
            if (!isActiveAndEnabled) return;

            if (_pending != null)
            {
                _dataApplied = true;
                Restore(_pending);
                _pending = null;
                return;
            }

            if (!_freshApplied)
            {
                _freshApplied = true;
                OnFresh();
            }
        }
    }
}