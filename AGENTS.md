# Repository Guidelines

## Project Structure & Module Organization
This repository contains a Unity game project in `Ninja Slasher/`. Core gameplay code lives in `Ninja Slasher/Assets/Scripts/`, organized by domain: `Gameplay Systems/`, `Player/`, `Enemies/`, `Platforms/`, `UI/`, and `Events/`. Scenes are under `Assets/Scenes/`; reusable prefabs, art, audio, and ScriptableObjects live in `Assets/Prefabs/`, `Assets/Graphics/`, `Assets/Audio/`, and `Assets/Scriptable Objects/`. Package and editor settings are stored in `Packages/` and `ProjectSettings/`. Do not hand-edit generated folders such as `Library/`, `Logs/`, or `obj/`.

## Build, Test, and Development Commands
Open the project with Unity Hub using `Ninja Slasher/` and Unity `6000.0.73f1`.

- `dotnet build "Ninja Slasher/Assembly-CSharp.csproj"`: fast compile check for gameplay scripts.
- Unity Editor -> Play: primary test loop for gameplay, UI, and scene flow.
- Unity Editor -> File -> Build Settings -> Android/iOS -> Build: produce device builds.
- `git status --short`: verify only intended files changed before committing.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and keep `using` directives minimal. Follow existing naming patterns: classes, methods, enums, and properties in `PascalCase`; private serialized fields in `_camelCase`; interfaces with `I` prefix. Keep scripts focused by feature area and prefer extending the existing manager-and-events architecture (`MonoBehaviourSingleton<T>`, `GameEvents`, `UIEvents`) instead of introducing new global state. Unsubscribe from events in `OnDisable`, and null-check singletons before use, for example `SaveManager.Instance?.Modify(...)`.

## Testing Guidelines
`com.unity.test-framework` is installed, but this repository currently relies on manual verification rather than committed test suites. Validate gameplay changes in Play Mode and retest the affected scene directly, especially `Assets/Scenes/` content and UI flows. If you add automated tests, place them in a dedicated `Assets/Tests/` folder and name files after the target class, such as `AdsManagerTests.cs`.

## Commit & Pull Request Guidelines
Recent history uses short, imperative commit subjects such as `Fix lvl 1`, `Magnetic Platform Anims`, and `Update PreGame modal layout and UI bindings`. Keep subjects concise, capitalized, and without trailing punctuation. Pull requests should summarize gameplay impact, list touched scenes/prefabs/scripts, mention editor or device validation performed, and include screenshots or recordings for visible UI or animation changes.
