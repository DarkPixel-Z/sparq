# First Android Build — Walkthrough

This is the playbook for getting Sparq onto a real Android phone for the
first time. ~1 hour from cold start if Unity Android Build Support is
already installed; ~3 hours if you have to install the Android module too.

## Pre-flight (what I already changed)

Direct edits I made to `ProjectSettings/`:

- `EditorBuildSettings.asset` — added `Boot.unity` and `Home.unity` to the
  scenes-in-build list. Without this, the build would have nothing to run.
- `ProjectSettings.asset:productName` — `unity` → `Sparq`
- `ProjectSettings.asset:companyName` — `DefaultCompany` → `Sparq`
  (change to your real company / LLC name before any store submission)
- `ProjectSettings.asset:AndroidMinSdkVersion` — `25` → `26` (Android 8.0)

These show up immediately when you open Unity. The rest below has to be
done through the Unity UI because the underlying YAML uses per-platform
maps that are safer to set through the Editor.

---

## Step 1: Confirm Android Build Support is installed (5 min)

1. Open **Unity Hub** → **Installs** tab.
2. Click the gear next to **Unity 6000.4.3f1** (or whichever LTS is shown).
3. **Add modules**. Make sure these are checked:
   - ✅ Android Build Support
   - ✅ OpenJDK
   - ✅ Android SDK & NDK Tools
4. If they were unchecked, install. Takes 15-30 min.

## Step 2: Open the project (2 min)

1. Unity Hub → **Projects** → open this folder.
2. First import after my YAML edits takes a few minutes — Unity reads the
   new scenes-in-build list and refreshes.
3. Watch the Console (Window → General → Console). Look for compile
   errors from the recent round-2 fixes (JournalPanel, QuestManager,
   Progression, CloudSaveService). If anything red appears, stop here
   and fix the script.

## Step 3: Player Settings — fill in the Android identity (10 min)

**File → Build Profiles** (or **File → Build Settings** in older flows).

1. **Platform**: select Android, click **Switch Platform** if not already
   active. First switch takes a few minutes (asset reimport for Android).
2. Click **Player Settings…** (bottom-left of Build Profiles).

In **Player → Settings for Android**:

### Identification
- **Package Name**: `com.<yourcompany>.sparq` — must be unique and
  reverse-DNS. Once you publish to Play Store under this name, you can
  never change it. Pick something you'll be happy with for years.
  Suggestion: `com.sparq.app` if no company yet.
- **Version**: `1.0` (already set)
- **Bundle Version Code**: `1` (already set; increment for each Play Store upload)
- **Minimum API Level**: `Android 8.0 (API 26)` (already set via YAML)
- **Target API Level**: `Android 14.0 (API 34)` — Google Play requires this for new submissions in 2025+
- **Install Location**: `Automatic`

### Other Settings → Configuration
- **Scripting Backend**: `IL2CPP`
  (Mono works for sideloading but Play Store requires IL2CPP + 64-bit)
