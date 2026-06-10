using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Runtime upgrade for the home screen's Karu and Mochi sprites — swaps the
    /// existing scene SpriteRenderer artwork to chibi characters from the new
    /// asset packs (BattleOfHeroes / Azure Will-o'-Wisp / BATTY BUDDIES).
    ///
    /// Critical bit: BoH source sprites are HUGE (1024–2048 px) compared to the
    /// existing Karu sprite (~256 px), so we also auto-scale the GameObject's
    /// transform.localScale to keep the visible size roughly the same as before.
    ///
    /// Runs once via Ensure() — safe to call multiple times.
    /// Call from BottomNavBar / HomeNavBar startup.
    /// </summary>
    public static class HomeChibiUpgrade
    {
        // Reference visual size we want the swapped sprite to occupy in world
        // units (matches the original Karu's footprint roughly).
        private const float TARGET_KARU_HEIGHT_UNITS = 6.8f;    // was 4.5 — bumped per user request (much bigger)
        private const float TARGET_MOCHI_HEIGHT_UNITS = 4.0f;   // smaller than Karu — gives the "set back" perspective
        // The Knight's 1800×980 idle frame is auto-cropped to the exact figure
        // (via alpha bounds), so this is the on-screen height of the knight
        // HIMSELF — not the whole padded sprite the 900×900 classes scale.
        private const float KNIGHT_FIGURE_HEIGHT_UNITS = 6.0f;
        // Offset RELATIVE TO KARU's position (not added to Mochi's baked pos).
        // Older editor scripts hard-coded Mochi's position so we ignore that and anchor to Karu instead.
        private static readonly Vector3 MOCHI_REL_TO_KARU = new Vector3(2.0f, 0.6f, 1.5f);    // BESIDE Karu (right) at his hand level, set back in z
        private const int   MOCHI_SORT_ORDER = -5;                                        // renders behind Karu

        // Karu candidate — Valkyrie matches Karu best (chibi female warrior with wings)
        private const string KARU_SPRITE =
            "Assets/BattleOfHeroes/Characters/Valkyrie_1/PNG/PNG Sequences/Idle/0_Valkyrie_Idle_000.png";
        // Fallbacks
        private static readonly string[] KARU_FALLBACKS = {
            "Assets/BattleOfHeroes/Characters/Greek Warrior/PNG/PNG Sequences/Idle/Idle_000.png",
            "Assets/BattleOfHeroes/Characters/Frontier Defender Spartan Knight/PNG/PNG Sequences/Idle/Idle_000.png",
        };
        private const string MOCHI_SPRITE = "Assets/Azure Will-o’-Wisp/Azure Will-o’-Wisp.png";

        private static bool _done;

        // Track avatar Image references found this refresh — used to equalize
        // Karu and Drip slot sizes at the end. Cleared on each Ensure().
        private static readonly System.Collections.Generic.List<UnityEngine.UI.Image> _heroAvatars
            = new System.Collections.Generic.List<UnityEngine.UI.Image>();
        private static readonly System.Collections.Generic.List<UnityEngine.UI.Image> _petAvatars
            = new System.Collections.Generic.List<UnityEngine.UI.Image>();

        // Una is the tutorial/help character. The hero picker now claims the
        // Time Keeper Mage chibi for the Mage class, so Una moves to a
        // Citizen-Women chibi (non-combatant, fits the mentor role).
        // GameObject in the scene is still named "Una" — only the sprite swaps.
        private const string UNA_SPRITE =
            "Assets/WomenChibi/Citizen-Women_1/PNG/PNG Sequences/Idle/0_Citizen-Women_Idle_000.png";

        public static void Ensure()
        {
            // ════════════════════════════════════════════════════════════════
            // The existing home scene is being REPLACED by Layer Lab Lobby.
            // We stop fighting the scene here and let HomeLobbyPanel render
            // on top of everything.
            //
            //   Old: nudge Karu, lock Blip, perch Pecky, scale chase, …
            //   New: hide the broken scene cast, show the Lobby prefab.
            // ════════════════════════════════════════════════════════════════

            if (!_done)
            {
                _done = true;
                // Keep the help icon (it's our own injected element and useful)
                SpawnHelpIcon();
                // Hide the leftover scene actors that the Lobby prefab replaces.
                HideOldHomeCast();
            }

            // TEMP FTUE BYPASS — Android UGUI Button.onClick isn't firing in
            // the build (InputField touches DO register, but Buttons don't),
            // which strands users on AuthPanel + HeroSelectPanel with no way
            // to advance. Until we find the root cause of touch-vs-click,
            // set defaults and drop straight into the lobby so the rest of
            // the closed test can proceed.
            var data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                bool changed = false;
                if (string.IsNullOrEmpty(data.playerName) || data.playerName == "Sparq User")
                { data.playerName = "Tester"; changed = true; }
                if (string.IsNullOrEmpty(data.heroClass))
                { data.heroClass = "knight"; changed = true; }
                if (changed) try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
            }
            try { Sparq.UI.HomeLobbyPanel.Show(); }
            catch (System.Exception ex)
            { Debug.LogError($"[HomeChibiUpgrade] Direct lobby show failed: {ex}"); }
        }

        // Disable the existing chibi/HUD GameObjects so they don't bleed
        // through the new Lobby canvas. Mochi (Blip) and Pecky stay because
        // we re-spawn them as UI Images inside the Lobby canvas next to Karu.
        private static void HideOldHomeCast()
        {
            string[] hideNames = {
                "Karu",   // hero is shown via the prefab's "Character" Image
                "PlayerHUD", "GameTitle", "HomeNavButtons",
                "Loot_Chest", "Loot_GoldenLeaf", "Loot_EasterEgg", "Loot_Butterfly",
                // Mochi (Blip) and Stage_4_Pecky are hidden too — UI versions spawn instead
                "Mochi", "Stage_4_Pecky", "Pecky",
            };
            int hidden = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                string n = go.name ?? "";
                bool match = false;
                foreach (var hn in hideNames) if (n == hn) { match = true; break; }
                if (!match) continue;
                go.SetActive(false);
                hidden++;
                Debug.Log($"[HomeChibiUpgrade] Hidden legacy '{n}' (replaced by Lobby).");
            }
            Debug.Log($"[HomeChibiUpgrade] Hid {hidden} legacy home GameObjects.");
        }

        private static MonoBehaviour _runner;
        public class RunnerMB : MonoBehaviour {}
        private static System.Collections.IEnumerator EnforcePositionsForOneSecond()
        {
            // Re-apply positions for 60 frames (~1 second) so any startup script
            // that nudges Karu/Pecky after our pass gets overridden.
            for (int i = 0; i < 60; i++)
            {
                PositionHomeCast();
                yield return null;
            }
            Debug.Log("[HomeChibiUpgrade] Position enforcement coroutine done.");
        }

        // Update the top-right HUD card so Karu's avatar shows the picked hero class
        // sprite directly (not relying on the runtime camera capture which fails when
        // Karu's scene sprite isn't ready) and the pet name reflects PetService data.
        private static void RefreshHudCard()
        {
            #if UNITY_EDITOR
            // Reset tracking — equalize step at the end consumes these.
            _heroAvatars.Clear();
            _petAvatars.Clear();

            // ── 1. Pull the active pet's nickname (Roster() runs the Wisp→Drip migration) ──
            string petName = "Drip";
            try
            {
                var active = Sparq.Systems.PetService.Active();
                if (active != null && !string.IsNullOrEmpty(active.nickname))
                    petName = active.nickname;
            }
            catch {}

            // Pre-load the hero class idle sprite for the Karu avatar swap (step 3)
            Sprite heroSp = null;
            var hero = Sparq.Systems.HeroClassResolver.Resolve();
            if (hero != null && !string.IsNullOrEmpty(hero.idleBase))
                heroSp = LoadSprite(hero.idleBase + "000.png");

            // Track what we found, so the avatar-fix step can use the same anchors.
            var labelsKaru = new System.Collections.Generic.List<TMPro.TMP_Text>();
            var labelsWisp = new System.Collections.Generic.List<TMPro.TMP_Text>();

            int updated = 0;
            foreach (var tm in Object.FindObjectsByType<TMPro.TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tm == null) continue;
                string raw = tm.text ?? "";
                // Strip TMP rich-text and whitespace so "  <b>Wisp</b> " matches "Wisp"
                string s = StripRich(raw).Trim();

                if (s.Equals("Wisp", System.StringComparison.OrdinalIgnoreCase))
                {
                    tm.text = petName;
                    labelsWisp.Add(tm);
                    updated++;
                }
                else if (s.Equals("Karu", System.StringComparison.OrdinalIgnoreCase))
                {
                    labelsKaru.Add(tm);
                }
            }

            // ── 2. Pet-row icon — make sure it shows the active pet's species sprite ──
            try
            {
                var active = Sparq.Systems.PetService.Active();
                Sprite petSp = null;
                if (active != null)
                {
                    var sp = Sparq.Systems.PetService.FindSpecies(active.speciesId);
                    if (sp != null && !string.IsNullOrEmpty(sp.spritePath))
                        petSp = LoadSprite(sp.spritePath);
                }
                if (petSp != null)
                {
                    int petsFixed = 0;
                    foreach (var lbl in labelsWisp)
                    {
                        var img = FindLeftmostAvatarInRow(lbl);
                        if (img == null) continue;
                        img.sprite = petSp;
                        img.color = Color.white;
                        img.preserveAspect = true;
                        ResetAvatarTransform(img);   // undo any leftover scale/size from prior runs
                        petsFixed++;
                    }
                    Debug.Log($"[HomeChibiUpgrade] Pet avatar: fixed {petsFixed} HUD slot(s).");
                }
            }
            catch (System.Exception ex) { Debug.LogWarning($"[HomeChibiUpgrade] pet icon swap: {ex.Message}"); }

            // ── 3. Karu avatar — replace the blank white box with the hero idle frame ──
            //    Belt-and-suspenders: (a) disable KaruAvatarCapture so it can't keep
            //    overwriting with a blank RenderTexture snapshot, and grab its
            //    `targetAvatar` field to write directly to the EXACT correct Image.
            //    (b) Also do the label-anchored sweep as a fallback.
            if (heroSp != null)
            {
                int avatarsFixed = 0;

                // (a) disable any KaruAvatarCapture and write to its targetAvatar
                foreach (var cap in Object.FindObjectsByType<KaruAvatarCapture>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (cap == null) continue;
                    var slot = cap.targetAvatar;
                    cap.enabled = false;   // stop the capture coroutine from re-running
                    if (slot != null)
                    {
                        slot.sprite = heroSp;
                        slot.color = Color.white;
                        slot.preserveAspect = true;
                        ResetAvatarTransform(slot);   // undo any leftover scale/size from prior runs
                        avatarsFixed++;
                    }
                }

                // (b) label-anchored fallback for any HUD without a KaruAvatarCapture
                foreach (var lbl in labelsKaru)
                {
                    var img = FindLeftmostAvatarInRow(lbl);
                    if (img == null) continue;
                    if (img.sprite == heroSp) continue;   // already fixed by (a)
                    img.sprite = heroSp;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    ResetAvatarTransform(img);
                    avatarsFixed++;
                }
                Debug.Log($"[HomeChibiUpgrade] Karu avatar: fixed {avatarsFixed} HUD slot(s).");
            }

            Debug.Log($"[HomeChibiUpgrade] HUD refreshed — pet=\"{petName}\", swapped {updated} pet labels, found {labelsKaru.Count} Karu label(s).");
            #endif
        }

        // Drip's prefab slot is smaller than Karu's. After both have sprites
        // applied, force their RECT SIZE (not just localScale) to match — that
        // way the chibis read as a matched pair regardless of prefab disparity.
        // Picks the *larger* of the two as the canonical size so we never shrink
        // anything; the smaller slot grows to match.
        private static void EqualizeAvatarSizes()
        {
            if (_heroAvatars.Count == 0 || _petAvatars.Count == 0)
            {
                Debug.Log("[HomeChibiUpgrade] Equalize skipped — missing hero or pet avatar.");
                return;
            }
            // Pick canonical size: the largest sizeDelta across all hero/pet slots.
            Vector2 target = Vector2.zero;
            foreach (var img in _heroAvatars)
            {
                if (img == null || img.rectTransform == null) continue;
                var s = img.rectTransform.sizeDelta;
                if (s.x * s.y > target.x * target.y) target = s;
            }
            foreach (var img in _petAvatars)
            {
                if (img == null || img.rectTransform == null) continue;
                var s = img.rectTransform.sizeDelta;
                if (s.x * s.y > target.x * target.y) target = s;
            }
            if (target.x < 1f || target.y < 1f) return;

            // Apply to both — smaller slot grows, larger stays put.
            foreach (var img in _heroAvatars)
            {
                if (img == null || img.rectTransform == null) continue;
                img.rectTransform.sizeDelta = target;
            }
            foreach (var img in _petAvatars)
            {
                if (img == null || img.rectTransform == null) continue;
                img.rectTransform.sizeDelta = target;
            }
            Debug.Log($"[HomeChibiUpgrade] Equalized avatar sizes to {target.x}x{target.y} ({_heroAvatars.Count} hero / {_petAvatars.Count} pet).");
        }

        // Cleanup: reset any leftover scale/size bloat from previous test runs.
        // Earlier iterations applied localScale=1.55 + grew sizeDelta to "equalize"
        // — those got saved into the scene file and now persist across Plays.
        // This forces the slot back to a sane HUD-avatar size so the layout
        // looks right again.
        private const float AVATAR_BASELINE_SIZE = 80f;   // safe default for HUD avatar slots
        private const float AVATAR_BLOAT_THRESHOLD = 140f; // anything above this is a leftover bug
        private static void ResetAvatarTransform(UnityEngine.UI.Image img)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            if (rt == null) return;
            // 1) Reset localScale to identity
            if (Mathf.Abs(rt.localScale.x - 1f) > 0.005f ||
                Mathf.Abs(rt.localScale.y - 1f) > 0.005f)
            {
                rt.localScale = Vector3.one;
            }
            // 2) If sizeDelta is absurdly bloated, snap it back to baseline
            var sd = rt.sizeDelta;
            if (sd.x > AVATAR_BLOAT_THRESHOLD || sd.y > AVATAR_BLOAT_THRESHOLD)
            {
                rt.sizeDelta = new Vector2(AVATAR_BASELINE_SIZE, AVATAR_BASELINE_SIZE);
                Debug.Log($"[HomeChibiUpgrade] Reset bloated avatar slot ({sd.x}x{sd.y}) -> {AVATAR_BASELINE_SIZE}");
            }
        }

        // Strip TMP rich-text tags so "<b>Wisp</b>" reads as "Wisp" for matching.
        private static string StripRich(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            bool inTag = false;
            foreach (char ch in s)
            {
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(ch);
            }
            return sb.ToString();
        }

        // Given a name-label TMP, walk up to the row container and pick the
        // LEFTMOST Image that is reasonably square + reasonably big and isn't
        // a level pill / xp bar / background. The avatar slot in a Karu/Drip-style
        // HUD row always sits on the far-left edge of the row.
        private static UnityEngine.UI.Image FindLeftmostAvatarInRow(TMPro.TMP_Text label)
        {
            if (label == null) return null;
            // Climb to whichever ancestor contains both this label and at least
            // one square Image — that's the row container.
            Transform row = label.transform.parent;
            UnityEngine.UI.Image best = null;
            float bestX = float.PositiveInfinity;
            int hops = 0;
            while (row != null && hops < 4 && best == null)
            {
                foreach (var img in row.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (img == null) continue;
                    if (img.GetComponent<TMPro.TMP_Text>() != null) continue;
                    string n = img.gameObject.name.ToLower();
                    if (n.Contains("bar") || n.Contains("fill") || n.Contains("xp") ||
                        n.Contains("level") || n.Contains("pill") || n.Contains("badge") ||
                        n.Contains("hp") || n.Contains("frame") || n.Contains("bg") ||
                        n.Contains("background")) continue;
                    var rt = img.rectTransform;
                    if (rt == null) continue;
                    var sz = rt.rect.size;
                    if (sz.x < 40 || sz.y < 40) continue;
                    if (Mathf.Abs(sz.x - sz.y) > Mathf.Max(sz.x, sz.y) * 0.45f) continue;
                    // Also require it to be roughly the size of the row's HEIGHT —
                    // the avatar slot is row-height square, never bigger than the row.
                    var rowRT = row as RectTransform;
                    if (rowRT != null && sz.y > rowRT.rect.height * 1.3f) continue;
                    // Pick the leftmost
                    Vector3 wp = rt.position;
                    if (wp.x < bestX) { bestX = wp.x; best = img; }
                }
                if (best != null) break;
                row = row.parent;
                hops++;
            }
            return best;
        }

        // (Legacy fallback — kept for compatibility.)
        private static UnityEngine.UI.Image FindAvatarSiblingImage(TMPro.TMP_Text label)
        {
            if (label == null) return null;
            // Climb up to 3 parents — usually the avatar lives at the row level.
            Transform t = label.transform.parent;
            int hops = 0;
            while (t != null && hops < 3)
            {
                foreach (var img in t.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (img == null) continue;
                    string n = img.gameObject.name.ToLower();
                    // Skip obvious non-avatars
                    if (n.Contains("bar") || n.Contains("fill") || n.Contains("xp") ||
                        n.Contains("level") || n.Contains("pill") || n.Contains("badge") ||
                        n.Contains("hp") || n.Contains("bg") || n.Contains("frame")) continue;
                    // Skip text glyph host
                    if (img.GetComponent<TMPro.TMP_Text>() != null) continue;
                    var rt = img.rectTransform;
                    if (rt == null) continue;
                    var sz = rt.rect.size;
                    // Avatar slots are roughly square and reasonably big
                    bool squareish = Mathf.Abs(sz.x - sz.y) < Mathf.Max(sz.x, sz.y) * 0.45f;
                    bool bigEnough = sz.x >= 40 && sz.y >= 40;
                    if (!squareish || !bigEnough) continue;
                    // Prefer an Image with no sprite assigned OR a near-white tint
                    // (the symptom we're fixing is the blank white square).
                    bool blankish = img.sprite == null
                                    || (img.color.r > 0.92f && img.color.g > 0.92f && img.color.b > 0.92f);
                    if (blankish) return img;
                }
                t = t.parent;
                hops++;
            }
            // Fallback — return the first square Image regardless of color
            t = label.transform.parent;
            hops = 0;
            while (t != null && hops < 3)
            {
                foreach (var img in t.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (img == null || img.GetComponent<TMPro.TMP_Text>() != null) continue;
                    string n = img.gameObject.name.ToLower();
                    if (n.Contains("bar") || n.Contains("fill") || n.Contains("xp") ||
                        n.Contains("pill") || n.Contains("badge") || n.Contains("bg")) continue;
                    var rt = img.rectTransform; if (rt == null) continue;
                    var sz = rt.rect.size;
                    if (sz.x >= 40 && sz.y >= 40 &&
                        Mathf.Abs(sz.x - sz.y) < Mathf.Max(sz.x, sz.y) * 0.45f)
                        return img;
                }
                t = t.parent;
                hops++;
            }
            return null;
        }

        // Sweep the scene for any GameObject named like "Una*" or "Help*"
        // (except our own Sparq_HelpIcon). Catches the leftover white circle.
        private static void HideUna()
        {
            int hidden = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                string n = go.name ?? "";
                if (n == "Sparq_HelpIcon") continue;            // keep our new icon
                bool match =
                    n.StartsWith("Una")        ||
                    n.StartsWith("HelpIcon")   ||   // ← the white-circle parent from SparqUnaHelpButton.cs
                    n.StartsWith("HelpButton") ||
                    n.StartsWith("HelpUna")    ||
                    n.StartsWith("HelpHome")   ||
                    n.StartsWith("HelpRing")   ||
                    n.StartsWith("HelpCircle") ||
                    n.StartsWith("HelpDisc")   ||
                    n.Equals("Help") ||
                    n.Contains("UnaHelp")      ||
                    n.Contains("UnaBlueBox")   ||
                    n.Contains("Una_");
                if (match && go.activeSelf)
                {
                    go.SetActive(false);
                    hidden++;
                }
            }
            Debug.Log($"[HomeChibiUpgrade] Una/Help sweep: hid {hidden} GameObject(s).");
        }

        // Position the home-scene cast using CAMERA VIEWPORT coords, not world
        // coords. World-space offsets fail when the scene was authored at
        // different world origins; viewport coords ([0..1] across visible area)
        // always land where the camera shows, regardless of scene edits.
        //
        // Layout target (from a polished mobile RPG):
        //
        //    ┌──────── Top: HUD + nav (UI overlay) ───────┐
        //    │                                            │
        //    │      [Mochi center]      [Pecky on rock]   │   ← mid-third
        //    │   [Karu hero]                              │   ← lower-third
        //    │ ━━━━━━━━━━━━ ground line ━━━━━━━━━━━━━━━━ │
        //    │                                            │
        //    │            Trial card / nav                │   ← bottom UI
        //    └────────────────────────────────────────────┘
        //
        // Karu prominent (left-foreground), Mochi center, Pecky perched right.
        private static void PositionHomeCast()
        {
            // Robust camera lookup — try MainCamera tag first, then "Main Camera"
            // by name, then any active camera. Some home scenes have an untagged
            // camera, which is why Camera.main was returning null.
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGO = GameObject.Find("Main Camera");
                if (camGO != null) cam = camGO.GetComponent<Camera>();
            }
            if (cam == null && Camera.allCamerasCount > 0)
            {
                var arr = new Camera[Camera.allCamerasCount];
                Camera.GetAllCameras(arr);
                foreach (var c in arr) if (c != null && c.isActiveAndEnabled) { cam = c; break; }
            }
            if (cam == null)
            {
                Debug.LogWarning("[HomeChibiUpgrade] No camera found — falling back to absolute world coords.");
                FallbackPositionFromOrigin();
                return;
            }
            Debug.Log($"[HomeChibiUpgrade] Camera={cam.name} pos={cam.transform.position} ortho={cam.orthographic} orthoSize={cam.orthographicSize}");

            // Choose a Z plane that matches the home cast's authored depth.
            // Use Mochi's Z if available (it's reliably in the right plane),
            // else use a sensible default a few units in front of camera.
            float z = -1f;
            var mochi = GameObject.Find("Mochi");
            if (mochi != null) z = mochi.transform.position.z;
            float worldZ = Mathf.Abs(cam.transform.position.z - z);

            // Helper: viewport (vx, vy) in [0..1] -> world position at depth z.
            Vector3 V(float vx, float vy)
            {
                Vector3 w = cam.ViewportToWorldPoint(new Vector3(vx, vy, worldZ));
                w.z = z;
                return w;
            }

            // ── Karu — hero, left-foreground, prominent ──
            // Use Mochi as anchor (always visibly correct in screenshots) and
            // attach a LateUpdate position-lock so animators can't move Karu
            // away after our pass.
            GameObject karu = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name == "Karu" && go.activeInHierarchy) { karu = go; break; }
            }
            if (karu != null && mochi != null)
            {
                // Disable Animator root motion if present.
                var anim = karu.GetComponent<Animator>();
                if (anim != null && anim.applyRootMotion) anim.applyRootMotion = false;

                // Compute target world position based on Mochi's current position
                // ONCE. We snapshot Mochi's pos to skip its IdleBob/Breathing offset
                // (the live position oscillates).
                Vector3 mochiPos = mochi.transform.position;
                // Karu: directly LEFT of Blip at the SAME Y — user wants the
                // trio standing side-by-side on the platform, not stacked.
                Vector3 target = new Vector3(mochiPos.x - 0.9f, mochiPos.y, mochiPos.z - 0.1f);

                var lock1 = karu.GetComponent<HomePositionLock>();
                if (lock1 == null) lock1 = karu.AddComponent<HomePositionLock>();
                lock1.worldTarget = target;
                lock1.hasTarget = true;

                Vector3 was = karu.transform.position;
                karu.transform.position = target;
                string parentName = karu.transform.parent != null ? karu.transform.parent.name : "<root>";
                Debug.Log($"[HomeChibiUpgrade] Karu '{karu.name}' (parent={parentName}) -> " +
                          $"world=({target.x:F2},{target.y:F2}) was=({was.x:F2},{was.y:F2}) [STATIC LOCK]");
            }
            else Debug.LogWarning($"[HomeChibiUpgrade] Karu={karu!=null} mochi={mochi!=null} — can't lock Karu position.");

            // ═══════════════════════════════════════════════════════════════
            // FINAL COMPOSITION — fixed all problems in one pass
            // ═══════════════════════════════════════════════════════════════
            //
            //   Layout:
            //                                    [Pecky on rock]
            //                                          ↓
            //   ◀── Karu ── Blip ────────────── ●──────▶
            //              (visible, still)
            //
            // Camera frustum ≈ x ∈ [-2.83, +2.83] · y ∈ [-5, +5] (portrait phone)
            const float TRIO_GROUND_Y = -2.45f;
            const float KARU_X = -1.2f;
            const float BLIP_X = 0.3f;
            const float PECKY_X = 1.9f;
            const float PECKY_ROCK_Y = 0.5f;        // up on the rock formation (background ledge)

            // Fix 1: Karu — smaller scale so it doesn't eat Blip's visual space
            if (karu != null)
            {
                karu.transform.localScale = Vector3.one * 0.85f;
                Debug.Log($"[HomeChibiUpgrade] Karu scale -> 0.85");
            }

            // Fix 2: Blip — bigger scale + disable bouncing animations so it
            // stays put and is visibly larger (was 0.7 scale + bobbing script).
            if (mochi != null)
            {
                Vector3 mWas = mochi.transform.position;
                Vector3 mTarget = new Vector3(BLIP_X, TRIO_GROUND_Y, mWas.z);
                mochi.transform.position = mTarget;
                mochi.transform.localScale = Vector3.one * 1.3f;   // was 0.7

                // Disable the IdleBob and IdleBreathing scripts that made
                // Blip bounce/scale every frame (looked like it was disappearing
                // behind Karu when it bobbed up).
                int disabled = 0;
                foreach (var mb in mochi.GetComponents<MonoBehaviour>())
                {
                    if (mb == null) continue;
                    string typeName = mb.GetType().Name;
                    if (typeName == "IdleBob" || typeName == "IdleBreathing")
                    {
                        mb.enabled = false;
                        disabled++;
                    }
                }
                Debug.Log($"[HomeChibiUpgrade] Blip: pos={mTarget}, scale=1.3, disabled {disabled} bob/breathe script(s)");

                // Re-anchor Karu lock to absolute world target
                if (karu != null)
                {
                    var lk = karu.GetComponent<HomePositionLock>();
                    if (lk != null)
                    {
                        lk.worldTarget = new Vector3(KARU_X, TRIO_GROUND_Y, mTarget.z - 0.1f);
                        lk.hasTarget = true;
                        karu.transform.position = lk.worldTarget;
                    }
                }
            }

            // ── Pecky — owl, perched ON TOP of the rock ledge upper-right ──
            string[] peckyNames = { "Stage_4_Pecky", "Pecky", "PeckyOwl", "Owl" };
            int peckyMoved = 0;
            foreach (var n in peckyNames)
            {
                var go = GameObject.Find(n);
                if (go == null || mochi == null) continue;

                Vector3 mochiPos = mochi.transform.position;
                // Pecky on the actual rock formation (background ledge that
                // catches the light beam). Was at -1.6 (still in grass), now
                // raised to +0.5 to perch on top of the rocks.
                Vector3 target = new Vector3(1.9f, 0.5f, mochiPos.z - 0.1f);

                var lock2 = go.GetComponent<HomePositionLock>();
                if (lock2 == null) lock2 = go.AddComponent<HomePositionLock>();
                lock2.worldTarget = target;
                lock2.hasTarget = true;

                Vector3 was = go.transform.position;
                go.transform.position = target;
                Debug.Log($"[HomeChibiUpgrade] '{n}' -> world=({target.x:F2},{target.y:F2}) was=({was.x:F2},{was.y:F2}) [STATIC LOCK]");
                peckyMoved++;
                break;   // only need to lock the first matching pecky
            }
            if (peckyMoved == 0)
                Debug.LogWarning("[HomeChibiUpgrade] No Pecky/Owl GameObject found.");
        }

        // Bump the font size on every TMP_Text in the top half of the canvas
        // so labels are readable. Excludes anything we explicitly created
        // (so we don't double-grow our own banners).
        private static void BumpTopLabels()
        {
            const float MIN_FONT = 28f;   // floor — anything below this gets pushed up
            int bumped = 0;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas == null || canvas.rootCanvas != canvas) continue;
                var canvasRT = canvas.transform as RectTransform;
                if (canvasRT == null) continue;
                float midY = canvasRT.rect.yMin + canvasRT.rect.height * 0.5f;

                foreach (var tm in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (tm == null) continue;
                    string n = tm.gameObject.name ?? "";
                    if (n.StartsWith("Sparq_")) continue;   // skip our own injected stuff
                    var rt = tm.rectTransform;
                    if (rt == null) continue;

                    // Top-half check
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    float bottomY = float.PositiveInfinity;
                    foreach (var c in corners)
                    {
                        Vector2 lp;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvasRT,
                            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, c),
                            canvas.worldCamera, out lp);
                        if (lp.y < bottomY) bottomY = lp.y;
                    }
                    if (bottomY < midY - 5f) continue;

                    if (tm.fontSize < MIN_FONT)
                    {
                        tm.fontSize = MIN_FONT;
                        // outlineWidth setter throws NRE when the font asset
                        // doesn't have an outline-capable material (some legacy
                        // TMP fonts in older asset packs). Wrap defensively.
                        try
                        {
                            if (tm.outlineWidth < 0.20f)
                            {
                                tm.outlineWidth = 0.22f;
                                tm.outlineColor = new Color(0.05f, 0.03f, 0.10f, 1f);
                            }
                        }
                        catch (System.Exception) { /* font lacks outline material — skip */ }
                        bumped++;
                    }
                }
            }
            Debug.Log($"[HomeChibiUpgrade] BumpTopLabels: bumped {bumped} TMP_Text label(s) to >= {MIN_FONT}.");
        }

        // Reset top-strip scaling on home load. The aggressive 1.4× bump
        // covered up the currency pills (gems/energy were hidden behind the
        // upscaled Sparq logo). Reverted: leave the prefab sizes alone.
        //
        // Diagnostic-only — logs what's in the top half so we can target
        // specific elements with surgical edits in the prefab if needed.
        private static void UpscaleTopUiStrip()
        {
            int considered = 0;
            int reset = 0;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas == null || canvas.rootCanvas == null) continue;
                if (canvas != canvas.rootCanvas) continue;
                var canvasRT = canvas.transform as RectTransform;
                if (canvasRT == null) continue;
                float canvasH = canvasRT.rect.height;
                if (canvasH < 1f) continue;
                float midY = canvasRT.rect.yMin + canvasH * 0.5f;

                foreach (Transform child in canvas.transform)
                {
                    if (child == null) continue;
                    var rt = child as RectTransform;
                    if (rt == null) continue;
                    considered++;
                    string n = child.name ?? "";

                    // Reset any previously-applied scaling so things go back
                    // to their prefab-authored size.
                    if (Mathf.Abs(rt.localScale.x - 1f) > 0.01f ||
                        Mathf.Abs(rt.localScale.y - 1f) > 0.01f)
                    {
                        var was = rt.localScale;
                        rt.localScale = Vector3.one;
                        Debug.Log($"[HomeChibiUpgrade] [RESET] '{n}' scale {was.x:F2} -> 1.0");
                        reset++;
                    }

                    // Diagnostic dump of every top-half child so we can see
                    // what's there and target individual elements with size
                    // overrides if the user wants specific ones bigger.
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    float bottomY = float.PositiveInfinity, topY = float.NegativeInfinity;
                    float w = rt.rect.width, h = rt.rect.height;
                    foreach (var c in corners)
                    {
                        Vector2 lp;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvasRT,
                            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, c),
                            canvas.worldCamera, out lp);
                        if (lp.y < bottomY) bottomY = lp.y;
                        if (lp.y > topY) topY = lp.y;
                    }
                    bool inTopHalf = bottomY >= midY - 5f;
                    if (inTopHalf)
                        Debug.Log($"[HomeChibiUpgrade] [TOP-STRIP] '{n}' size={w:F0}x{h:F0} rectY=[{bottomY:F0}..{topY:F0}]");
                }
            }
            Debug.Log($"[HomeChibiUpgrade] Top-strip: {considered} child(ren) considered, reset {reset} previously-scaled.");
        }

        // Fallback if Camera.main is null — use Mochi-relative offsets.
        private static void FallbackPositionFromOrigin()
        {
            var mochi = GameObject.Find("Mochi");
            Vector3 anchor = mochi != null ? mochi.transform.position : Vector3.zero;
            var karu = GameObject.Find("Karu");
            if (karu != null) karu.transform.position = anchor + new Vector3(-1.8f, 0.1f, -0.1f);
            string[] peckyNames = { "Stage_4_Pecky", "Pecky", "PeckyOwl", "Owl" };
            foreach (var n in peckyNames)
            {
                var go = GameObject.Find(n);
                if (go != null) go.transform.position = anchor + new Vector3(2.4f, 0.4f, -0.1f);
            }
        }

        // Match the home-screen "Mochi" GameObject's sprite to the player's active pet.
        // Was previously stuck on the busy Azure Will-o'-Wisp atlas (rendered as a mess).
        private static void UpgradeMochiFromActivePet()
        {
            #if UNITY_EDITOR
            var mochi = GameObject.Find("Mochi");
            if (mochi == null) return;
            var sr = mochi.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;

            var pet = Sparq.Systems.PetService.Active();
            if (pet == null) return;
            var species = Sparq.Systems.PetService.FindSpecies(pet.speciesId);
            if (species == null || string.IsNullOrEmpty(species.spritePath)) return;

            var sp = LoadSprite(species.spritePath);
            if (sp == null) return;

            sr.sprite = sp;
            sr.color = Color.white;
            sr.sortingOrder = MOCHI_SORT_ORDER;   // render behind Karu and other front sprites
            // Auto-scale so the new sprite fits TARGET_MOCHI_HEIGHT_UNITS tall
            float spriteHeight = sp.bounds.size.y;
            if (spriteHeight > 0.001f)
            {
                sr.transform.localScale = Vector3.one;
                float ratio = TARGET_MOCHI_HEIGHT_UNITS / spriteHeight;
                sr.transform.localScale = new Vector3(ratio, ratio, 1f);
            }
            // Attach a LateUpdate follower so Drip is RE-ANCHORED every frame.
            // (One-shot transform.position assignments kept getting overridden by
            // animators / parent layout groups / legacy scripts running after us.)
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                var follower = mochi.GetComponent<DripFollower>();
                if (follower == null) follower = mochi.AddComponent<DripFollower>();
                follower.target = karu.transform;
                follower.offset = MOCHI_REL_TO_KARU;
                // Also set it once now so we don't see a 1-frame snap
                mochi.transform.position = karu.transform.position + MOCHI_REL_TO_KARU;
            }

            // ── Destroy any duplicate Sweet-Droplet NPC from HomeNpcSpawner ──
            // (Sweet-Droplet was in the FRIENDLY_NPCS pool; if a session already
            //  spawned one before we removed it, it's still in the scene.)
            string petPath = species.spritePath;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null || go == mochi) continue;
                if (!go.name.StartsWith("HomeNpc")) continue;
                var npcSr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (npcSr == null || npcSr.sprite == null) continue;
                // If this NPC is showing the same pet sprite, kill it
                if (npcSr.sprite.name == sp.name)
                {
                    Object.Destroy(go);
                    Debug.Log($"[HomeChibiUpgrade] Destroyed duplicate Drip NPC: {go.name}");
                }
            }

            Debug.Log($"[HomeChibiUpgrade] Home pet → {species.name} (scaled {TARGET_MOCHI_HEIGHT_UNITS}u, rel-to-karu {MOCHI_REL_TO_KARU}, sort {MOCHI_SORT_ORDER})");
            #endif
        }

        // Help button — gets its own dedicated Canvas at very high sortingOrder so
        // no other home-screen overlay (HUD, popups, ranger canvases) can hide it.
        private static void SpawnHelpIcon()
        {
            // Clean up any prior version (including any old root canvas)
            var existing = GameObject.Find("Sparq_HelpIcon_Root");
            if (existing != null) Object.Destroy(existing);
            var existingIcon = GameObject.Find("Sparq_HelpIcon");
            if (existingIcon != null) Object.Destroy(existingIcon);

            // Dedicated full-screen canvas — sortingOrder 9000 puts us above HomeNavButtons (5000) and most UI
            var rootGO = new GameObject("Sparq_HelpIcon_Root",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            Object.DontDestroyOnLoad(rootGO);
            var rrt = rootGO.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var rc = rootGO.GetComponent<Canvas>();
            rc.renderMode = RenderMode.ScreenSpaceOverlay;
            rc.sortingOrder = 9000;
            var rcs = rootGO.GetComponent<CanvasScaler>();
            rcs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            rcs.referenceResolution = new Vector2(1080, 1920);
            rcs.matchWidthOrHeight = 0.5f;

            // The button itself
            var go = new GameObject("Sparq_HelpIcon",
                typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(rootGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(48, 140);
            rt.sizeDelta = new Vector2(128, 128);   // bigger per user request
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.10f, 0.12f, 0.22f, 0.95f);
            img.sprite = MakeSolidCircleSprite();

            var btn = go.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                HelpPopup.Show();
            });

            // Italic gold "i"
            var lblGO = new GameObject("Lbl", typeof(RectTransform));
            lblGO.transform.SetParent(go.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
            tm.text = "?";
            tm.fontSize = 88;
            tm.fontStyle = TMPro.FontStyles.Bold;
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.color = new Color(1f, 0.85f, 0.40f, 1f);
            tm.raycastTarget = false;

            Debug.Log("[HomeChibiUpgrade] Help icon spawned on dedicated canvas (sortingOrder 9000).");
        }

        private static Sprite _circleSp;
        private static Sprite MakeSolidCircleSprite()
        {
            if (_circleSp != null) return _circleSp;
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = sz / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx*dx + dy*dy);
                    tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
                }
            tex.Apply();
            _circleSp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return _circleSp;
        }

        public static void ForceRun() { _done = false; Ensure(); }

        /// <summary>
        /// Swap Karu's home-screen sprite to match the player's currently picked
        /// hero class. Safe to call repeatedly — runs every time, no _done guard.
        /// Loads only the first idle frame (single PNG = same safe path Una uses).
        /// Call from HeroSelectPanel after the player commits a pick.
        /// </summary>
        public static void RefreshKaruFromHeroClass()
        {
            #if UNITY_EDITOR
            var hero = Sparq.Systems.HeroClassResolver.Resolve();
            if (hero == null || string.IsNullOrEmpty(hero.idleBase)) return;

            // Most class packs use 3-digit zero-padded frame numbers starting at 000
            string firstFrame = hero.idleBase + "000.png";

            var karu = GameObject.Find("Karu");
            if (karu == null) return;
            var sr = karu.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;

            // Resolve the display sprite. The Knight's 1800×980 frame needs
            // alpha-cropping (huge transparent padding + a long spear) — all
            // that logic lives in HeroPortrait, the single source of truth
            // shared with HomeLobbyPanel + HeroSelectPanel.
            Sprite sp = null;
            float targetHeight = TARGET_KARU_HEIGHT_UNITS;
            if (hero.classId == "knight")
            {
                var p = HeroPortrait.LoadIdle(hero, excludeWeapon: true);
                if (p.ok && p.sprite != null && p.cropped)
                {
                    // Body-only crop: tight to the figure with a centre pivot
                    // (== body centre, spear excluded), so it scales to the
                    // dedicated Knight target and lands cleanly on Karu.
                    sp = p.sprite;
                    targetHeight = KNIGHT_FIGURE_HEIGHT_UNITS;
                }
            }
            if (sp == null) sp = LoadSprite(firstFrame);   // other classes, or knight fallback
            if (sp == null)
            {
                Debug.LogWarning($"[HomeChibiUpgrade] Karu refresh failed — sprite not found: {firstFrame}");
                return;
            }

            sr.sprite = sp;
            sr.color = Color.white;
            float spriteHeight = sp.bounds.size.y;
            if (spriteHeight > 0.001f)
            {
                sr.transform.localScale = Vector3.one;
                float ratio = targetHeight / spriteHeight;
                sr.transform.localScale = new Vector3(ratio, ratio, 1f);
            }
            Debug.Log($"[HomeChibiUpgrade] Karu refreshed → {hero.className}");
            #endif
        }

        private static void UpgradeKaru()
        {
            #if UNITY_EDITOR
            var karu = GameObject.Find("Karu");
            if (karu == null) return;
            var sr = karu.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;

            // Try primary, then fallbacks
            Sprite sp = LoadSprite(KARU_SPRITE);
            if (sp == null)
                foreach (var f in KARU_FALLBACKS)
                { sp = LoadSprite(f); if (sp != null) break; }
            if (sp == null) return;

            ApplySprite(sr, sp, TARGET_KARU_HEIGHT_UNITS);
            Debug.Log($"[HomeChibiUpgrade] Karu sprite -> {sp.name}");
            #endif
        }

        private static void UpgradeMochi()
        {
            #if UNITY_EDITOR
            var mochi = GameObject.Find("Mochi");
            if (mochi == null) return;
            var sr = mochi.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;
            var sp = LoadSprite(MOCHI_SPRITE);
            if (sp == null) return;
            ApplySprite(sr, sp, TARGET_MOCHI_HEIGHT_UNITS);
            Debug.Log($"[HomeChibiUpgrade] Mochi sprite -> Azure Will-o'-Wisp");
            #endif
        }

        // Set the sprite + auto-adjust GameObject's localScale so the visible
        // height matches the target. Compensates for big-resolution sprites.
        private static void ApplySprite(SpriteRenderer sr, Sprite sp, float targetHeightUnits)
        {
            sr.sprite = sp;
            sr.color = Color.white;

            // Sprite world height = bounds.size.y (in world units at default PPU)
            float spriteHeight = sp.bounds.size.y;
            if (spriteHeight <= 0.001f) return;

            // Existing scale on the GameObject (don't blow it away — multiply)
            // Reset to 1 first so we don't compound previous swaps
            sr.transform.localScale = Vector3.one;
            float ratio = targetHeightUnits / spriteHeight;
            sr.transform.localScale = new Vector3(ratio, ratio, 1f);
        }

        private static Sprite LoadSprite(string path)
        {
            #if UNITY_EDITOR
            // Auto-import as Sprite if needed
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && !Application.isPlaying)
            {
                bool changed = false;
                if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
                { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
                if (imp.spriteImportMode != UnityEditor.SpriteImportMode.Single)
                { imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; changed = true; }
                if (changed) imp.SaveAndReimport();
            }
            return Sparq.Core.SpriteLoader.Load(path);
            #else
            return null;
            #endif
        }
    }

    /// <summary>
    /// Bulletproof position lock — re-anchors Drip to Karu's position every
    /// LateUpdate (after all other Update/Animator passes), so no other script
    /// or component in the scene can override us.
    /// </summary>
    public class DripFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;

        private void LateUpdate()
        {
            if (target == null)
            {
                // Re-find Karu in case it was destroyed/re-created
                var k = GameObject.Find("Karu");
                if (k != null) target = k.transform;
                return;
            }
            transform.position = target.position + offset;
        }
    }

    /// <summary>
    /// Same idea as DripFollower but for any home-scene character that needs
    /// to be locked to a position relative to an anchor (Mochi). Runs in
    /// LateUpdate so animator curves and other position-driving scripts can't
    /// fight us. Used to keep Karu and Pecky in their authored on-screen
    /// positions every frame.
    /// </summary>
    public class HomePositionLock : MonoBehaviour
    {
        // Static world position — captured once, not driven by a live anchor.
        // Mochi has IdleBob+IdleBreathing scripts that move it every frame; if
        // we anchored to Mochi.transform here, Karu/Pecky would bob along with
        // it. A static target gives a clean visual lock.
        public Vector3 worldTarget;
        public bool hasTarget;

        private void LateUpdate()
        {
            if (!hasTarget) return;
            transform.position = worldTarget;
        }
    }
}
