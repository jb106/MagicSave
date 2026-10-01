using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicSave
{
    public abstract class SaveManagerBase<TData, THeader> : MonoBehaviour
        where TData : class, new()
        where THeader : SaveHeaderBase, new()
    {
        [SerializeField] private float _autoSaveIntervalMinutes = 1f;
        
        private enum BootRequest { None, Load, New }

        private static BootRequest _request;
        private static string _requestName = string.Empty;

        private readonly List<ISaveParticipant<TData>> _participants = new();
        private readonly List<ISaveBlocker> _blockers = new();

        public TData Data { get; private set; }
        public THeader Header { get; private set; }
        public string CurrentSaveName { get; private set; } = string.Empty;

        public bool AutoStartRequested { get; private set; }
        public bool ScenePristine { get; private set; } = true;

        public event Action OnSaved;
        public event Action OnSlotsChanged;

        // -----------------------------------------------------------------------------------------------------------
        // What each game provides

        protected abstract int CurrentVersion { get; }
        protected virtual int MinimumVersion => 1;
        
        // Raise it to delete every save on each machine, once
        protected virtual int SavesGeneration => 0;

        // Stored in the game's global save
        protected abstract string AutoLoadSaveName { get; set; }

        // Reload the scene, Boot will run again and pick the request
        protected abstract void RequestReload();

        // Default content of a new game
        protected virtual void OnNewGame(TData data, THeader header) { }

        // Data and Header are set, new or loaded
        protected virtual void OnDataReady() { }

        // Game specific header fields, right before writing
        protected virtual void FillHeader(THeader header) { }

        // Old save: bring data to CurrentVersion. False if it can't be done
        protected virtual bool Migrate(TData data, int fromVersion) => true;
        
        private float _autoSaveTimer;

        // -----------------------------------------------------------------------------------------------------------
        
        // Call it from the game's Awake, once the global save is loaded
        protected void Boot()
        {
            SaveWipe.WipeIfOutdated(SavesGeneration);
            
            var request = _request;
            string requestName = _requestName;

            _request = BootRequest.None;
            _requestName = string.Empty;

            if (request == BootRequest.Load && TryLoad(requestName))
            {
                AutoStartRequested = true;
                return;
            }

            if (request == BootRequest.New)
            {
                StartNew(requestName);
                AutoStartRequested = true;
                return;
            }

            // A requested save that failed to load falls here too
            if (TryLoad(AutoLoadSaveName)) return;

            // No save, or one that can't be loaded = start fresh. The files are kept
            if (!string.IsNullOrEmpty(AutoLoadSaveName)) AutoLoadSaveName = string.Empty;

            StartNew(string.Empty);
        }

        protected virtual void Update()
        {
            if (Header != null) Header.totalPlayTimeSeconds += Time.deltaTime;
            
            UpdateAutoSave();
        }

        private void StartNew(string saveName)
        {
            Data = new TData();
            Header = new THeader
            {
                saveVersion = CurrentVersion,
                createdAtUtc = DateTime.UtcNow.Ticks
            };

            OnNewGame(Data, Header);

            CurrentSaveName = saveName ?? string.Empty;
            OnDataReady();
        }

        private bool TryLoad(string saveName)
        {
            if (!GameSlots.Exists(saveName)) return false;

            var header = PeekHeader(saveName);
            if (header == null)
            {
                Debug.LogError($"[MagicSave] '{saveName}' has no readable header, not loaded");
                return false;
            }

            if (!IsSupported(header.saveVersion))
            {
                Debug.LogError($"[MagicSave] '{saveName}' is version {header.saveVersion}, supported = {MinimumVersion} to {CurrentVersion}");
                return false;
            }

            var data = SaveFile.Read<TData>(SavePaths.GameDataFile(saveName));
            if (data == null) return false;

            if (header.saveVersion < CurrentVersion)
            {
                if (!Migrate(data, header.saveVersion)) return false;
                header.saveVersion = CurrentVersion;
            }

            Data = data;
            Header = header;
            CurrentSaveName = saveName;

            OnDataReady();
            return true;
        }

        // -----------------------------------------------------------------------------------------------------------

        public void RequestNewGame(string saveName)
        {
            _request = BootRequest.New;
            _requestName = saveName ?? string.Empty;

            RequestReload();
        }

        public void RequestLoad(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                RequestNewGame(string.Empty);
                return;
            }

            if (!GameSlots.Exists(saveName)) return;

            _request = BootRequest.Load;
            _requestName = saveName;

            RequestReload();
        }

        public void MarkGameStarted() => ScenePristine = false;

        // The scene was already initialized from that exact save at Boot, so joining is enough
        public bool CanJoinWithoutReload(string saveName) => ScenePristine && !string.IsNullOrEmpty(saveName) && saveName.Equals(CurrentSaveName);
        public bool CanStartNewGameWithoutReload() => ScenePristine && string.IsNullOrEmpty(CurrentSaveName);

        public void AdoptSaveName(string saveName) => CurrentSaveName = saveName ?? string.Empty;

        // -----------------------------------------------------------------------------------------------------------
        // Save

        public void RegisterParticipant(ISaveParticipant<TData> participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }

        public void UnregisterParticipant(ISaveParticipant<TData> participant) => _participants.Remove(participant);

        public void RegisterBlocker(ISaveBlocker blocker)
        {
            if (!_blockers.Contains(blocker)) _blockers.Add(blocker);
        }

        public void UnregisterBlocker(ISaveBlocker blocker) => _blockers.Remove(blocker);

        public virtual bool CanSaveNow()
        {
            foreach (var blocker in _blockers)
            {
                if (blocker.BlocksSave) return false;
            }

            return true;
        }

        public bool Save()
        {
            if (string.IsNullOrEmpty(CurrentSaveName)) return false;
            if (!CanSaveNow()) return false;

            foreach (var participant in _participants) participant.Capture(Data);

            Header.saveVersion = CurrentVersion;
            Header.appVersion = Application.version;
            Header.lastSavedAtUtc = DateTime.UtcNow.Ticks;
            FillHeader(Header);

            if (!SaveFile.Write(SavePaths.GameDataFile(CurrentSaveName), Data)) return false;
            if (!SaveFile.Write(SavePaths.GameHeaderFile(CurrentSaveName), Header)) return false;

            OnSaved?.Invoke();
            OnSlotsChanged?.Invoke();
            return true;
        }

        // -----------------------------------------------------------------------------------------------------------
        // Slots

        public THeader PeekHeader(string saveName) => SaveFile.Read<THeader>(SavePaths.GameHeaderFile(saveName));

        public bool IsCompatible(string saveName)
        {
            var header = PeekHeader(saveName);
            return header != null && IsSupported(header.saveVersion);
        }

        private bool IsSupported(int version) => version >= MinimumVersion && version <= CurrentVersion;

        public bool RenameGame(string oldName, string newName)
        {
            newName = newName?.Trim();
            if (!GameSlots.Rename(oldName, newName)) return false;

            if (AutoLoadSaveName == oldName) AutoLoadSaveName = newName;
            if (CurrentSaveName == oldName) CurrentSaveName = newName;

            OnSlotsChanged?.Invoke();
            return true;
        }

        public string DuplicateGame(string sourceName)
        {
            string newName = GameSlots.Duplicate(sourceName);
            if (newName != null) OnSlotsChanged?.Invoke();

            return newName;
        }

        public bool DeleteGame(string saveName)
        {
            if (!GameSlots.Delete(saveName)) return false;

            if (AutoLoadSaveName == saveName) AutoLoadSaveName = string.Empty;

            OnSlotsChanged?.Invoke();
            return true;
        }
        
        // Auto save
        
        protected virtual bool AutoSaveEnabled => true;
        
        private void UpdateAutoSave()
        {
            if (_autoSaveIntervalMinutes <= 0f || !AutoSaveEnabled) return;

            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer < _autoSaveIntervalMinutes * 60f) return;

            _autoSaveTimer = 0f;
            Save();
        }

        protected virtual void OnApplicationQuit()
        {
            if (AutoSaveEnabled) Save();
        }
    }
}