- **Api Compatibility Level**: `.NET Standard 2.1`
- **Target Architectures**:
  - ✅ ARMv7
  - ✅ ARM64
  (both required for Play Store; ARM64 alone won't install on older test phones)

### Other Settings → Rendering
- **Color Space**: `Linear` (matches URP)
- **Auto Graphics API**: ON, with Vulkan + OpenGLES3 as fallbacks

### Resolution and Presentation
- **Default Orientation**: `Portrait`
- **Allowed Orientations**: Portrait + Portrait Upside Down only
  (rotation isn't tested; don't ship landscape)

## Step 4: Keystore (5 min)

For sideloaded builds, Unity will use its built-in debug keystore by
default — that's fine for a closed friends-and-family test. For Play
Store you'll need a real signing keystore later.

For now, just confirm:

**Project Settings → Player → Android → Publishing Settings**
- Leave **Custom Keystore** unchecked. (Unity's debug keystore signs APKs
  that install fine on any unsecured Android device.)

When you're ready for Play Store, come back here and create a real keystore.
Back it up to two places — losing it means you can never push another update
under the same package name.

## Step 5: Set the right Boot scene first (1 min)

**Build Profiles → Scene List** should show:
```
☑ 0  Scenes/Boot.unity
☑ 1  Scenes/Home.unity
```

If the order is reversed, drag `Boot.unity` to the top. The scene at index
0 is what loads first.

## Step 6: Plug in a phone (5 min)

1. On the Android phone: **Settings → About → tap Build Number 7 times**
   to enable Developer Options.
2. **Settings → Developer Options → USB Debugging**: ON.
3. Plug the phone into the computer with a USB cable. Phone prompts to
   trust the computer — accept.
4. In Unity, **Build Profiles → Run Device** dropdown should show your
   phone. If it shows "Default device", click **Refresh** next to the
   dropdown.

If the phone doesn't appear:
- Check the cable supports data (some "charging only" cables don't).
- On Windows, install the OEM USB driver for your phone (Samsung, Pixel,
  etc. each have one).
- `adb devices` from a command line should list the phone too.

## Step 7: Build And Run (15-45 min on first build)

Click **Build And Run**. Unity will:

1. Prompt for an output `.apk` path. Use `Builds/sparq-v1.0.apk`.
2. Compile (slow first time, IL2CPP cross-compile to ARM).
3. Package the APK.
4. Install on the connected phone.
5. Launch the app.

First IL2CPP build can take 30-45 minutes. Subsequent incremental builds
are 2-5 min. Watch the bottom-right of Unity for progress.

If the build fails, read the **Console** carefully. Common causes:

| Error                                   | Fix                                            |
| --------------------------------------- | ---------------------------------------------- |
| "Android SDK not found"                 | Edit → Preferences → External Tools → set Android SDK path |
| "Unable to merge AndroidManifest"       | Usually a plugin (Firebase, Notifications). Disable optional plugins if `SPARQ_CLOUDSAVE` / `SPARQ_NOTIFICATIONS` are off. |
| "Gradle build failed"                   | Open the gradle output — usually a missing dependency or proguard issue |
| Out of memory                           | Close Chrome / heavy apps. IL2CPP wants 8GB+. |
| "Editor compile errors"                 | Fix the C# errors first; the build won't even start if scripts don't compile. |

## Step 8: First-launch smoke test on the phone (10 min)

App should:

1. Launch into Boot.unity (just a Sparq logo / loading screen, fast).
2. Transition to Home.unity.
3. Show the WelcomePanel (FTUE) on the very first launch.
4. After FTUE, drop into the lobby.

What to verify in the first 5 minutes:

- ✅ FTUE renders without missing sprites (purple/magenta placeholders = sprite path broken)
- ✅ Lobby loads — pet visible, top bar visible, quest list visible
- ✅ Tap a mood crystal → XP floater appears, level bar moves
- ✅ Go to World → Battle one stage → win
- ✅ Background-and-foreground the app → state persists
- ✅ Open Settings → tap Version row 7× → red TESTER TOOLS panel appears
- ✅ Reset Save → app does NOT crash

If any of these fail, screenshot the phone + capture logcat:

```
adb logcat -d -s Unity:V > sparq-log.txt
```

(Run with the phone plugged in. The output includes every `Debug.Log`
from the app since boot.)

## Step 9: Save the APK + capture a known-good baseline

Once it works on YOUR phone:

1. Copy `Builds/sparq-v1.0.apk` somewhere stable (a `Builds/` folder in
   the repo, or Drive).
2. Note the exact commit hash you built from: `git rev-parse HEAD`.
3. That commit + that APK is your **baseline**. If a tester reports a
   bug, you want to be able to reproduce on the exact build they have.

## Step 10: Distribute to testers

See [TESTING.md](TESTING.md) for the playbook. Short version:

1. Upload the APK to Drive / Discord / email.
2. Send testers the install instructions (in TESTING.md "For Testers" section).
3. Spin up the feedback Google Form.
4. Link the form from inside the debug menu before sending.

---

## Things that probably won't work first try (and what to do)

### "AndroidManifest merger failed: SPARQ_NOTIFICATIONS plugin"

The notification scaffold is gated behind `SPARQ_NOTIFICATIONS` define. If
the plugin isn't installed and the define is OFF, this shouldn't trigger.
Confirm:

- **Project Settings → Player → Other Settings → Scripting Define Symbols**
- Should NOT contain `SPARQ_NOTIFICATIONS` or `SPARQ_CLOUDSAVE` for closed test.

### "Resource not found: Assets/_Recovery/*.unity"

The `_Recovery/` folder has 7 old scene backups. They shouldn't end up in
the build (we only added Boot/Home to scenes-in-build) but if they do,
delete them. They're auto-recovered scene snapshots from old crash sessions.

### "DLL not found: FirebaseAuth.dll" or similar

If Firebase Unity SDK was ever imported, leftover DLLs in `Assets/Plugins/`
will fail the build when `SPARQ_CLOUDSAVE` is off but Firebase plugins are
present. Either:
- Remove the Firebase plugins from `Assets/`, or
- Flip `SPARQ_CLOUDSAVE` on (adds dependencies but takes the path).

For closed test, removing is the cleanest move.

### APK installs but crashes immediately on launch

Most common cause: missing scene 0 or wrong scene order. Reopen Build
Profiles, confirm Boot.unity is index 0 and enabled.

Second most common: a `Resources.Load` call hitting a path that exists in
editor but wasn't included in the build. Capture logcat — the
`NullReferenceException` will point at the offending file.

---

## What success looks like

✅ APK installs cleanly on the test phone.
✅ App launches into Boot → Home in under 5 seconds.
✅ FTUE works.
✅ The full loop (mood log → quest → battle → level up → save → quit → relaunch → save persists) works.
✅ Debug menu opens on 7-tap Version.

That's the minimum for ship-to-testers. Anything more polished is
gravy for round 2.
