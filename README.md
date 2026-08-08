# Quick Access

A lightweight Editor window for pinning frequently-used assets, scenes, and in-scene
GameObjects/components for one-click access.

## Opening the window

`Tools > Quick Access`

## Usage

- **Drag and drop** any asset, scene, or scene object (GameObject/Component) into the window to pin it.
- Pinned items are grouped into three columns:
  - **Assets** — click the row to open the asset, click **@** to ping/select it in the Project window.
  - **Scenes** — click the row to open the scene, click **@** to ping it, click **▶** to open the scene and enter Play mode.
  - **Scene Objects** — click the row (or **@**) to open the object's scene (if needed) and ping/select it in the Hierarchy.
- Click **X** on any row to unpin it.
- Newly dropped items briefly highlight and auto-scroll into view.

## Persistence

Pinned items are stored per-project in `UserSettings/QuickAccessData.asset`. This file is:

- **Not** version controlled (Unity's default `.gitignore` excludes `UserSettings/`).
- **Not** synced between machines — each clone of the project keeps its own pinned list.
- Preserved across Editor restarts and Editor layout resets.

## Requirements

Unity 2022.3 or later (verified). Earlier LTS versions with `ScriptableSingleton` and
`GlobalObjectId` support may also work but have not been tested.
