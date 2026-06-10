# Unity 101 for Sparq — only the 10% you need

> **Evergreen onboarding doc.** The Unity-operation skills below are still
> exactly right. The one outdated bit: **Sparq today uses only two scenes**
> (`Boot.unity` → `Home.unity`) plus a stack of procedural UI panels that
> render as overlays. The original plan of separate Battle / Dungeon / Auth
> scenes was simplified to overlay panels on the Home scene. So when this doc
> says "Sparq will have Boot/Auth/Home/Battle/Dungeon", read that as
> "Boot/Home — everything else is an in-scene panel."

You don't need to be a Unity developer. You need to know how to **operate** Unity so I can give you precise instructions. ~2 hours of familiarization and you're set.

---

## The 4 windows you'll use constantly

```
┌─────────────┬─────────────────────┬──────────────┐
│             │                     │              │
│  HIERARCHY  │    SCENE VIEW       │   INSPECTOR  │
│             │   (editable world)  │  (properties │
│  (list of   │                     │   of selected│
│   objects)  │                     │    object)   │
│             ├─────────────────────┤              │
│             │   GAME VIEW         │              │
│             │  (what player sees) │              │
├─────────────┴─────────────────────┴──────────────┤
│  PROJECT (files: scripts, prefabs, art, audio)   │
└───────────────────────────────────────────────────┘
```

### 1. Hierarchy (left panel)
Lists every GameObject in the current scene. Like an outline.
- Click any entry → selects it
- Drag to rearrange / re-parent
- Right-click → create new objects

### 2. Scene View (center, top)
Visual world editor. You position and size things here.
- Left drag = select / move
- Right drag = rotate camera
- Middle drag / scroll = pan / zoom
- `F` key = focus camera on selected object

### 3. Game View (center, bottom — or a tab)
What the player will actually see. Hit **Play** (triangle button at top) to simulate. Hit **Play** again to stop.

### 4. Inspector (right panel)
Shows all properties of whatever you selected. Change values here.

### 5. Project (bottom panel)
Your files. Scripts, prefabs, art, audio, scenes.
- Double-click a scene to open it
- Double-click a script to edit it (opens VSCode)
- Drag a prefab into the Hierarchy to place it in the scene

---

## The 3 buttons you need to know

Top toolbar, middle area:
- **▶️ Play** — start/stop game simulation
- **⏸️ Pause** — pause game
- **⏭️ Step** — advance one frame at a time

Top-left:
- **Hand / Move / Rotate / Scale / Rect** tools — how your mouse interacts with Scene View. 99% of the time you want **Move** (keyboard `W`).

---

## Key concepts (5 minutes)

### GameObject
Any "thing" in the game world. A button, the player, a background, an invisible manager. Has a Transform (position/rotation/scale) and optionally has components.

### Component
A behavior attached to a GameObject. Examples:
- `SpriteRenderer` = makes it visible
- `Rigidbody2D` = makes it physically react
- `Button` (UI) = makes it clickable
- `QuestManager.cs` = custom C# logic you attached

**I write components, you attach them to GameObjects.**

### Prefab
A pre-made GameObject saved as a file. Drag into scene = instance. Change the prefab = all instances update. Like a template.

### Scene
A level / screen. Sparq will have: Boot, Auth, Home, Battle, Dungeon. You switch scenes in code or by double-clicking in Project tab.

### ScriptableObject
A data container. We'll use these for enemy definitions, items, adventures. Edit values in the Inspector like a form; the game reads them at runtime.

### Scripts
C# files. We put them in `Assets/Scripts/`. I write them. You **attach them to GameObjects** by dragging the script file onto the GameObject in the Inspector, or clicking "Add Component" and finding it by name.

---

## Common tasks you'll do (this is what I'll ask of you)

### "Drag `QuestCardPrefab` into the HomeCanvas under `QuestList`"
1. In **Project tab**, find `Assets/Prefabs/QuestCardPrefab`
2. In **Hierarchy**, expand `HomeCanvas` → find `QuestList`
3. Drag prefab from Project → drop on `QuestList`
4. Done

