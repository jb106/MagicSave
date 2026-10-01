# Magic Save

JSON save system for Unity:

- one global save for user preferences (language, game to load at startup, settings...);
- game slots: one folder per game;
- entities: scene objects that save their state through a fixed id;
- auto save.

Dependency: `com.unity.nuget.newtonsoft-json` (installed automatically).

```
<persistentDataPath>/
  global.json              user preferences
  SavesData/
    game_001/              one game = one folder
      data.json            main game data
      header.json          summary read by the load menu
    game_002/
```

## Quick start

### 1 - Three data classes

```csharp
// User preferences. Default values go here
public class GameGlobalSave
{
    public string autoloadSaveName = string.Empty;
    public int languageIndex;
}

// One game. Free class, no base class required
public class SaveData
{
    public int money;
    public List<string> discoveredDishes = new();
    public Dictionary<string, object> entityStates = new();   // only if you use entities
}

// What the load menu displays. The base already has version, dates and play time
public class SaveHeader : SaveHeaderBase
{
    public int money;
}
```

### 2 - The save manager

One per game, on an object of the scene. It has to run before the other scripts.

```csharp
[DefaultExecutionOrder(-100)]
public class SaveManager : SaveManagerBase<SaveData, SaveHeader>
{
    public static SaveManager Instance;

    private readonly GlobalSave<GameGlobalSave> _global = new();
    public GameGlobalSave Global => _global.Data;
    public void SaveGlobal() => _global.Save();

    // Required
    protected override int CurrentVersion => 1;

    protected override string AutoLoadSaveName
    {
        get => _global.Data.autoloadSaveName;
        set { _global.Data.autoloadSaveName = value; _global.Save(); }
    }

    protected override void RequestReload() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    private void Awake()
    {
        Instance = this;
        _global.Load();   // before Boot, which reads AutoLoadSaveName
        Boot();           // loads a game or starts a new one. Data and Header are never null after this
    }

    // Optional
    protected override void OnNewGame(SaveData data, SaveHeader header) => data.money = 500;
    protected override void FillHeader(SaveHeader header) => header.money = Data.money;
}
```

### 3 - Use it

```csharp
SaveManager.Instance.Data.money += 10;
SaveManager.Instance.Save();           // false when blocked, or when the game has no name yet

SaveManager.Instance.Global.languageIndex = 2;
SaveManager.Instance.SaveGlobal();
```

### What gets saved

Same rules as the Unity inspector: public fields and private fields with `[SerializeField]`. Properties, events and fields with `[JsonIgnore]` are not saved.

Never save a reference to a Unity object (`MonoBehaviour`, `ScriptableObject`, `GameObject`). Save an id instead.

## Saving a system: participants and blockers

When a state doesn't live in `Data` during the game, a participant copies it into `Data` right before the files are written. Restoring is up to each system: it reads `Data` when it is ready.

```csharp
public class RestaurantStateManager : MonoBehaviour, ISaveParticipant<SaveData>
{
    private void Start()
    {
        SaveManager.Instance.RegisterParticipant(this);
        RestoreInventory(SaveManager.Instance.Data);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.UnregisterParticipant(this);
    }

    // Called by Save()
    public void Capture(SaveData data) => CaptureInventory(data);
}
```

A blocker forbids the save while its property is true:

```csharp
public class PlanetLoadManager : MonoBehaviour, ISaveBlocker
{
    public bool BlocksSave => IsBusy;   // no save while a planet is loading

    private void Start() => SaveManager.Instance.RegisterBlocker(this);
}
```

For a rule that belongs to the manager itself, override `CanSaveNow()`.

## Auto save

The manager saves by itself every few minutes, and once more when the game closes.

- Set Auto Save Interval Minutes in the inspector of your manager. `0` turns it off.
- Override `AutoSaveEnabled` to plug it to a user setting:

```csharp
protected override bool AutoSaveEnabled => Global.autoSave;
```

An auto save is a normal `Save()`: blockers apply.

## Entities

To save many scene objects without writing a participant for each one.

### 1 - Add `SaveableEntity` to the object

Its id is generated automatically, and again if the object is duplicated. Only objects placed in a scene get an id: prefabs and objects spawned at runtime don't.

### 2 - Give it a state

Add a component that inherits from `PendingSaveable<T>` on the entity or one of its children:

```csharp
[System.Serializable]
public class SimpleChest_SaveData
{
    public bool opened;
}

public class SimpleChest : PendingSaveable<SimpleChest_SaveData>
{
    private bool _opened;

    public override string SaveKey => "SimpleChest";

    protected override SimpleChest_SaveData Capture() => new() { opened = _opened };

    protected override void Restore(SimpleChest_SaveData data)
    {
        if (data.opened) OpenInstantly();
    }

    // Optional: nothing in the save for this object
    protected override void OnFresh() { }
}
```

`Restore` is called once the object has started and is active. For a position, the `TransformSaver` component is ready to use.

Rules for the state class:

