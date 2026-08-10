# AGENTS.md

Unity 6 (6000.3.10f1) hangman-style word game ("HANGARoo"). Code lives in `Assets/Scripts/`; all gameplay/UI logic is plain MonoBehaviour classes (no ECS, no visual scripting).

## Tooling & verification

- There is NO test suite, linter, or CI. `com.unity.test-framework` is installed but no `Tests/` folders exist.
- Do NOT try to build from the CLI: `Assembly-CSharp.csproj` hard-references the editor install under `/Applications/Unity/Hub/Editor/6000.3.10f1/`, so compile fails outside Unity.
- The only verification is opening/edit-mode compile in the Unity editor. After editing scripts, say so; let the user confirm there are no console errors.

## Scene flow & architecture

- Build order (`ProjectSettings/EditorBuildSettings.asset`): `splash` → `MainMenu` → `GamePlay`.
- `AudioManager` is a `DontDestroyOnLoad` singleton spawned in `splash`; all audio (music, click, victory, defeat) goes through it. Do not create a second one in other scenes — `Awake` destroys duplicates.
- Settings are persisted with `PlayerPrefs`. `AudioManager` loads its own keys in `Awake` (`SfxVolume`, `ClickSound`, `GameSounds`, `MusicEnabled`); `SettingsManager` additionally uses `Hints`, `ShowCategory`, `Difficulty`.

## Critical wiring conventions

- The shared `Assets/Prefabs/Settings_Panel.prefab` is instanced (via `PrefabInstance`) in both `MainMenu` and `GamePlay`. Scenes only override `RectTransform`/`m_IsActive`. **Bind UI fields and events (onClick/onValueChanged) in the prefab file, never in the scenes.**
- In-prefab serialized event targets must point at the prefab's own component fileIDs (e.g. `SettingsManager` uses `m_Target: {fileID: -4216260151771862093}`). `m_Target: {fileID: 0}` means the call is unwired/broken — the `Save` and `Reset To Default` buttons were found broken this way and fixed to reference the in-prefab `SettingsManager`.
- `Settings_Panel` starts inactive (`m_IsActive: 0`), so `SettingsManager.Start` only runs after the panel is opened. Never rely on `SettingsManager` to apply audio at startup — `AudioManager.Awake` owns that.
- `ButtonClickSound.cs` auto-subscribes `PlayClick()` to every `Button` via `FindObjectsOfType(true)`. New buttons get click sounds automatically; do not wire them manually.

## Git conventions

- Conventional-commit style: `feat:`, `fix:`, `chore:`, etc.
- Work on feature branches; merges to `development`/`main` are done via PRs (history shows PRs #5, #6 from feature branches). Current branch: `audio-setup`.
- `.meta` files for two scripts were recently truncated to a guid-only stub (no trailing newline). Do not hand-edit `.meta` files; let Unity regenerate them.