### "Attach `HomeController.cs` to the `Home` GameObject"
1. In **Hierarchy**, click `Home`
2. In **Inspector**, scroll to bottom → click **Add Component**
3. Type "Home Controller" → select it from dropdown
4. Done

### "Set the `enemyColor` on this ScriptableObject to #FF6A00"
1. In **Project**, double-click the ScriptableObject (like `EnemyVolt.asset`)
2. In **Inspector**, find the `enemyColor` field
3. Click the color swatch → pick color → done

### "Build for Android"
1. **File → Build Settings**
2. Select **Android** platform → click **Switch Platform** (one-time, takes minutes)
3. Click **Build and Run** (phone plugged in) OR **Build** (produces APK/AAB)
4. Done

### "Open the Auth scene"
1. In **Project**, navigate to `Assets/Scenes/`
2. Double-click `Auth.unity`
3. Scene switches

---

## The C# panic-free zone

You'll look at C# sometimes. Here's all you need to parse:

```csharp
public class QuestItem : MonoBehaviour  // It's a "quest item" script attached to an object
{
    public string questName;    // This shows up in the Inspector as an editable text field
    public int xpReward = 20;   // This shows as an editable number, defaulting to 20

    public void OnClick()       // This is a method — triggered when something happens
    {
        // Code here runs when OnClick is called
    }
}
```

**What you need to do with scripts**: mostly nothing. You'll change values in the **Inspector** (the text fields/numbers marked `public` show up there). I rarely need you to edit the code.

---

## VSCode setup (5 min)

Unity uses whatever external editor you tell it to. Set VSCode:

1. **Edit → Preferences → External Tools**
2. External Script Editor → pick **Visual Studio Code**
3. If not installed, get it: https://code.visualstudio.com
4. Install the **C# Dev Kit** extension in VSCode

Now when you double-click a script, VSCode opens it. (We'll mostly have you read, not write.)

---

## Things that WILL trip you up

### "Some text shows up in the console as red"
Errors. Send me a screenshot of the full console message. Don't panic. Usually a missing reference in the Inspector.

### "The play button is grey"
Unity is still compiling scripts. Wait 10-30 seconds. The bottom-right shows compilation status.

### "Nothing happens when I hit Play"
Likely the scene has no Camera or no Canvas. I'll tell you which to add.

### "Unity crashes"
Happens. Just reopen the project. If it crashes consistently, report to me with the "Last error" from `Editor.log` (I'll show you how to find it).

### "I saved something and it looks broken"
Don't freak out. Unity versions everything in `ProjectSettings/` and `Assets/`. As long as you don't git commit broken state, we can roll back.

### "Where is the project file?"
Unity doesn't have a single project file like VS. The whole `unity/` folder IS the project. To "share the project" you share the folder (we use git).

---

## Our workflow going forward

**Every Unity task I give you will look like this:**

```
TASK: Add the quest list to the Home scene
1. Open scene: Assets/Scenes/Home.unity
2. In Hierarchy, find Canvas → Content → Panel
3. Right-click Panel → UI → Scroll View. Name it "QuestList".
4. Set Anchors: stretch horizontal, top. Width 100%, Height 400.
5. Attach QuestListController.cs (already in Scripts/UI/) to QuestList.
6. Drag QuestItemPrefab (Assets/Prefabs/) into the QuestListController's "Template" field in Inspector.
7. Hit Play. You should see 3 quest rows.
8. Send me a screenshot.
```

Every step. Every drag. Every click.

**You'll never have to "figure out" what I mean.** If any step is unclear, that's my fault — tell me and I'll rewrite it.

---

## If you're nervous

That's expected. First week of Unity is always weird. By Week 2 it's muscle memory. By Week 4 you're fluent.

The worst case: you hit a wall, send me your screen, I debug it remotely.

The usual case: things Just Work. You're here to drag prefabs and hit Play. I do the hard part.

Let's build.
