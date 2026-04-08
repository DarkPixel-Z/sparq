# Sparq — Unity Native App

This folder contains the Unity project for the **Sparq** Play Store release.
The web prototype in the root directory serves as the design + UX reference.

## Engine Stack

| Layer        | Tool                       | Purpose                                      |
|--------------|----------------------------|----------------------------------------------|
| Game engine  | Unity 2022 LTS             | Core app logic, UI, Android build            |
| 3D assets    | Unreal Engine 5 / RealityScan | Karu + Fitch high-fidelity renders, then baked to sprites |
| Animation    | Spine 2D                   | Fluid anime-style bone animation for Karu    |
| Art style    | MapleStory / anime         | Vibrant 2D sprite art, exaggerated expressions |
| Backend      | Firebase (Firestore + Auth)| User data, rival sync, community feed        |

## Project Structure

```
unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── PlayerData.cs      ← save/load persistent data
│   │   │   ├── XPSystem.cs        ← XP, leveling, events
│   │   │   └── PetController.cs   ← Karu animations & mood
│   │   ├── Systems/
│   │   │   ├── TaskManager.cs     ← daily quests
│   │   │   ├── JournalSystem.cs   ← daily journal entries
│   │   │   └── RivalSystem.cs     ← Fitch rival AI + sync
│   │   └── UI/                    ← (to be built)
│   ├── Prefabs/                   ← reusable UI/game objects
│   ├── Sprites/
│   │   ├── Characters/            ← Karu & Fitch sprite sheets
│   │   └── UI/                    ← buttons, panels, icons
│   └── Audio/                     ← MapleStory-style BGM + SFX
└── ProjectSettings/
```

## Art Pipeline (Unreal Engine → Unity)

1. **Model** Karu and Fitch in Unreal Engine 5 using MetaHuman or custom rigged model
2. **Render** high-res character frames with Unreal's path tracer
3. **Export** sprite sheets at 2x / 3x resolution
4. **Rig** in Spine 2D for runtime bone animation
5. **Import** Spine skeleton into Unity via the Spine-Unity runtime

## Play Store Roadmap

- [ ] Phase 1 — Web prototype complete (done ✓)
- [ ] Phase 2 — Unity project scaffolded (done ✓)
- [ ] Phase 3 — Karu & Fitch character art commissioned
- [ ] Phase 4 — Core gameplay loop (tasks, XP, pet) in Unity
- [ ] Phase 5 — Firebase backend integration
- [ ] Phase 6 — Community feed (Firebase + moderation)
- [ ] Phase 7 — Android build + internal testing track
- [ ] Phase 8 — Play Store open beta
- [ ] Phase 9 — iOS App Store submission

## Build Instructions (once Unity project is initialized)

```bash
# Open Unity Hub → Add project → select this folder
# Set target platform: Android
# Min SDK: Android 8.0 (API 26)
# Target SDK: Android 14 (API 34)
# Scripting backend: IL2CPP
# Build → Generate Gradle Project
```
