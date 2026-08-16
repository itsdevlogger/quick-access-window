[Email](mailto:thevaishnavchincholkar@gmail.com) · [LinkedIn](https://www.linkedin.com/in/vaishnav-chincholkar)

More tools like this one: **[thevaishnav.github.io](https://thevaishnav.github.io/)**

---

# Quick Access

*Free & open source · Unity Editor tool*

Pin the assets, scenes and scene objects you keep hunting for, and get to them in **one click**, instead of digging through the Project window or the Hierarchy again.

---

## 1 · What it does

**Quick Access** is a lightweight Editor window for pinning frequently-used assets, scenes, and in-scene GameObjects or components. Drag something in, and it stays one click away for the rest of the project's life.

Open it from `Tools > Quick Access`.

### 1.1 · Why it exists

Every project has a handful of things you open twenty times a day: the player prefab, the boot scene, the one settings asset buried five folders deep. Favourites help a little, but they don't open a scene, they don't survive a layout reset, and they don't hold scene objects at all. This window does all three.

## 2 · Install

Add it as a UPM package from Git. In Unity: `Window ▸ Package Manager ▸ + ▸ Add package from git URL…` and paste:

```
https://github.com/itsdevlogger/quick-access-window.git
```

Or clone / download the repo and drop the folder anywhere under `Assets/`.

## 3 · Usage

**Drag and drop** any asset, scene, or scene object (GameObject or Component) into the window to pin it. Pinned items are grouped into three columns:

| Column | What clicking does |
| --- | --- |
| Assets | Click the row to open the asset. Click **@** to ping and select it in the Project window. |
| Scenes | Click the row to open the scene. Click **@** to ping it. Click **▶** to open the scene and enter Play mode. |
| Scene Objects | Click the row (or **@**) to open the object's scene if it isn't already loaded, then ping and select it in the Hierarchy. |

- Click **X** on any row to unpin it.
- Newly dropped items briefly highlight and auto-scroll into view.

## 4 · Persistence

Pinned items are stored per-project in `UserSettings/QuickAccessData.asset`. That file is:

- **Not** version controlled, because Unity's default `.gitignore` excludes `UserSettings/`.
- **Not** synced between machines. Each clone of the project keeps its own pinned list.
- Preserved across Editor restarts and Editor layout resets.

> **Your list is yours**
> Because the data lives outside the project's tracked files, pinning something never shows up in a teammate's diff, and their pins never show up in yours.

## 5 · Requirements

Unity **2022.3** or later (verified). Earlier LTS versions with `ScriptableSingleton` and `GlobalObjectId` support may also work, but have not been tested.
