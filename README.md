# Magic Save

JSON save system for Unity: a global save for user preferences, game slots, auto save, and scene objects that save their own state.

## Install

In Unity, open Window > Package Manager, click + > Install package from git URL, and paste:

```
https://github.com/jb106/MagicSave.git
```

Or add this line to the `dependencies` of `Packages/manifest.json`:

```json
"com.lonefire.magic-save": "https://github.com/jb106/MagicSave.git",
```

Newtonsoft Json is installed automatically.

## Setup

1 - Write your data classes. Public fields and private `[SerializeField]` fields are saved.

```csharp
public class GameGlobalSave              // user preferences
{
    public string autoloadSaveName = string.Empty;
    public int languageIndex;
}

public class SaveData                    // one game
{
    public int money;
    public Dictionary<string, object> entityStates = new();
}

public class SaveHeader : SaveHeaderBase // shown in the load menu
{
    public int money;
}
```

2 - Write your manager and put it on an object of the scene.

```csharp
[DefaultExecutionOrder(-100)]
public class SaveManager : SaveManagerBase<SaveData, SaveHeader>
{
    public static SaveManager Instance;

    private readonly GlobalSave<GameGlobalSave> _global = new();
    public GameGlobalSave Global => _global.Data;
    public void SaveGlobal() => _global.Save();

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
        _global.Load();
        Boot();   // loads a game or starts a new one
    }

    protected override void OnNewGame(SaveData data, SaveHeader header) => data.money = 500;
    protected override void FillHeader(SaveHeader header) => header.money = Data.money;
}
```

3 - Use it.

```csharp
SaveManager.Instance.Data.money += 10;
SaveManager.Instance.Save();

SaveManager.Instance.Global.languageIndex = 2;
SaveManager.Instance.SaveGlobal();
```

Auto save: set Auto Save Interval Minutes in the inspector of the manager, 0 turns it off.

## Games

```csharp
string[] names = GameSlots.GetAll();
SaveHeader header = SaveManager.Instance.PeekHeader(name);

SaveManager.Instance.RequestNewGame(GameSlots.GetNextFreeName());
SaveManager.Instance.RequestLoad(name);

SaveManager.Instance.RenameGame(oldName, newName);
SaveManager.Instance.DuplicateGame(name);
SaveManager.Instance.DeleteGame(name);
```

`RequestNewGame` and `RequestLoad` reload the scene.

## Saving a system

When a state doesn't live in `Data`, copy it there right before the save:

```csharp
public class Inventory : MonoBehaviour, ISaveParticipant<SaveData>
{
    private void Start()
    {
        Restore(SaveManager.Instance.Data);
        SaveManager.Instance.RegisterParticipant(this);
    }

    public void Capture(SaveData data) { /* copy the state into data */ }
}
```

To forbid the save for a while, implement `ISaveBlocker` and call `RegisterBlocker(this)`:

```csharp
public bool BlocksSave => _isLoading;
```

## Saving scene objects

1 - Add the `SaveableEntity` component to an object of the scene. It gets a fixed id.

2 - Add a component with a state on it:

```csharp
public class Chest_SaveData
{
    public bool opened;
}

public class Chest : PendingSaveable<Chest_SaveData>
{
    private bool _opened;

    public override string SaveKey => "Chest";

    protected override Chest_SaveData Capture() => new() { opened = _opened };
    protected override void Restore(Chest_SaveData data) => _opened = data.opened;
}
```

3 - Capture and restore all of them from one participant:

```csharp
public class SceneSaver : MonoBehaviour, ISaveParticipant<SaveData>
{
    private SaveableEntity[] _entities;

    private void Start()
    {
        _entities = FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        SaveableEntityGroup.Restore(_entities, SaveManager.Instance.Data.entityStates);
        SaveManager.Instance.RegisterParticipant(this);
    }

    public void Capture(SaveData data) => SaveableEntityGroup.Capture(_entities, data.entityStates);
}
```

`TransformSaver` is a ready to use component for position and rotation.

## Good to know

- Never save a reference to a Unity object, save an id instead.
- Don't rename a saved state class once saves exist.
- Files are in `Application.persistentDataPath`: `global.json`, and `SavesData/<game>/data.json` + `header.json`.
- Raise `CurrentVersion` and override `Migrate` when you rename or remove saved fields. Adding fields needs nothing.