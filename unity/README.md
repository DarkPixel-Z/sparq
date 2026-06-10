# Sparq — Unity Mobile App

A 2D mobile RPG with a wellness-bridge twist: real-life mood and care actions
power up your in-game squad. Built in Unity 6 for Android first, iOS after.

> **Status (June 2026):** Feature-complete for closed test. 30 backlog items
> shipped, two rounds of code review applied (12 fixes), hidden tester debug
> menu in place. Next milestone: first Android device build → 6-person
> friends-and-family test. See [TESTING.md](TESTING.md) for the test plan.

## Engine stack

| Layer          | Tool                                              | Notes                                             |
| -------------- | ------------------------------------------------- | ------------------------------------------------- |
| Game engine    | Unity 6 LTS                                       | URP 2D, portrait-only, 1080×1920 reference        |
| UI             | Procedural (UGUI + TMP)                           | All panels built in code — no prefabs to drift   |
| Animation      | Sprite-frame swap + TweenLite-style helpers       | DOTween-equivalent reach for free                 |
| Art            | Asset Store sprite packs (~120 in `Assets/`)      | Inventoried in [ASSETS.md](ASSETS.md)             |
| Persistence    | `PlayerPrefs` + JsonUtility (single save blob)    | Cloud save scaffolded (Firestore, gated off)      |
| Push           | Native plugin scaffold (gated off)                | Off for closed test                               |
| Auth           | None for closed test                              | Cloud save + auth are the post-test priority      |

## Architecture at a glance

```
Boot.unity                   ← bootstrap: SaveService.Load, then transition
  └→ Home.unity              ← the only "real" scene
       └→ HomeLobbyPanel     ← top-level lobby (procedural canvas)
            └→ N overlay panels at higher canvas sortingOrder
               (Store, Quests, Equipment, Bag, World, Battle,
                Journal, Pet, Settings, Debug, …)
```

Everything past `Home.unity` is a procedural UI panel rendered on its own
`Canvas` with a dynamically-computed sortingOrder (`maxSort + 20`) so it always
stacks above whatever's below. There are **no scene transitions for gameplay**
— battles, the explore map, the store, etc. are all overlays. This keeps
state simple (no scene-passing-data shenanigans) and made the entire
back-button / pause story trivial.

## Project layout

```
unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/           PlayerData · SaveService · GameManager
│   │   ├── Audio/          SoundManager · ProceduralSFX
│   │   ├── Cinematic/      CameraBreathing · IdleBreathing · ParallaxLayer
│   │   ├── Models/         QuestTypes
│   │   ├── Safety/         BlockList · ContentModerator · ModerationQueue · …
│   │   ├── Systems/  (29)  Progression · QuestManager · MoodService ·
│   │   │                   VitalityService · AfkRewardService · AllyRoster ·
│   │   │                   CloudSaveService · DailyBonusManager ·
│   │   │                   EquipmentService · HeroClassResolver · …
│   │   └── UI/       (86)  HomeLobbyPanel · WorldExplorePanel · StorePanel ·
│   │                       JournalPanel · QuestsPanel · BagPanel · SettingsPanel ·
│   │                       DebugPanel · WelcomePanel · …
│   ├── Scene/              Boot.unity · Home.unity
│   ├── (~120 asset packs)  See ASSETS.md
│   └── _Recovery/          Old scene snapshots — DO NOT include in build
├── ProjectSettings/        Engine config (build target, scripting backend, …)
└── *.md                    Docs (this file, SETUP, KICKOFF, TESTING, ASSETS, UNITY_101)
```

## Major systems

The 29 `Scripts/Systems/` modules each own one slice. The most-touched:

- **`Progression`** — *the* canonical XP curve (`100 × 1.18^(L-1)`). Every
  XP source flows through `Progression.GrantXp` so a level-up is consistent
  no matter what granted it. `MigrateLegacyThreshold` heals saves from older
  curves; `ResetMigrationLatch` re-arms it after cloud-merge.

- **`QuestManager`** — daily quests, streak logic, Grace Day shields (the
  weekly safety net for low-energy / ADHD users). Daily reset rolls quests
  + applies streak math; `EnsureFloorQuestSatisfied` guarantees a baseline
  reward path even if no quest completes.

- **`MoodService`** — per-day mood log persisted as CSV in `PlayerPrefs`.
  Caches `LoggedToday()` because the lobby + AFK popup poll it many times
  per second.

- **`VitalityService`** — the wellness→power bridge. Reads
  mood-log + care actions; outputs a 0–`MAX_BUFF` multiplier that boosts
  squad ATK/HP and AFK earnings. Single read site (`UpdateVitalityBanner`).

- **`AfkRewardService`** — offline-earnings clock + cap.
  Pairs with the AFK rewards popup on lobby re-open.

- **`EquipmentService` + `AllyRoster` + `HeroClassResolver`** — gear,
  recruitable allies (one per zone), and hero-class resolution for squad
  builds.

- **`SaveService`** — JsonUtility serialized into `PlayerPrefs`. Single save
  blob. Has `Clear()` for the debug menu's Reset Save.

- **`CloudSaveService`** — Firestore scaffold gated behind `SPARQ_CLOUDSAVE`.
  Off for closed test. Hooks `Progression.MigrateLegacyThreshold` after merge.

## Save data

`PlayerData` is a single class (`Assets/Scripts/Core/PlayerData.cs`) serialized
to one `PlayerPrefs` key (`sparq.save`). Fields are documented inline. Key
groups:

