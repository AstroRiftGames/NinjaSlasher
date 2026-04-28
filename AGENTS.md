# AGENTS.md - Ninja Slasher

Guidelines for autonomous coding agents.

## Project

- **Type**: Unity mobile game (Android/iOS), URP
- **Version**: Unity 2021.3+
- **Architecture**: Manager-orchestrated + event-driven
- **Pattern**: `MonoBehaviourSingleton<T>`, `GameEvents`

---

## Build Commands

### Android
```
Unity Hub > open "Ninja Slasher" > Build Settings > Build (Android)
```

### iOS
```
Unity Hub > open "Ninja Slasher" > Build Settings > Platform > iOS > Build
```

### Testing
No unit tests. Test in-editor via Play Mode.

---

## Code Style

### Template
```csharp
using UnityEngine;
using System.Collections;
using System.Linq;

public class MyClass : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] private float _someValue = 1f;

    [Header("References")]
    [SerializeField] private Transform _myTransform;

    #region INITIALIZATION
    private void Awake() { }
    private void Start() { }
    #endregion
}
```

### Imports
```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManager;
```

### Naming
| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `PlayerController` |
| Interface | I Prefix | `ITracker` |
| Private Field | _camelCase | `_isDashing` |
| Property | PascalCase | `IsVictory` |

### Null Safety
```csharp
if (SaveManager.Instance != null)
    SaveManager.Instance.Modify(data => data.AddCoins(amount));

GameManager.Instance?.PublicMethod();
```

---

## Event Pattern (Critical)

```csharp
private void OnEnable()
{
    GameEvents.OnLevelStarted += OnLevelStarted;
}

private void OnDisable()
{
    GameEvents.OnLevelStarted -= OnLevelStarted;  // ALWAYS unsubscribe!
}
```

---

## Singleton
```csharp
public class MyManager : MonoBehaviourSingleton<MyManager>
{
    public override void Awake()
    {
        base.Awake();
    }
}
```

---

## Unity Lifecycle

1. `Awake()` - initializes
2. `OnEnable()` - becomes active
3. `Start()` - before first frame
4. `OnDisable()` - becomes inactive
5. `OnDestroy()` - is destroyed

---

## Directory Structure
```
Assets/Scripts/
├── Gameplay Systems/  # Managers
├── Player/            # Player controller
├── Enemies/           # Enemies
├── Events/            # GameEvents
└── UI/                # Screens, Modals
```

---

## Critical Rules

1. **Unsubscribe** events in `OnDisable` only
2. **Null check** singletons before use
3. **Use Time.deltaTime** for movement
4. **No static instances** - use `MonoBehaviourSingleton<T>`
5. Keep changes incremental and production-safe