- a class with a parameterless constructor, not an `int` or a `bool` directly;
- no reference to a Unity object;
- don't rename it once saves exist, its name is written in the file;
- two components of the same entity need different `SaveKey` values.

### 3 - Capture and restore the entities of a scene

Entities don't save themselves. A participant gathers them:

```csharp
public class RestaurantStateManager : MonoBehaviour, ISaveParticipant<SaveData>
{
    private SaveableEntity[] _entities;

    private void Awake()
    {
        _entities = FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SaveableEntityGroup.WarnOnDuplicateIds(_entities);
    }

    private void Start()
    {
        SaveManager.Instance.RegisterParticipant(this);
        SaveableEntityGroup.Restore(_entities, SaveManager.Instance.Data.entityStates);
    }

    public void Capture(SaveData data) => SaveableEntityGroup.Capture(_entities, data.entityStates);
}
```

Editor tools: the inspector of `SaveableEntity` has a Generate Entity ID button, and Tools > Save fixes empty or duplicated ids on the opened scenes.

## Menus

```csharp
// Load menu: only header.json is read
foreach (string saveName in GameSlots.GetAll())
{
    var header = SaveManager.Instance.PeekHeader(saveName);
    bool compatible = SaveManager.Instance.IsCompatible(saveName);

    Debug.Log($"{saveName} | {header.money}$ | {header.totalPlayTimeSeconds:0}s | {compatible}");
}

// Both reload the scene
SaveManager.Instance.RequestNewGame(GameSlots.GetNextFreeName());   // game_001, game_002...
SaveManager.Instance.RequestLoad(saveName);

SaveManager.Instance.RenameGame(oldName, newName);
SaveManager.Instance.DuplicateGame(saveName);
SaveManager.Instance.DeleteGame(saveName);

bool free = GameSlots.IsNameAvailable(newName);
```

Subscribe to `OnSlotsChanged` to refresh the list.

## Changing the save format

Adding fields needs nothing: a missing field keeps its default value, an unknown one is ignored.

| Need | Override in the manager |
|---|---|
| A saved field or class was renamed or removed | Raise `CurrentVersion`, convert in `Migrate(data, fromVersion)` |
| Refuse games that are too old, without deleting them | Raise `MinimumVersion` |
| Delete every game on each machine, once | Raise `SavesGeneration` |

`SavesGeneration` can't be undone and applies to players too: keep it for development.

## Advanced

### Manager hooks

| Member | Role |
|---|---|
| `OnNewGame(data, header)` | Starting values of a new game |
| `OnDataReady()` | `Data` and `Header` are set, new or loaded. Push them where the game reads them |
| `FillHeader(header)` | Game specific header fields, right before writing |
| `CanSaveNow()` | Extra rule on top of the blockers |
| `AutoSaveEnabled` | Switch for the auto save |
| `OnSaved`, `OnSlotsChanged` | Events: a save was written, the list of games changed |

### What `Boot()` picks

The first that succeeds: the game asked by `RequestLoad`, the new game asked by `RequestNewGame`, the game named by `AutoLoadSaveName`, else a fresh game with no name. A game with no name can't be saved until `AdoptSaveName(name)` is called.

### `ISaveableState`: when you can't inherit from `PendingSaveable<T>`

`PendingSaveable<T>` is built on this interface. Use it directly when your script already has a base class, or when you want to decide yourself when the state is applied.

```csharp
public class DiscoverTriggerArea : MonoBehaviour, ISaveableState
{
    public string SaveKey => "DiscoverTriggerArea";

    public object CaptureState() => new DiscoverTriggerArea_SaveData { triggered = _triggered };

    public void RestoreState(object state)
    {
        if (state is not DiscoverTriggerArea_SaveData data) return;

        _triggered = data.triggered;
    }
}
```

Unlike `Restore`, `RestoreState` is called right away, possibly before your `Start` or while the object is inactive, and not at all when the save has nothing for it.

### Extra file in a game folder

For heavy data kept out of `data.json`, like a generated map:

```csharp
string path = SavePaths.GameSectionFile(SaveManager.Instance.CurrentSaveName, "bake_" + planetId);

SaveFile.Write(path, bake);
var bake = SaveFile.Read<PlanetBakeData>(path);   // null when the file doesn't exist
```

The file follows the game when it is renamed, duplicated or deleted.

### Time spent while an object was not loaded

Implement `IOfflineSimulatable` on objects that keep living while unloaded (a regrowing plant, a producing machine). The package doesn't call it, the game does after a restore:

```csharp
double elapsed = SaveManager.Instance.Header.totalPlayTimeSeconds - state.lastCapturePlayTime;

foreach (var s in GetComponentsInChildren<IOfflineSimulatable>(true)) s.Simulate(elapsed);
```

### Development saves

`DevSaves.Enabled = true` points the games folder to `<project>/DevSaves/`, next to `Assets`, to share test games in the repository.

### Other

- `SaveableEntityRegistry.Find(id)`: find a loaded entity by its id.
- `SaveJson.Settings`: the Newtonsoft settings every file goes through.