- **Identity**: `playerName`, `pronouns`, `ndProfile`, avatar fields
- **Progression**: `level`, `currentXP`, `xpToNextLevel`, `totalXP`, `heroClass`
- **Streak**: `streak`, `longestStreak`, `streakShields` (Grace Days), `lastActiveDate`
- **Pet (Karu)**: `petLevel`, hunger / hygiene / dental / social meters, `petAlive`
- **Economy**: `sparqCoins`, equipment, ally unlocks
- **Quests**: `customTasks`, `lastQuestResetDate`, `completedToday`
- **Daily login**: `lastDailyBonusDate`, login streak

Side state in `PlayerPrefs` (not part of the save blob):
- `sparq.ftue.welcomed` — FTUE flag
- `sparq.quests.floorXpDate` — floor-XP guard (per-day)
- `sparq.mood.log` — MoodService's CSV log

The debug menu's Reset Save clears all of these.

## Recent work — chronological

A summary of what landed; see commits and the in-repo task list for detail.

1. **Phases 1–8 of original KICKOFF roadmap** — scaffold, save system,
   pet+home, quest system, combat, adventures, shop+bag+feeding, focus
   dungeon. (See [KICKOFF.md](KICKOFF.md) for the original plan.)
2. **30 backlog items** covering: zone progression, AFK rewards + popup +
   doubler, multi-zone enemies + signature drops + ally roster, Vitality
   bridge, daily login bonus, gold-sink Forge, FTUE, push-notification
   scaffold, cloud-save scaffold, pet evolution, gear catalog expansion,
   accessibility (Reduce Motion).
3. **2 code-review passes** = 12 fixes:
   - Round 1 (8): unify XP routing through `Progression`, stage-title
     plumbing, QuestManager floor-XP daily guard, MigrateLegacyThreshold,
     StorePanel Featured-on-default fix, MoodService cache, HeroPortrait
     cache, ZONES struct consolidation.
   - Round 2 (3 + 1): JournalPanel XP routing, Grace Day refund loophole,
     CloudSave migration hook, plus threshold-heal persistence.
4. **Hidden tester debug menu** (`DebugPanel.cs`) — 7 taps on the Version
   row in Settings. Reset Save, +1000 Coins, Replay FTUE, Force Daily
   Reset, Re-run Migration. See [TESTING.md](TESTING.md).

## What is NOT done

Honest list — these are not blockers for the closed test, but they ARE
blockers for store submission:

- **Auth flow** — there's no sign-in. Closed test uses anonymous local saves.
- **Cloud save activation** — scaffolded behind `SPARQ_CLOUDSAVE`, off.
- **Server-authoritative anything** — currency, level, equipment all local.
  Trivially editable on a rooted device. Fine for closed test.
- **Push notifications activation** — scaffolded behind `SPARQ_NOTIFICATIONS`, off.
- **Real privacy policy hosting** — page exists; needs a stable URL for store submission.
- **iOS build** — Android first. iOS pipeline untested.
- **Analytics / telemetry** — none. Closed-test feedback is collected via
  a Google Form linked from the debug menu (see TESTING.md).
- **Localization** — English only.

## Build to a device

Full walkthrough: **[BUILD_ANDROID.md](BUILD_ANDROID.md)** — step-by-step
playbook for the first device build (~1 hour with Android Build Support
installed, ~3 hours from cold start).

Pre-flight already done in `ProjectSettings/`:

- Scenes-in-build = `Boot.unity`, `Home.unity` (in that order)
- `productName = Sparq`, `companyName = Sparq`
- `AndroidMinSdkVersion = 26` (Android 8.0)

Still to do in Unity UI (Player Settings — Android):

- Package Name (must be unique reverse-DNS, e.g. `com.sparq.app`)
- Target architectures: ARMv7 + ARM64
- Scripting Backend: IL2CPP
- Target SDK: API 34 (Android 14)

## Working in this project

- **Procedural-UI pattern**: every panel has its own `EnsureEventSystem`,
  `NewGO`, `MakeText`, `LoadSprite` helpers. Yes, this duplicates ~25
  files — but a shared base meant cross-panel breakage when one tweaked
  layout. The current copy-paste pattern is intentional friction.
- **Sort order**: every new top-level panel computes
  `int maxSort = N; foreach (Canvas c in FindObjectsByType<Canvas>()…)`
  and sets its own sortingOrder to `maxSort + 20`. This is how the
  10+ panels stack reliably.
- **XP / level**: ALWAYS use `Progression.GrantXp(data, amount)`. Direct
  writes to `data.totalXP` / `data.currentXP` are a bug, caught by both
  code-review passes.
- **Day rollover**: the codebase mixes local `DateTime.Now` (quests,
  streak, daily bonus) and UTC (mood log unix timestamps). This is a
  known design inconsistency; not a blocker, but worth knowing if you're
  touching daily-rollover code.
- **Editor-only `LoadSprite` paths**: many panels use
  `UnityEditor.AssetDatabase.LoadAssetAtPath` inside `#if UNITY_EDITOR`
  with a runtime fallback. The runtime fallback uses `Resources.Load` or
  baked references. Verify your panel works in a built Player, not just
  in-editor.

## Docs map

- **[SETUP.md](SETUP.md)** — Day 1 setup for a new dev. Unity install,
  Asset Store, package list.
- **[BUILD_ANDROID.md](BUILD_ANDROID.md)** — the first-Android-build
  walkthrough. Step-by-step from Player Settings through phone install.
- **[TESTING.md](TESTING.md)** — the 6-person closed test playbook.
  Debug menu trigger, feedback form, what to test, how to install.
- **[KICKOFF.md](KICKOFF.md)** — original 10-week roadmap, kept as history.
  Today's state is past most of it.
- **[ASSETS.md](ASSETS.md)** — asset-pack shopping list, with notes on
  what's actually in use.
- **[UNITY_101.md](UNITY_101.md)** — Unity for the not-a-Unity-developer.
  Generic crash course; useful onboarding for non-engineers.
