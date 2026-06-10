# Player Profile Bar + Avatar Upload — Setup Guide

End-to-end wiring for the Top Heroes-style top banner with avatar / name / level / XP, plus the avatar picker with custom upload + moderation.

**Scope split:**
- **Path A (today):** local-only avatar — pic stays on the player's device. Ships immediately, zero infrastructure.
- **Path B (after Firestore lands):** uploaded pic moderated by Google Cloud Vision before other players see it. Spec'd and code-stubbed; uncomment + deploy when ready.

---

## Files shipped

| File | Purpose | Wiring effort |
|---|---|---|
| `Assets/Scripts/Core/PlayerData.cs` | Avatar fields added (`avatarPresetId`, `customAvatarPath`, `moderationState`, etc.) | None — automatic |
| `Assets/Scripts/Core/PresetAvatars.cs` | Curated registry of preset avatars + load helpers | Drop sprites in `Assets/Resources/Avatars/` matching the `resourcePath` field of each preset |
| `Assets/Scripts/Core/AvatarUploadService.cs` | File picker + validation + local save (Path A); cloud upload stub (Path B) | Mobile: install [UnityNativeGallery](https://github.com/yasirkula/UnityNativeGallery) and enable the `#elif UNITY_ANDROID || UNITY_IOS` block |
| `Assets/Scripts/UI/PlayerProfileBar.cs` | The top banner component | Drop on a banner GameObject, wire 6 Inspector refs |
| `Assets/Scripts/UI/AvatarPickerPanel.cs` | Picker grid + upload button modal | Build a prefab at `Resources/UI/AvatarPickerPanel.prefab`, wire refs |
| `cloud-functions/moderateAvatar.js` | Firebase Cloud Function — Vision SafeSearch + Firestore writeback | Path B only — see below |
| `cloud-functions/storage.rules` | Firebase Storage security rules | Path B only |
| `cloud-functions/firestore.rules` | Firestore rules for users + moderation subcollection | Path B only |
| `cloud-functions/package.json` | npm deps for the function | Path B only |

---

## Path A — Unity wiring (ship today)

### 1. Drop the preset sprites

The registry in [PresetAvatars.cs](./PresetAvatars.cs) expects sprites at these Resources paths (so add `Resources/Avatars/<name>.png` for each):

```
Resources/Avatars/default.png
Resources/Avatars/knight_01.png
Resources/Avatars/archer_01.png
Resources/Avatars/mage_01.png
Resources/Avatars/assassin_01.png
Resources/Avatars/karu_pink.png
Resources/Avatars/mochi_blue.png
Resources/Avatars/flame.png
Resources/Avatars/crystal.png
Resources/Avatars/star.png
Resources/Avatars/heart.png
Resources/Avatars/gold_crown.png
Resources/Avatars/dragon.png
Resources/Avatars/legend.png
```

Recommended: square, **256×256 minimum** (preferably 512×512), transparent background, hand-cropped to match the chibi style. Set Texture Type → Sprite (2D and UI) on each import.

You can use placeholder colored squares for v1 and swap in art before alpha.

### 2. Build the PlayerProfileBar GameObject

In your home scene Canvas (probably `HomeLobbyPanel`'s instantiated Lobby.prefab), build a horizontal banner ~50px tall under the resource header. Inside it:

```
PlayerProfileBar  (RectTransform, anchored stretch-x top)
├── Button (full-bar tap target — opens picker)
├── AvatarImage (Image, 40px circle)
├── NameLabel (TMP_Text — bold 14pt, white)
├── LevelLabel (TMP_Text — 10pt grey)
├── XpFill (Image, type=Filled, method=Horizontal)
├── XpLabel (TMP_Text — optional "3,240 / 5,000")
└── PendingBadge (small GameObject — circle with "..." or hourglass)
```

Attach `PlayerProfileBar.cs` to the root. In the Inspector:
- `Avatar Image` → AvatarImage
- `Name Label` → NameLabel
- `Level Label` → LevelLabel
- `Xp Fill` → XpFill
- `Xp Label` → XpLabel (or leave empty)
- `Pending Badge` → PendingBadge (deactivate by default)
- `Root Button` → the Button on the root
- `Default Avatar` → drag `Avatars/default.png` for the fallback
- `Open Picker On Tap` → ☑

### 3. Build the AvatarPickerPanel prefab

Create `Resources/UI/AvatarPickerPanel.prefab` — a modal panel with:

```
AvatarPickerPanel  (Canvas-child; modal background)
├── CloseButton (Button — top-right "✕")
├── ScrollView
│   └── Viewport
│       └── Content (with GridLayoutGroup, cell 100×100, 4–5 cols)
├── UploadButton (Button — "Upload photo" with a camera icon)
└── StatusLabel (TMP_Text — error / pending messages)
```

And a `PresetCell` child prefab:

```
PresetCell  (Button)
├── Image (the avatar sprite)
├── Label (TMP_Text — preset name OR "Lv N" if locked)
└── Lock (GameObject — locked overlay with padlock icon, inactive by default)
```

Attach `AvatarPickerPanel.cs` to the panel root. In the Inspector:
- `Grid Container` → Content
- `Preset Cell Prefab` → the PresetCell prefab
- `Close Button` → CloseButton
- `Upload Button` → UploadButton
- `Status Label` → StatusLabel
- `Allow Custom Upload` → ☑ (uncheck for under-13 builds or COPPA-restricted regions)

### 4. Install a file picker (Android / iOS only)

For mobile builds, drop in [UnityNativeGallery](https://github.com/yasirkula/UnityNativeGallery) via the Package Manager (Git URL: `https://github.com/yasirkula/UnityNativeGallery.git`), then in `AvatarUploadService.PickFile()` uncomment the `NativeGallery.GetImageFromGallery(...)` block.

The Editor uses Unity's built-in `EditorUtility.OpenFilePanel` so you can test without any plugin.

### 5. Test the loop

1. Run the scene in the Editor
2. Tap the profile bar → AvatarPickerPanel opens
3. Pick a preset → bar refreshes, panel closes
4. Tap "Upload photo" → file picker → choose a PNG/JPG → bar refreshes with your photo
5. Profile bar shows custom pic; `moderationState` is `"local"`; pic file lives at `Application.persistentDataPath/avatar_<uuid>.png`
6. Confirm save persists: stop play, restart, your custom pic should still load

### COPPA / safety note (Path A)

In local-only mode, custom pics never reach a server — only the owner sees them on their own device. **No moderation is required by COPPA/COPPA-equivalent rules** because there is no public-facing image surface. That said:
- When you add age gating to Sparq, set `AvatarPickerPanel.allowCustomUpload = false` for under-13 accounts as a defense-in-depth signal (and so the upload button is hidden — easier to support).
- The file is saved with EXIF stripped (we re-encode as PNG via `Texture2D.EncodeToPNG`), so location metadata never persists.

---

## Path B — Cloud upload + moderation (deploy when Firestore lands)

You'll know you're ready for Path B when:
- Firestore Unity SDK is installed and `SaveService` mirrors PlayerData to a `users/{uid}` doc
- The player has a Firebase Auth uid (anonymous auth is fine for v1)

Then:

### 1. Deploy the Cloud Functions

```bash
cd cloud-functions
npm install
firebase deploy --only functions:moderateAvatar,functions:resolveAvatarReview
firebase deploy --only storage:rules,firestore:rules
```

In GCP console, **enable the Cloud Vision API** for the Firebase project (one-time).

### 2. Wire the upload destination in Unity

In `AvatarUploadService.cs`, find the `CloudUpload` method and replace the stub with:

```csharp
private static async void CloudUpload(string localPath, Action<Result> onComplete)
{
    string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
    if (string.IsNullOrEmpty(userId)) {
        onComplete?.Invoke(new Result { status = Status.Error, error = "Not signed in" });
        return;
    }
    string uuid = System.Guid.NewGuid().ToString("N");
    string storagePath = $"avatars/{userId}/pending/{uuid}.png";
    var storageRef = Firebase.Storage.FirebaseStorage.DefaultInstance.RootReference.Child(storagePath);
    try {
        await storageRef.PutFileAsync($"file://{localPath}");
        onComplete?.Invoke(new Result { status = Status.Success, localPath = localPath, isLocalOnly = false });
    } catch (Exception e) {
        onComplete?.Invoke(new Result { status = Status.Error, error = e.Message });
    }
}
```

Also flip `cloudAvailable = true` in `PickAndUpload`.

### 3. Add the moderation listener

Create `Assets/Scripts/Core/AvatarModerationListener.cs`:

```csharp
using System;
using Firebase.Firestore;
using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Subscribes to users/{uid}/avatarModeration/current and pushes state
    /// changes into PlayerData. Mount once on app start.
    /// </summary>
    public class AvatarModerationListener : MonoBehaviour
    {
        ListenerRegistration _listener;

        void Start()
        {
            string uid = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(uid)) return;
            var doc = FirebaseFirestore.DefaultInstance
                .Collection("users").Document(uid)
                .Collection("avatarModeration").Document("current");
            _listener = doc.Listen(snap => {
                if (!snap.Exists) return;
                string state = snap.GetValue<string>("state");
                var d = SaveService.Data;
                if (d == null) return;
                d.moderationState = state ?? "pending";
                if (state == "rejected") {
                    d.customAvatarPath = "";   // revert to preset
                }
                SaveService.Save();
            });
        }

        void OnDestroy() { _listener?.Stop(); }
    }
}
```

### 4. Build the moderator queue UI (later)

When you have moderators, build an admin tool that:
- Queries `db.collectionGroup('avatarModeration').where('state', '==', 'pending_review')`
- Shows the pending image (fetch from the `pendingPath` field in Storage)
- On approve/reject, calls the `resolveAvatarReview` HTTPS callable with the userId + decision

For solo-dev launch, a 10-line Firebase Functions HTTP endpoint that takes a userId via URL param and returns the pending image inline + two buttons (approve/reject) is enough. Scale to a real dashboard when volume justifies it.

### Cost notes

- Cloud Vision SafeSearch: **1,000 free units/month**, then $1.50 per 1,000
- Firebase Storage: 5 GB free, then $0.026/GB
- Cloud Functions invocations: 2M free/month
- At 10K DAU × 5% monthly avatar change rate → ~500 Vision calls + 500 storage objects + 500 function invocations per month → **free tier covers it through ~20K DAU**

### Children + COPPA in Path B

When Path B is live, the rule becomes hard:
- Under-13 accounts must have `AvatarPickerPanel.allowCustomUpload = false`
- Cloud Function should reject uploads from age-tier=child accounts at the storage rules layer too (defense in depth) — add an `age_tier` field to the user doc and check it in `storage.rules`:

```
allow write: if request.auth != null
              && request.auth.uid == userId
              && get(/databases/(default)/documents/users/$(userId)).data.age_tier != 'child'
              && request.resource.size < 5 * 1024 * 1024
              && request.resource.contentType.matches('image/(png|jpe?g)');
```

---

## What changes when both paths are live

| Player action | Path A behavior | Path B behavior |
|---|---|---|
| Pick a preset | Saved locally + visible to others (in leaderboards etc.) | Same |
| Upload custom photo | Saved locally + only you see it on your device | Saved locally, uploaded to Storage, moderation pending — others see your preset while pending; your custom pic with a small "in review" badge for the owner |
| Photo passes review | n/a | Approved URL written to user doc; everyone sees the photo |
| Photo fails review | n/a | Custom path cleared locally; reverts to preset; player sees a "photo can't be used" toast |
| Reset to preset later | Tap the preset in picker | Same |

---

## Open questions for future-you

- **Frame / badge overlays?** Top Heroes adds a VIP frame around the avatar that scales with rank/level. The `PlayerProfileBar` is structured so you can add a `Frame` Image sibling to AvatarImage when you want this.
- **Animated avatars?** Path A loads static PNG. If you ever want APNG / Lottie / Spine, route through a different loader. Out of scope for v1.
- **Friend list previews?** When social lands, friends list shows `avatarUrl` (denormalized on the user doc by the Cloud Function on approval) — no per-friend Firestore read needed.

---

*v1 — 2026-05-16. Ship Path A now; uncomment Path B when Firestore is wired.*
