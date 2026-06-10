# Sparq Unity — Setup

> **For new devs joining mid-project.** The project is past the original
> scaffold phase, but the install / Unity-Hub / package steps below still
> apply. You can skip the Asset-Store shopping section — the
> [`Assets/`](Assets/) folder already has ~120 packs in it; see [ASSETS.md](ASSETS.md)
> for what's used.
>
> If you're a tester, not a dev, you don't need this file. See
> [TESTING.md](TESTING.md) instead.

**Total time**: 30–60 min for a returning Unity dev, 2–3 hours from a cold start.

---

## Step 1: Install Unity Hub + Unity Editor (30 min)

1. Download **Unity Hub** from https://unity.com/download
2. Install it. Sign in with your existing Unity account.
3. In Unity Hub, go to **Installs** tab → click **Install Editor**
4. Pick **Unity 6 LTS** (or 2022.3 LTS if 6 isn't listed)
5. In "Add modules", check these boxes:
   - ✅ **Android Build Support**
     - ✅ OpenJDK
     - ✅ Android SDK & NDK Tools
   - ✅ **iOS Build Support** (optional, for later)
   - ✅ **Documentation**
6. Click **Install**. Takes 15-30 min.

---

## Step 2: Open the existing Sparq Unity scaffold (5 min)

1. Unity Hub → **Projects** tab → **Open** → **Add project from disk**
2. Navigate to: `C:\Users\angya\OneDrive\Desktop\tech\sparq\unity`
3. Select that folder, click **Open**
4. Unity will say "project version is different" → click **Open with current version**
5. First-time open takes 2-5 minutes while Unity imports everything
6. If asked to upgrade project → accept

You should see:
- `Assets/Scripts/` folder with 9 `.cs` files already there (our scaffold)
- Empty `Scenes/` folder (we'll fill this)
- Empty scene opened (bg is dark blue-grey)

---

## Step 3: Unity 101 crash course (1 hour — watch while it installs)

You don't need to master Unity — just know **how to navigate**. Watch one of these:

**Fastest (20 min):**
https://www.youtube.com/watch?v=pwZpJzpE2lQ — "Learn Unity in 17 minutes"

**More thorough (1 hour):**
https://learn.unity.com/tutorial/unity-editor-fundamentals — free official

**What you MUST be able to do after watching:**
- Open a scene
- Drag a prefab into the scene
- Hit Play / Stop
- Find a script in the Project tab
- Attach a script to a GameObject
- Change values in the Inspector
- Build for Android (we'll cover this together)

**What you DON'T need to know:**
- Shader code ← I write this
- Animator state machines ← I configure via code
- Lighting, physics ← not used in our 2D game
- Complex C# ← you can read it; I write it

---

## Step 4: Buy the starter asset pack (1 hour shopping)

Open **Window → Asset Store** in Unity (or use website: https://assetstore.unity.com).

### Must-buy before Week 1 (~$150 total)

Search these exact terms — pick whichever looks closest to the mood we want. I'll verify compatibility.

| Asset | Search term | Budget |
|-------|-------------|--------|
| **Pixel RPG UI Kit** | "Fantasy RPG GUI" or "Pixel UI Kit" | $20-40 |
| **Character animations** | "2D pixel hero animated" (pick one with idle/attack/hit/death) | $15-30 |
| **Enemy pack** | "Pixel monster pack" (need 12+ unique enemies) | $20-50 |
| **Particle VFX** | "Pixel VFX 2D" (sparkles, hits, explosions) | $15-30 |
| **Chiptune music** | "8-bit music pack" or "retro game music" (5-10 tracks) | $15-25 |
| **Sound FX library** | "Retro game sfx" or "RPG sound effects" | $10-25 |
| **DOTween Pro** | "DOTween Pro" | $15 (or free "DOTween" basic version) |

### Free essentials (install after the buys)
Free from Unity itself:
- Window → Package Manager → Unity Registry → install:
  - ✅ **Input System** (for touch)
  - ✅ **TextMeshPro** (usually auto-installed)
  - ✅ **2D Sprite**
  - ✅ **2D Animation**
  - ✅ **Cinemachine** (for camera polish)
  - ✅ **Universal RP** (for URP 2D)

### Firebase SDK (free, install last)
1. Download from https://firebase.google.com/docs/unity/setup
2. Unzip → import the `.unitypackage` files:
   - `FirebaseAuth.unitypackage`
   - `FirebaseFirestore.unitypackage`
3. Unity will prompt "Resolve dependencies" → click YES
4. When done, drop your existing `google-services.json` into `Assets/` folder

---

## Step 5: Send me screenshots

When you finish, screenshot:
1. Unity Hub showing the installed Editor version
2. The Sparq project open in Unity
3. Package Manager showing installed packages
4. Asset Store downloads folder

I'll verify everything's set up correctly and generate Week 1 code.

---

## What to expect after setup

Monday = setup day. By Tuesday we'll have:
- A **Boot scene** that checks auth
- An **Auth scene** matching your current login screen
- Firebase Auth working end-to-end
- First build running on your phone

You won't need to write any C#. You'll be dragging things, hitting Play, and telling me how it looks.

## If you get stuck

- **Unity won't open the project** → try upgrading project; if it crashes, we start fresh (I'll scaffold a new empty one)
- **Package resolution errors** → screenshot the console, send to me
- **Can't find an asset on the store** → send me the category link, I'll recommend a specific one
- **Don't understand what Unity is asking** → take a screenshot of the popup/dialog and ask

**Nothing is urgent.** Take all day Monday if needed. Tuesday we code.
