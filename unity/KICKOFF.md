# Sparq Unity v2 — Kickoff

> **Historical document.** This was the original 10-week roadmap. Phases 1–8
> are substantially complete and the project has grown well past the original
> scope (133 scripts, ~120 asset packs, Vitality bridge, multi-zone world,
> recruitable allies, FTUE, hidden debug menu, two code-review passes).
>
> For current state, architecture, and what's left, see **[README.md](README.md)**.
> For the 6-person test playbook, see **[TESTING.md](TESTING.md)**.
>
> This file is kept as a historical record of the original plan — useful for
> context but not as a current to-do list.

---

**Goal**: Rebuild Sparq as a native Unity 2D mobile game. Feature-parity with WebView v1 + game-feel polish worthy of Top Heroes / AFK Arena tier apps.

**Team**: 1 developer (you) + Claude Code (all scripting)
**Timeline**: 10 weeks to Play Store submission
**Engine**: Unity 6 LTS (6000.x) or Unity 2022.3 LTS — both work
**Render pipeline**: URP 2D
**Target**: Android first, iOS after launch
**Aspect**: Portrait, 9:16 base, support 9:18, 9:19.5, 9:20+ with safe areas

---

## Week-by-Week

| Week | Phase | Deliverable |
|------|-------|-------------|
| 1 | Setup + Foundation | Unity project builds to Android. Firebase Auth working. |
| 2 | State + Save | Player data system in C#. Firestore sync. Login flow. |
| 3 | Pet + Home Screen | Karu renders + animates on home. Level badge, XP bar. |
| 4 | Quest System | Daily quests, custom quests, quest-to-XP flow. |
| 5 | Rival & Combat | Enemy HP bar. Tap battle mini-game. 12 enemies. |
| 6 | Adventures + Achievements | Multi-step quest chains. Achievement unlocks. |
| 7 | Shop, Bag, Feeding | Buy/equip/consume items. Pet hunger. |
| 8 | Focus Dungeon | Timed Pomodoro mini-game with spawning enemies. |
| 9 | Onboarding + Polish | Una tutorial. Audio mixing. Visual FX pass. |
| 10 | Ship | Build signed AAB. Submit to Play Store beta. |

Each week assumes ~10-15 hours from you + unlimited Claude Code.

---

## Architecture

```
unity/
  Assets/
    Scripts/
      Core/          — GameManager, PlayerData, Save/Load
      Systems/       — Combat, Quests, Adventures, Shop, Achievements
      UI/            — Home, Profile, Battle, Dungeon, Tutorial
      Data/          — ScriptableObjects (Enemies, Items, Adventures)
      Firebase/      — Auth, Firestore, Sync
    Prefabs/         — Reusable game objects (EnemyCard, QuestItem, etc.)
    Scenes/
      Boot.unity     — Loads Firebase, determines auth state
      Auth.unity     — Login / signup screen
      Home.unity     — Main game
      Battle.unity   — Tap battle (additive overlay)
      Dungeon.unity  — Focus Dungeon (additive overlay)
    Art/             — Sprites, animations, UI (bought from Asset Store)
    Audio/           — Music + SFX (bought from Asset Store)
    Resources/       — Runtime-loaded assets
```

## Core systems we'll build first

1. **`GameManager`** — singleton, app entry point
2. **`PlayerData`** — matches WebView state schema 1:1
3. **`SaveService`** — local (PlayerPrefs) + cloud (Firestore)
4. **`EventBus`** — pub/sub for `OnQuestCompleted`, `OnLevelUp`, etc.
5. **`FirebaseService`** — Auth + Firestore wrapper
6. **`UIManager`** — page switching, toasts, modals

## Data parity with WebView

These state keys MUST survive the migration so users don't lose progress:
- `totalXP`, `currentXP`, `level`, `xpToNextLevel`
- `streak`, `longestStreak`, `lastActiveDate`
- `fitchXP`, `currentEnemyIndex`, `defeatedEnemies`
- `sparqCoins`, `purchases`, `equippedItems`
- `customTasks`, `customReminders`, `completedAdventures`
- `unlockedAchievements`, `eggs`
- `foodInventory`, `potionInventory`, `treatInventory`, `keyInventory`
- `hunger`, `petTaps`
- `activePet`, `petName`, `pronouns`, `ndProfile`
- `soundEnabled`, `reducedMotion`
- `onboardingComplete`, `voltBeats`, `streakShields`, `voltFreezeUntil`
- `tapBattleCooldownUntil`, `focusSessions`, `focusMinutesTotal`

Firestore schema: `users/{uid}` document with all the above fields. Same as WebView.

## Migration strategy

1. Unity reads the same Firestore document the WebView writes
2. Existing users log in → their progress loads automatically
3. No database migration needed — it's the same schema
4. WebView stays deployed as a fallback for a few months

## Next: open `SETUP.md` for Monday's first steps.
