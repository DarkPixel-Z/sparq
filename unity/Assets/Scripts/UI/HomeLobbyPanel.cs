using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Sparq's new home page, built around Layer Lab's GUI Pro-FantasyHero
    /// Lobby.prefab — a polished, professional mobile-RPG home layout.
    ///
    /// What this panel does:
    /// 1. Creates a high-sort Canvas on top of the existing home scene
    /// 2. Instantiates Lobby.prefab as the central content
    /// 3. Wires its key elements to Sparq systems:
    ///    - Player name / level / XP from SaveService
    ///    - Hero portrait from HeroClassResolver
    ///    - PLAY button -> StageMapPanel.Show()
    ///    - Currency labels from SaveService.Data
    /// 4. Adds a top currency strip + bottom nav since the prefab is just
    ///    the central player banner (the demo scene composes multiple parts)
    ///
    /// Lifecycle: Show() creates everything, Hide() destroys the canvas.
    /// </summary>
    public static class HomeLobbyPanel
    {
        private const string LOBBY_PREFAB_PATH =
            "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/Lobby.prefab";

        private static GameObject _root;

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);

            EnsureEventSystem();

            // ── Build host canvas — high sort so this is THE primary screen ──
            _root = new GameObject("Sparq_HomeLobby",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            // Sort above everything else, including BottomNavBar's 5000.
            int maxSort = 14000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            c.sortingOrder = maxSort + 10;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            // matchWidthOrHeight 0.0 = match WIDTH only. On 20:9 phones (POCO),
            // this keeps the 1080-wide content fitting horizontally — bottom nav
            // labels ("JOURNAL", "WEAPONS") stay readable, side rails stay on
            // screen. Vertical content may not fill bottom-to-top — backdrop
            // colour fills the gap. 1.0 (match height) caused horizontal
            // overflow; 0.5 caused dark corner gaps at top. Match width is the
            // least-bad of the three for tall mobile aspect ratios.
            cs.matchWidthOrHeight = 0.0f;

            // ── Solid OPAQUE backdrop — completely hides the legacy home scene
            //    behind the lobby. Trial card, scene art, world sprites, Sparq
            //    logo etc. all become invisible because this fills the screen.
            //    Black so the lobby art stands out cleanly.
            var bg = MakeImg(_root.transform, "Backdrop", new Color(0f, 0f, 0f, 1f));
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            // Backdrop is purely visual — let clicks pass through it to UI behind.
            bg.GetComponent<Image>().raycastTarget = false;

            // ── Instantiate Lobby.prefab — Resources first (works in editor + APK),
            //    AssetDatabase fallback for fresh checkouts where the Resources
            //    copy hasn't been staged yet. The `instance` variable is declared
            //    OUTSIDE any #if so the rest of the method compiles in both contexts.
            GameObject prefab = Resources.Load<GameObject>("Lobby");
            #if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(LOBBY_PREFAB_PATH);
            #endif
            if (prefab == null)
            {
                Debug.LogError($"[HomeLobbyPanel] Lobby.prefab NOT found (Resources/Lobby OR {LOBBY_PREFAB_PATH}).");
                BuildErrorMessage("Lobby.prefab missing — needs to be in Assets/Resources/.");
                return;
            }
            // Runtime-safe Instantiate (PrefabUtility.InstantiatePrefab is editor-only).
            var instance = GameObject.Instantiate(prefab, _root.transform);
            instance.name = "LobbyContent";
            // Make sure the prefab fills the canvas (Layer Lab prefabs are
            // typically authored at 1080x1920 already).
            var iRT = instance.GetComponent<RectTransform>();
            if (iRT != null)
            {
                iRT.anchorMin = Vector2.zero;
                iRT.anchorMax = Vector2.one;
                iRT.offsetMin = Vector2.zero;
                iRT.offsetMax = Vector2.zero;
                iRT.localScale = Vector3.one;
            }

            // ── Wire what we can — names sourced from prefab inspection ──
            WirePlayerLabels(instance);
            WireHeroPortrait(instance);
            KillNonButtonRaycasts(instance);   // critical — clears decoration Images blocking clicks
            // Critical: Layer Lab prefab has 0 Button components. Add them to
            // the visible button-shaped Images so our wiring code can hook in.
            InjectMissingButtons(instance);
            InjectSettingsButton(instance);    // top-right hex settings menu
            WireButtons(instance);
            WireLobbyRealData(instance);   // currency wiring (bottom-nav routing removed)
            WireChatButton(instance);      // speech-bubble frame → WorldPanel (chat)
            WireStoreTab(instance);        // bottom-nav 1st hex (shop)   → PopupManager.OpenShop
            WireResourceBarButtons(instance); // top-bar "+" buttons → Store on matching tab
            WireJournalTab(instance);      // bottom-nav 2nd hex → JournalPanel
            WirePetsTab(instance);         // bottom-nav 5th hex (was lock) → PetPanel
            WireRankingsButton(instance);  // right-rail trophy hex (above guild) → RankingsPanel
            WireGuildButton(instance);     // right-rail sword+shield hex   → GuildPanel
            WireFriendsButton(instance);   // left-rail ghost hex (red badge) → WorldPanel (Friends)
            HideTabAlertDiamonds(instance);// strip chunky green/red placeholder diamonds from the bottom nav
            WireDailyQuestsTile(instance); // middle "brown-frame" tile → QuestsPanel
            WireRemindersButton(instance); // upper-right large gray button → RemindPanel
            WireWeaponsTab(instance);      // bottom-nav 3rd tab (swords)  → EquipmentPanel
            WireBagTab(instance);          // bottom-nav 4th tab (chest)   → BagPanel
            // WirePlayerProfileBar moved BELOW DeclutterLobbyPrefab — the
            // declutter pass hides BaseFrame_Basic_Rectangle_H40_Divided
            // because of leftover Layer Lab placeholders (99,999 trophy +
            // blue '3' level pill), which was killing the profile bar.
            try { Sparq.Systems.RemindAlertRunner.EnsureRunning(); } catch {}
            try { Sparq.Systems.PetCareRunner.EnsureRunning(); } catch {}
            WireByTextLabel(instance);     // text-based wiring (PLAY, BATTLE, etc.)
            ForceInteractableAll(instance);    // override any disabled Selectables
            DiagnoseInteractivity(instance);   // comprehensive dump
            // First-time username prompt — only opens if name is still default
            try { Sparq.UI.UsernameEditPopup.ShowIfFirstTime(); } catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Username popup failed: {ex.Message}"); }
            // Strip Layer Lab placeholder clutter we don't actually use —
            // player banner, resource bars, side rails, chat. Keeps the
            // hero showcase and bottom nav, drops the noise.
            DeclutterLobbyPrefab(instance);
            // Wire the profile bar AFTER declutter so the bar isn't hidden
            // again by leftover placeholder content matches.
            WirePlayerProfileBar(instance);
            // Morning Vitality meter — the wellness→power link, front and centre.
            BuildVitalityBanner(instance);
            SpawnCompanions(instance);     // Blip + Pecky next to the hero
            // (Today's Trial card removed — daily quests now live in the
            //  brown DAILY QUESTS tile in the upper row.)
            // SpawnTodaysTrialCard();
            DumpLobbyInventory(instance);  // writes Assets/_sparq_lobby_inventory.txt for review
            // (KaruNameplate move and pet HP bar removed — they were noise)

            // (Back button removed — the lobby IS the home now. No fallback
            //  to the broken old scene; that page is being deprecated.)

            Debug.Log("[HomeLobbyPanel] Ready — Layer Lab Lobby instantiated.");

            // Morning Spoon Check — auto-prompt once per day if not done.
            // Energy-adaptive quest filtering needs this signal; Skip is
            // available (Skip just closes — no penalty).
            try
            {
                if (!Sparq.UI.SpoonCheckPanel.CheckedToday())
                    Sparq.UI.SpoonCheckPanel.Show();
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] SpoonCheck prompt failed: {ex.Message}"); }

            // Daily login reward — shown AFTER the spoon check so it sorts on top
            // (claim the reward first, then the spoon check is revealed beneath).
            try { Sparq.Systems.DailyBonusManager.ShowIfDue(); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] DailyBonus prompt failed: {ex.Message}"); }

            // If a Grace Day shield just saved the streak, surface it (the safety
            // net is otherwise invisible). Transient, non-blocking top toast.
            try
            {
                if (Sparq.Systems.QuestManager.ConsumeGraceDaySavedFlag())
                    ShowStreakSavedToast();
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Streak-saved toast failed: {ex.Message}"); }

            // Pet just crossed an evolution threshold (Juvenile/Adult/Elder)?
            // Celebrate the long-term care payoff with a one-time popup.
            try
            {
                if (Sparq.Systems.PetService.TryConsumeEvolutionAdvance(out int newStage) && newStage > 0)
                    Sparq.UI.PetEvolutionPopup.Show(newStage);
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Pet evolution popup failed: {ex.Message}"); }

            // Retroactively unlock allies for already-cleared zones (one-time
            // backfill — idempotent so safe on every lobby open).
            try { Sparq.Systems.AllyRoster.BackfillFromZoneProgress(); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Ally backfill failed: {ex.Message}"); }

            // Normalize xpToNextLevel for legacy saves still on the pre-unification
            // curve (idempotent + once-per-session latched inside Progression).
            try { Sparq.Systems.Progression.MigrateLegacyThreshold(Sparq.Core.SaveService.Data); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Progression migration failed: {ex.Message}"); }

            // First-run intro — shown LAST so it sorts on top and is the very
            // first thing a brand-new player sees (teaches the wellness→power loop).
            try { Sparq.UI.WelcomePanel.ShowIfFirstTime(); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Welcome FTUE failed: {ex.Message}"); }

            // (Re)schedule local notifications for when the app is next closed —
            // a daily check-in nudge + an "idle rewards full" reminder. No-op until
            // the Mobile Notifications package + SPARQ_NOTIFICATIONS define are added.
            try { Sparq.Systems.NotificationService.ScheduleDailyReminders(); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Notification scheduling failed: {ex.Message}"); }

            // Bootstrap cloud save (anonymous auth + pull-if-newer). Auto-uploads
            // on app pause / quit. No-op until SPARQ_CLOUDSAVE + Firebase SDK are added.
            try { Sparq.Systems.CloudSaveService.InitializeAsync(); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Cloud save init failed: {ex.Message}"); }
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // PLAYER DATA WIRING
        // ─────────────────────────────────────────────────────────────────

        private static void WirePlayerLabels(GameObject root)
        {
            var data = Sparq.Core.SaveService.Data;
            string playerName = data != null && !string.IsNullOrEmpty(data.petName) ? data.petName : "Karu";
            int level = data != null ? data.level : 1;

            // The prefab has TMP_Text components named "UserName" and
            // "Text_CharacterName". Find them by name and inject our data.
            foreach (var tm in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tm == null) continue;
                string n = tm.gameObject.name;
                if (n == "UserName" || n.Contains("UserName"))
                    tm.text = $"<b>{playerName}</b>";
                else if (n == "Text_CharacterName" || n.Contains("CharacterName"))
                    tm.text = playerName;
            }
        }

        // ── Hero portrait tuning knobs ───────────────────────────────────
        // The prefab's "Character" slot is small (~282×272). Because the hero
        // sprite is now alpha-cropped tight to the figure, we can enlarge the
        // slot freely — preserveAspect keeps the hero undistorted.
        private static readonly Vector2 HERO_PORTRAIT_SIZE  = new Vector2(360f, 540f);
        // Offset applied to the prefab's anchored position (left + down onto
        // the platform). Tune here if the hero needs to move.
        private static readonly Vector2 HERO_PORTRAIT_SHIFT = new Vector2(-150f, -65f);

        private static void WireHeroPortrait(GameObject root)
        {
            // The prefab uses an Image named "Character" for the hero portrait.
            // Swap in the player's selected class — alpha-cropped to the actual
            // figure (see HeroPortrait) so the Knight's huge 1800×980 frame
            // doesn't render him as a tiny sliver.
            #if UNITY_EDITOR
            var hero = Sparq.Systems.HeroClassResolver.Resolve();
            if (hero == null || string.IsNullOrEmpty(hero.idleBase)) return;

            var portrait = Sparq.UI.HeroPortrait.LoadIdle(hero, excludeWeapon: true);
            if (!portrait.ok || portrait.sprite == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Hero portrait load failed — leaving prefab art.");
                return;
            }

            int swapped = 0;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                if (img.gameObject.name != "Character") continue;
                img.sprite = portrait.sprite;
                img.preserveAspect = true;
                var rt = img.rectTransform;
                if (rt != null)
                {
                    // Enlarge the slot so the hero is prominent, then shift him
                    // left + down onto the platform.
                    rt.sizeDelta = HERO_PORTRAIT_SIZE;
                    rt.anchoredPosition += HERO_PORTRAIT_SHIFT;
                }
                swapped++;
            }
            Debug.Log($"[HomeLobbyPanel] Hero portrait set ({hero.className}, " +
                      $"cropped={portrait.cropped}) on {swapped} 'Character' Image(s).");
            #endif
        }

        // Strip Layer Lab's prefab down to what we actually use.
        // PHASE 1: hide by visible TMP_Text content (most reliable — Layer
        //          Lab uses generic GameObject names like "Card" so name-
        //          matching missed everything).
        // PHASE 2: hide by partial GameObject name as a backup.
        private static void DeclutterLobbyPrefab(GameObject lobbyRoot)
        {
            // ── PHASE 1: hide by visible text content ──
            // If a TMP_Text shows any of these labels, walk up to the container
            // and hide it. Catches side rails, player banner duplicates, etc.
            string[] hideByText = {
                "FRIENDS", "Friends", "CAMPAIGN", "Campaign",
                "RANKING", "Ranking", "CLAN", "Clan",
                "MAIL",    "INBOX",   "Inbox",
                "ACHIEVE", "Acheive", "Trophy", "TROPHY",
                "Looking friends add me", "Layerlab",   // chat banner placeholder
            };
            int hiddenByText = 0;
            foreach (var tm in lobbyRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tm == null) continue;
                // Never let our OWN injected labels trigger a hide. The rail-hex
                // labels we add ("RANKINGS", "FRIENDS", etc.) are named
                // "Sparq_*Label" and their text can collide with hideByText
                // entries (e.g. "FRIENDS" → the "Friends" rule), which would
                // hide the very button we just wired.
                if (tm.gameObject.name.StartsWith("Sparq_")) continue;
                string txt = (tm.text ?? "").Trim();
                if (txt.Length == 0) continue;
                bool match = false;
                foreach (var ht in hideByText)
                    if (txt.Contains(ht, System.StringComparison.OrdinalIgnoreCase)) { match = true; break; }
                if (!match) continue;
                // Walk up to find the visible container — 2 hops usually enough
                Transform target = tm.transform.parent;
                int hops = 0;
                while (target != null && hops < 2 && target != lobbyRoot.transform)
                {
                    if (target.GetComponent<Image>() != null) break;
                    target = target.parent;
                    hops++;
                }
                var hideGO = (target != null && target != lobbyRoot.transform)
                    ? target.gameObject
                    : tm.gameObject;
                if (hideGO == lobbyRoot) continue;   // safety
                hideGO.SetActive(false);
                hiddenByText++;
                Debug.Log($"[HomeLobbyPanel] Decluttered by text '{txt}': hid '{hideGO.name}'");
            }
            Debug.Log($"[HomeLobbyPanel] PHASE 1 text-match: hid {hiddenByText} container(s).");

            // ── PHASE 1B: hide top-banner placeholder bars by NUMERIC pattern ──
            // The trophy "99,999" and resource bars "16/40", "13/50" all live
            // in the top ~30% of the screen. Karu's nameplate "336/280" is in
            // the LOWER half, so it's safe. We hide containers whose text
            // matches X/Y or large comma-numbers AND sits in the top region.
            int hiddenByNumeric = 0;
            foreach (var tm in lobbyRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tm == null || !tm.gameObject.activeSelf) continue;
                string txt = (tm.text ?? "").Trim();
                if (txt.Length == 0) continue;

                // Must be a placeholder pattern: "X/Y" with both sides digits,
                // OR a comma-separated big number like "99,999" or "999,999"
                bool isSlashRatio = System.Text.RegularExpressions.Regex.IsMatch(
                    txt, @"^\d{1,4}/\d{1,4}$");
                bool isCommaNumber = System.Text.RegularExpressions.Regex.IsMatch(
                    txt, @"^\d{1,3}(,\d{3})+$");
                if (!isSlashRatio && !isCommaNumber) continue;

                // Must be in TOP 30% of screen (banner/resource area)
                Vector3[] cc = new Vector3[4];
                tm.rectTransform.GetWorldCorners(cc);
                Vector3 wc = (cc[0] + cc[2]) * 0.5f;
                Vector2 sc = RectTransformUtility.WorldToScreenPoint(null, wc);
                if (sc.y < Screen.height * 0.70f) continue;   // skip mid/lower-screen

                // Walk up to find the BANNER container — go further (4 hops)
                // since Layer Lab nests "Text > Bg > InfoArea > Banner" patterns.
                // We stop when we hit a container that's clearly the whole
                // banner: has Image + is wide (>500px) + multiple children.
                Transform target = tm.transform.parent;
                int hops = 0;
                Transform bestContainer = null;
                while (target != null && hops < 4 && target != lobbyRoot.transform)
                {
                    var tRT = target as RectTransform;
                    if (tRT != null && target.GetComponent<Image>() != null)
                    {
                        // Track the widest Image ancestor found
                        if (tRT.rect.width >= 300f) bestContainer = target;
                    }
                    target = target.parent;
                    hops++;
                }
                var hideGO = (bestContainer != null)
                    ? bestContainer.gameObject
                    : tm.gameObject;
                if (hideGO == lobbyRoot) continue;   // safety
                hideGO.SetActive(false);
                hiddenByNumeric++;
                Debug.Log($"[HomeLobbyPanel] Decluttered by numeric '{txt}': hid '{hideGO.name}'");
            }
            Debug.Log($"[HomeLobbyPanel] PHASE 1B numeric-match: hid {hiddenByNumeric} placeholder bar(s).");

            // ── PHASE 1C: hide the level-pill banner (a single-digit "1" or
            //    "3" in the TOP 20% of screen is the player-level banner). ──
            int hiddenByLevelPill = 0;
            foreach (var tm in lobbyRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tm == null || !tm.gameObject.activeSelf) continue;
                string txt = (tm.text ?? "").Trim();
                // Single-digit or two-digit level pill (e.g. "1", "3", "12")
                if (txt.Length < 1 || txt.Length > 2) continue;
                bool allDigits = true;
                foreach (var c in txt) if (c < '0' || c > '9') { allDigits = false; break; }
                if (!allDigits) continue;

                // TOP 20% of screen — that's the player banner area
                Vector3[] cc = new Vector3[4];
                tm.rectTransform.GetWorldCorners(cc);
                Vector3 wc = (cc[0] + cc[2]) * 0.5f;
                Vector2 sc = RectTransformUtility.WorldToScreenPoint(null, wc);
                if (sc.y < Screen.height * 0.80f) continue;   // strictly top 20%

                // Walk up to a banner-sized container (Image, width >= 400)
                Transform target = tm.transform.parent;
                int hops = 0;
                Transform bestContainer = null;
                while (target != null && hops < 5 && target != lobbyRoot.transform)
                {
                    var tRT = target as RectTransform;
                    if (tRT != null && target.GetComponent<Image>() != null && tRT.rect.width >= 400f)
                        bestContainer = target;
                    target = target.parent;
                    hops++;
                }
                if (bestContainer == null) continue;
                bestContainer.gameObject.SetActive(false);
                hiddenByLevelPill++;
                Debug.Log($"[HomeLobbyPanel] Decluttered by top-banner level pill '{txt}': hid '{bestContainer.name}'");
            }
            Debug.Log($"[HomeLobbyPanel] PHASE 1C level-pill banner: hid {hiddenByLevelPill}.");

            // ── PHASE 2: hide by partial GameObject name (backup) ──
            // Substrings — if a GameObject's name contains ANY of these, hide it.
            // Order: side rails / player banner / resource bars / chat / misc placeholders.
            string[] hidePartials =
            {
                "Friends",   "Campaign",  "Ranking",   "Clan",         // side rails
                "Trophy",    "Acheive",                                   // player trophy + achievement
                "Mailbox",   "Inbox",                                     // inbox/mail
                "Chat",                                                   // chat banner
                "Chest_",    "Chest1",   "ChestBg",   "ChestBar",        // chest progress bar
                "InnerLLIne_Purple",                                       // left chest + blue progress bar (unused) — NOT the Brown DAILY QUESTS frame
                "Pass_",     "PassBg",   "PassBar",                       // VIP pass bar
                "Mission_",  "MissionBg",                                 // mission progress
                "FreeChest",                                              // free chest with timer
                "ProfileFrame", "UserName", "UserInfo",                   // player banner duplicate
                "Icon_Trophy", "Icon_Badge", "Icon_Gift", "Icon_Ticket",  // banner icons
            };

            // Safeguards — never hide these even if substring matches
            string[] keepExact =
            {
                "Lobby", "Canvas", "Character", "CharacterShadow",
                "Sparq_HomeLobby", "LobbyContent",
                "Sparq_PetHpBar", "Sparq_TrialCard", "Sparq_PetNameplate",
                "Blip", "Pecky",
                "BarBg", "Fill", "Pct", "Lvl", "Txt",   // our pet HP bar children
            };

            int hidden = 0;
            foreach (var rt in lobbyRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == null) continue;
                string n = rt.gameObject.name ?? "";

                // Skip safeguarded keepers
                bool isKeeper = false;
                foreach (var k in keepExact) if (n == k) { isKeeper = true; break; }
                if (isKeeper) continue;
                if (n.StartsWith("Sparq_")) continue;   // our own injected stuff

                // Substring match against hide list
                bool match = false;
                foreach (var hp in hidePartials) if (n.Contains(hp)) { match = true; break; }
                if (!match) continue;

                rt.gameObject.SetActive(false);
                hidden++;
                Debug.Log($"[HomeLobbyPanel] Decluttered: hidden '{n}' (matched substring rule)");
            }
            Debug.Log($"[HomeLobbyPanel] Declutter pass complete — hid {hidden} placeholder element(s).");
        }

        // ─────────────────────────────────────────────────────────────────
        // DIAGNOSTIC — writes the full clickable-element inventory of the
        // lobby to a file (Assets/_sparq_lobby_inventory.txt) so the exact
        // button names / positions can be reviewed without console copy-paste.
        // Runs after all wiring + declutter, so it reflects the FINAL state.
        // ─────────────────────────────────────────────────────────────────
        private static void DumpLobbyInventory(GameObject root)
        {
            #if UNITY_EDITOR
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# Sparq lobby inventory  —  {System.DateTime.Now}");
                sb.AppendLine($"# Screen: {Screen.width}x{Screen.height}  (rel coords: 0,0 = bottom-left, 1,1 = top-right)");
                sb.AppendLine();

                var imgs = root.GetComponentsInChildren<Image>(true);
                sb.AppendLine($"## {imgs.Length} Image element(s) — anything with hasButton=True is clickable:");
                sb.AppendLine();
                foreach (var img in imgs)
                {
                    if (img == null) continue;
                    var go = img.gameObject;
                    var rt = img.rectTransform;
                    Vector3[] cc = new Vector3[4]; rt.GetWorldCorners(cc);
                    Vector2 ctr = RectTransformUtility.WorldToScreenPoint(null, (cc[0] + cc[2]) * 0.5f);
                    float relX = Screen.width  > 0 ? ctr.x / Screen.width  : 0f;
                    float relY = Screen.height > 0 ? ctr.y / Screen.height : 0f;
                    var btn = go.GetComponent<Button>();
                    string txt = "";
                    var tmp = go.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null) txt = (tmp.text ?? "").Replace("\n", " ").Trim();
                    sb.AppendLine(
                        $"  name='{go.name}'  active={go.activeInHierarchy}  " +
                        $"hasButton={(btn != null)}  raycast={img.raycastTarget}  " +
                        $"rel=({relX:F2},{relY:F2})  size=({rt.rect.width:F0}x{rt.rect.height:F0})  " +
                        $"text='{txt}'");
                    sb.AppendLine($"      path: {HierPath(go.transform, root.transform)}");
                }

                string outPath = Application.dataPath + "/_sparq_lobby_inventory.txt";
                System.IO.File.WriteAllText(outPath, sb.ToString());
                Debug.Log($"[HomeLobbyPanel] Lobby inventory written → {outPath}");
            }
            catch (System.Exception ex)
            { Debug.LogError($"[HomeLobbyPanel] DumpLobbyInventory failed: {ex.Message}"); }
            #endif
        }

        // Slash-separated hierarchy path from `root` down to `t`.
        private static string HierPath(Transform t, Transform root)
        {
            var parts = new System.Collections.Generic.List<string>();
            while (t != null && t != root) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts);
        }

        // ─────────────────────────────────────────────────────────────────
        // INJECT MISSING BUTTONS — Layer Lab prefab has 0 Buttons. Visible
        // "buttons" are just Images. Add Button components to the obvious
        // ones so our wiring code can hook them up.
        // ─────────────────────────────────────────────────────────────────
        // Surgical button injection — adds Button ONLY to elements that
        // SHOULD be interactive: PLAY (text), and 5 bottom-nav hexes
        // (largest Image per cluster in bottom 15% of screen).
        private static void InjectMissingButtons(GameObject root)
        {
            int added = 0;

            // ── 1) PLAY button — anywhere with TMP_Text "PLAY" in subtree ──
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                string t = (tmp.text ?? "").Trim().ToUpper();
                if (t != "PLAY" && t != "BATTLE" && t != "START") continue;
                // Walk UP to find the visible button container (Image, not text)
                Transform target = tmp.transform.parent;
                int hops = 0;
                Image containerImg = null;
                while (target != null && hops < 3)
                {
                    var ti = target.GetComponent<Image>();
                    if (ti != null) { containerImg = ti; break; }
                    target = target.parent; hops++;
                }
                if (containerImg == null) continue;
                if (containerImg.gameObject.GetComponent<Selectable>() != null) continue;

                var btn = containerImg.gameObject.AddComponent<Button>();
                btn.targetGraphic = containerImg;
                btn.interactable = true;
                containerImg.raycastTarget = true;
                btn.onClick.AddListener(() => {
                    Debug.Log($"[HomeLobbyPanel] ✓ PLAY (text='{t}') tapped on '{containerImg.gameObject.name}'.");
                    LaunchStageMap();
                });
                added++;
                Debug.Log($"[HomeLobbyPanel] Injected PLAY button on '{containerImg.gameObject.name}' (label='{t}')");
            }

            // ── (Bottom-nav cluster injection REMOVED) ──
            // This used to cluster bottom-of-screen Images and inject a Button
            // on the largest per cluster. The lobby inventory proved it was
            // creating PHANTOM buttons on Layer Lab decorations — a broken
            // zero-width TabMenu and the chat-bar frame — which then got
            // mis-routed by WireLobbyRealData's position sort (that's how the
            // chat bubble ended up opening Quests).
            //
            // The real bottom nav is the dedicated BottomNavBar / TopNav
            // systems. PLAY (above) is the only button HomeLobbyPanel injects;
            // the chat button is wired explicitly, by name, in WireChatButton().

            Debug.Log($"[HomeLobbyPanel] InjectMissingButtons (PLAY only): added {added} Button component(s).");
        }

        // Inject + wire the settings/menu button at top-right of canvas.
        // Layer Lab puts a hex burger-menu icon there with no text. Find by
        // position (top-right corner) + reasonable size, hook to a settings
        // action. For now opens the username editor as the only settings UI.
        private static void InjectSettingsButton(GameObject root)
        {
            Image best = null;
            float bestScore = 0;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                if (img.gameObject.GetComponent<Selectable>() != null) continue;
                var rt = img.rectTransform;
                if (rt == null) continue;
                var sz = rt.rect.size;
                if (sz.x < 60f || sz.y < 60f || sz.x > 200f || sz.y > 200f) continue;
                // Top-right of the screen — relX > 0.78, relY > 0.92
                Vector3[] cc = new Vector3[4]; rt.GetWorldCorners(cc);
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, (cc[0] + cc[2]) * 0.5f);
                float relX = sp.x / Screen.width;
                float relY = sp.y / Screen.height;
                if (relX < 0.78f || relY < 0.90f) continue;
                // Prefer larger Image (the bg of the menu button, not its inner icon)
                float score = sz.x * sz.y;
                if (score > bestScore) { bestScore = score; best = img; }
            }
            if (best == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Settings button not found at top-right.");
                return;
            }
            var btn = best.gameObject.AddComponent<Button>();
            btn.targetGraphic = best;
            btn.interactable = true;
            best.raycastTarget = true;
            btn.onClick.AddListener(() => {
                Debug.Log($"[HomeLobbyPanel] ✓ Settings tapped on '{best.gameObject.name}'.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                // Open the proper Settings panel — has username/audio/about/sign-out
                Hide();
                try { Sparq.UI.SettingsPanel.Show(); }
                catch (System.Exception ex) { Debug.LogError($"[HomeLobbyPanel] SettingsPanel: {ex.Message}"); }
            });
            Debug.Log($"[HomeLobbyPanel] Injected Settings button on '{best.gameObject.name}'.");
        }

        // ─────────────────────────────────────────────────────────────────
        // WIRE BY TEXT LABEL — bulletproof button matching by visible text
        // ─────────────────────────────────────────────────────────────────
        //
        // GameObject names in Layer Lab's prefab are generic ("Bg", "Icon",
        // "Button_Convex_Rectangle_01_l_Gray"). The most reliable identifier
        // is what the button's TEXT SAYS — "PLAY", "BATTLE", "BEGIN", "GO".
        // This pass scans every Button, looks at its TMP children, and wires
        // by visible label. Wires only buttons that currently have no
        // persistent listeners (to avoid stomping on prior wiring).
        private static void WireByTextLabel(GameObject root)
        {
            int wired = 0;
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                if (btn.onClick.GetPersistentEventCount() > 0) continue;   // already wired
                // WireChatButton already owns the chat-bar frame — never let
                // text-matching touch it. Its placeholder text contains
                // "Me"(ssage), which false-matched the PROFILE route below and
                // wired ProfilePanel onto the chat button as a SECOND listener
                // (chat opened, profile opened underneath it).
                if (btn.gameObject.name == "BaseFrame_Border_Rectangle_H50") continue;

                // Skip ALL bottom-nav tabs — they have explicit wires (see
                // WireWeaponsTab / WireBagTab / WireJournalTab). Without
                // this, my "WEAPONS" / "BAG" labels would double-wire and
                // Show()'s toggle behaviour would close the panel again.
                {
                    bool inTabMenu = false;
                    Transform p = btn.transform.parent;
                    while (p != null)
                    {
                        if (p.name == "TabMenu_BottomFlush_01") { inTabMenu = true; break; }
                        p = p.parent;
                    }
                    if (inTabMenu) continue;
                }

                // Walk all TMP children for visible text
                string label = "";
                foreach (var tmp in btn.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    string t = (tmp.text ?? "").Trim().ToUpper();
                    if (t.Length == 0) continue;
                    label = t; break;
                }
                if (label.Length == 0) continue;

                // Match labels → routes
                System.Action route = null;
                if (label.Contains("PLAY") || label.Contains("START") ||
                    label.Contains("BATTLE") || label.Contains("FIGHT") ||
                    label == "GO")
                {
                    route = LaunchStageMap;
                }
                else if (label.Contains("BEGIN"))
                {
                    route = () => { Hide(); try { Sparq.UI.QuestsPanel.Show(); } catch {} };
                }
                else if (label.Contains("QUEST"))
                {
                    route = () => { Hide(); try { Sparq.UI.QuestsPanel.Show(); } catch {} };
                }
                else if (label.Contains("BAG") || label.Contains("INVENTORY"))
                {
                    // BAG / INVENTORY → consumables panel (BagPanel).
                    // Weapons/armour live on the sword hex via WireWeaponsTab.
                    route = () => { Hide(); try { Sparq.UI.BagPanel.Show(); } catch {} };
                }
                else if (label.Contains("WEAPON") || label.Contains("EQUIP") ||
                         label.Contains("ARMOR")  || label.Contains("ARMOUR") ||
                         label.Contains("GEAR"))
                {
                    route = () => { Hide(); try { Sparq.UI.EquipmentPanel.Show(); } catch {} };
                }
                else if (label.Contains("PET"))
                {
                    route = () => { Hide(); try { Sparq.UI.PetPanel.Show(); } catch {} };
                }
                else if (label.Contains("PROFILE") || label == "ME" || label == "👤")
                {
                    route = () => { Hide(); try { Sparq.UI.ProfilePanel.Show(); } catch {} };
                }

                if (route == null) continue;
                var captured = route;
                var capturedLabel = label;
                btn.onClick.AddListener(() => {
                    Debug.Log($"[HomeLobbyPanel] ✓ Text-wired button '{btn.gameObject.name}' (label='{capturedLabel}') fired.");
                    try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                    captured();
                });
                wired++;
                Debug.Log($"[HomeLobbyPanel] WireByTextLabel: wired '{btn.gameObject.name}' (label='{label}') -> route.");
            }
            Debug.Log($"[HomeLobbyPanel] WireByTextLabel: wired {wired} button(s) by visible text.");
        }

        // ─────────────────────────────────────────────────────────────────
        // DIAGNOSTIC — dump everything that affects button clicks
        // ─────────────────────────────────────────────────────────────────
        private static void DiagnoseInteractivity(GameObject root)
        {
            // 1) EventSystem status
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
                Debug.LogError("[Diag] NO EventSystem in scene — buttons cannot fire.");
            else
                Debug.Log($"[Diag] EventSystem: '{es.name}' active={es.gameObject.activeInHierarchy} enabled={es.enabled} module={es.currentInputModule}");

            // 2) Every Canvas + GraphicRaycaster
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c == null) continue;
                var gr = c.GetComponent<GraphicRaycaster>();
                Debug.Log($"[Diag] Canvas '{c.name}' sort={c.sortingOrder} raycaster={(gr!=null?"YES":"NO")} interactable=YES");
            }

            // 3) CanvasGroup state on every ancestor of the lobby root
            Transform t = root.transform;
            int hops = 0;
            while (t != null && hops < 6)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                    Debug.Log($"[Diag] CanvasGroup on '{t.name}': interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts} alpha={cg.alpha}");
                t = t.parent; hops++;
            }

            // 4) Every Selectable in the lobby — name, interactable, raycast on its graphic
            int selectables = 0, nonInteractable = 0, noRaycast = 0;
            foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
            {
                if (sel == null) continue;
                selectables++;
                bool gotRaycast = false;
                var graphic = sel.targetGraphic;
                if (graphic != null && graphic.raycastTarget) gotRaycast = true;
                if (!sel.interactable) { nonInteractable++; Debug.LogWarning($"[Diag] DISABLED Selectable '{sel.gameObject.name}' (interactable=false)"); }
                if (!gotRaycast) { noRaycast++; Debug.LogWarning($"[Diag] NO RAYCAST on '{sel.gameObject.name}' targetGraphic (={graphic?.name})"); }
            }
            Debug.Log($"[Diag] Total Selectables in lobby: {selectables} ({nonInteractable} disabled, {noRaycast} no-raycast-on-graphic).");

            // 5) Listener count on every Button
            int wiredButtons = 0, unwiredButtons = 0;
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                int n = btn.onClick.GetPersistentEventCount();
                // Count runtime listeners too (RemoveAllListeners can hide them)
                // Note: we can't reliably count non-persistent listeners in newer Unity,
                // but we can check if it has ANY listeners using HasPersistentInvokeForListener
                if (n > 0) wiredButtons++;
                else unwiredButtons++;
            }
            Debug.Log($"[Diag] Buttons: {wiredButtons} with persistent listeners, {unwiredButtons} appear unwired.");
        }

        // Force everything live + visible + raycastable. Layer Lab prefabs
        // reference Feel's MMF_Player (we don't have Feel installed), so
        // missing scripts leave Selectables in a non-interactive, non-
        // raycastable state. Force the final-state values + raycast ON +
        // strip the broken script stubs.
        private static void ForceInteractableAll(GameObject root)
        {
            int fixedCount = 0;
            int raycastsEnabled = 0;

            // 1. Force every Selectable's hit graphics to raycast=true
            foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
            {
                if (sel == null) continue;
                if (!sel.interactable) { sel.interactable = true; fixedCount++; }
                // Ensure the Selectable's own Image has raycastTarget = true.
                var img = sel.GetComponent<Image>();
                if (img != null && !img.raycastTarget)
                { img.raycastTarget = true; raycastsEnabled++; }
                // Also force raycast on the targetGraphic (might be a different Image)
                if (sel.targetGraphic != null && !sel.targetGraphic.raycastTarget)
                { sel.targetGraphic.raycastTarget = true; raycastsEnabled++; }
                // And on ALL Image children — covers the case where Layer Lab
                // wraps button visuals in nested icons that all need raycast on.
                foreach (var childImg in sel.GetComponentsInChildren<Image>(true))
                {
                    if (childImg == null || childImg == img) continue;
                    if (!childImg.raycastTarget)
                    { childImg.raycastTarget = true; raycastsEnabled++; }
                }
            }

            // 2. Force every CanvasGroup to alpha=1, interactable=true, blocksRaycasts=true
            foreach (var cg in root.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (cg == null) continue;
                if (cg.alpha < 0.99f) { cg.alpha = 1f; fixedCount++; }
                if (!cg.interactable) { cg.interactable = true; fixedCount++; }
                if (!cg.blocksRaycasts) { cg.blocksRaycasts = true; fixedCount++; }
            }

            Debug.Log($"[HomeLobbyPanel] ForceInteractableAll: re-enabled {fixedCount} state(s), raycast-enabled {raycastsEnabled} image(s).");

            // 3. Strip broken Feel MMF_Player script stubs
            #if UNITY_EDITOR
            int stripped = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                stripped += UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            if (stripped > 0)
                Debug.Log($"[HomeLobbyPanel] Stripped {stripped} missing-script MonoBehaviour(s).");
            #endif
        }

        // Walk every Image in the lobby and disable raycastTarget on those
        // that AREN'T part of an interactive Button or Selectable hierarchy.
        //
        // KEY INSIGHT: Unity's Button fires when raycast hits ANY graphic
        // that's a descendant of the Button GameObject — not just the Button's
        // own targetGraphic. So we keep raycast on every Image whose ancestor
        // chain contains a Button (or Toggle/Slider/Selectable).
        private static void KillNonButtonRaycasts(GameObject lobbyRoot)
        {
            int killed = 0;
            foreach (var img in lobbyRoot.GetComponentsInChildren<Image>(true))
            {
                if (img == null || !img.raycastTarget) continue;
                if (img.GetComponent<Selectable>() != null) continue;

                // Walk ALL ancestors up to the canvas — if any is a Selectable
                // (Button, Toggle, Slider, InputField...), keep raycast.
                bool hasSelectableAncestor = false;
                Transform t = img.transform.parent;
                while (t != null && t != lobbyRoot.transform.parent)
                {
                    if (t.GetComponent<Selectable>() != null)
                    { hasSelectableAncestor = true; break; }
                    t = t.parent;
                }
                if (hasSelectableAncestor) continue;

                img.raycastTarget = false;
                killed++;
            }
            Debug.Log($"[HomeLobbyPanel] KillNonButtonRaycasts: disabled raycastTarget on {killed} non-interactive Image(s).");
        }

        // ─────────────────────────────────────────────────────────────────
        // WIRE LOBBY TO REAL SPARQ DATA
        // ─────────────────────────────────────────────────────────────────
        // Replaces Layer Lab's placeholder demo values with real PlayerData:
        //   - Currency numbers (top bar) read from SaveService.Data.sparqCoins
        //   - Bottom nav buttons routed to existing Sparq panels
        private static void WireLobbyRealData(GameObject lobbyRoot)
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] No SaveService data — currency stays as placeholder.");
            }
            else
            {
                // Replace Layer Lab's demo currency numbers in the top bar.
                // Demo shows: "9/9" (energy), "999999" (coins), "9999" (gems).
                // We map: coins -> sparqCoins. Energy + gems stay placeholder
                // until those systems exist in Sparq.
                int swapped = 0;
                foreach (var tm in lobbyRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tm == null) continue;
                    string txt = (tm.text ?? "").Trim();
                    // The big coin number — "999999" placeholder → real coins
                    if (txt == "999999" || txt == "999,999")
                    {
                        tm.text = data.sparqCoins.ToString("N0");
                        swapped++;
                    }
                    // Gems — show 0 (no system yet)
                    else if (txt == "9999" || txt == "9,999")
                    {
                        tm.text = "0";
                        swapped++;
                    }
                    // Energy "9/9" — repurposed as the combined POWER rating
                    // (hero level + gear + active pet). The ⚡ icon reads as
                    // "might", so this is a meaningful at-a-glance strength
                    // number that climbs whenever the player levels up, equips
                    // gear on the hero, or strengthens their pet.
                    else if (txt == "9/9")
                    {
                        tm.text = ComputeCombinedPower().ToString("N0");
                        swapped++;
                    }
                    // Player level "3" near banner — use real level
                    else if (txt == "3" && tm.fontSize < 30)
                    {
                        // tight match: small font, single digit, near top
                        // (we don't want to overwrite Karu's nameplate Lv pill)
                        Vector3[] cc = new Vector3[4];
                        tm.rectTransform.GetWorldCorners(cc);
                        Vector3 wc = (cc[0] + cc[2]) * 0.5f;
                        Vector2 sc = RectTransformUtility.WorldToScreenPoint(null, wc);
                        if (sc.y > Screen.height * 0.5f)   // top half = player banner
                        {
                            tm.text = data.level.ToString();
                            swapped++;
                        }
                    }
                }
                Debug.Log($"[HomeLobbyPanel] Currency/data wired: replaced {swapped} placeholder value(s) with real data (coins={data.sparqCoins}).");
            }

            // ── (Bottom-nav routing REMOVED) ──
            // This used to grab every Button in the bottom 15% of the screen,
            // sort them by X, and assign HOME/QUESTS/BATTLE/BAG/PROFILE by
            // index. But the only buttons down there were the PHANTOMS that
            // InjectMissingButtons used to create on Layer Lab decorations —
            // so the position sort routed the chat bubble to Quests and a
            // decorative bar to Battle. The real bottom nav is owned by the
            // dedicated BottomNavBar / TopNav systems; HomeLobbyPanel no
            // longer touches it. The chat button is wired in WireChatButton().
        }

        // Combined "Power" rating shown in the top-bar ⚡ slot — a single
        // at-a-glance strength number that aggregates the player's three
        // progression vectors so it climbs no matter what they invest in:
        //   • Hero level   (base scaling, never zero)
        //   • Hero gear    (sum of equipped weapon/armour stats)
        //   • Active pet   (sum of the companion's atk/def/hp)
        private static int ComputeCombinedPower()
        {
            int power = 0;
            try
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null) power += Mathf.Max(1, data.level) * 25;   // hero base
            }
            catch {}
            try
            {
                var hero = Sparq.Systems.EquipmentService.TotalStats();
                power += hero.atk + hero.def + hero.hp;
            }
            catch {}
            try
            {
                var pet = Sparq.Systems.PetService.Active();
                if (pet != null)
                {
                    var ps = Sparq.Systems.PetService.StatsOf(pet);
                    power += ps.atk + ps.def + ps.hp;
                }
            }
            catch {}
            return power;
        }

        // The Layer Lab lobby has a chat-bar decoration near the bottom-left:
        // a wide frame named "BaseFrame_Border_Rectangle_H50" with a speech-
        // bubble Icon at its left end. Wire the whole frame to open ChatPanel
        // (the 3-tab chat: Individual DM inbox / World / Guild). By NAME,
        // explicitly — so it can never be mis-routed by a position heuristic.
        private static void WireChatButton(GameObject root)
        {
            Transform frame = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt != null && rt.gameObject.name == "BaseFrame_Border_Rectangle_H50")
                { frame = rt.transform; break; }
            }
            if (frame == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Chat-bar frame 'BaseFrame_Border_Rectangle_H50' not found — chat button not wired.");
                return;
            }

            var go = frame.gameObject;
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            var img = go.GetComponent<Image>();
            if (img == null) img = go.GetComponentInChildren<Image>(true);
            if (img != null) btn.targetGraphic = img;
            btn.interactable = true;

            // KillNonButtonRaycasts ran earlier and cleared raycastTarget on
            // this frame's graphics (no Selectable ancestor at the time) — turn
            // them back on so a tap anywhere on the bar registers.
            foreach (var g in go.GetComponentsInChildren<Graphic>(true))
                if (g != null) g.raycastTarget = true;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] ✓ Chat button tapped — opening ChatPanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.ChatPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] ChatPanel.Show failed: {ex.Message}"); }
            });
            Debug.Log("[HomeLobbyPanel] Wired chat button (BaseFrame_Border_Rectangle_H50 → ChatPanel).");
        }

        // The VISIBLE bottom-nav is the lobby's 'TabMenu_BottomFlush_01'. Its
        // tabs ship as pure decoration — no Button, raycast off — which is why
        // tapping them did nothing. The 2nd tab, 'Tab_Nomal (2)' (the hex with
        // the green alert badge the user circled), should open the Journal.
        // Inject a Button on it, turn its raycasts back on, and wire it.
        private static void WireJournalTab(GameObject root)
        {
            Transform tabMenu = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "TabMenu_BottomFlush_01")
                { tabMenu = rt.transform; break; }
            if (tabMenu == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] 'TabMenu_BottomFlush_01' not found — Journal tab not wired.");
                return;
            }

            var journalTab = tabMenu.Find("Tab_Nomal (2)");
            if (journalTab == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] 'Tab_Nomal (2)' not found under TabMenu — Journal tab not wired.");
                return;
            }

            // Turn raycast back on for every graphic in the tab (KillNonButton-
            // Raycasts cleared them) so a tap anywhere on the visible hex lands.
            foreach (var g in journalTab.GetComponentsInChildren<Graphic>(true))
                if (g != null) g.raycastTarget = true;

            var btn = journalTab.GetComponent<Button>();
            if (btn == null) btn = journalTab.gameObject.AddComponent<Button>();
            var img = journalTab.GetComponent<Image>();
            if (img == null) img = journalTab.GetComponentInChildren<Image>(true);
            if (img != null) btn.targetGraphic = img;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] ✓ Journal tab tapped — opening JournalPanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.JournalPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] JournalPanel.Show failed: {ex.Message}"); }
            });
            // Replace the Layer Lab placeholder "TEXT" label with "JOURNAL".
            SetTabLabel(journalTab, "JOURNAL");
            Debug.Log("[HomeLobbyPanel] Wired Journal tab (TabMenu_BottomFlush_01/Tab_Nomal (2) → JournalPanel).");
        }

        // ─────────────────────────────────────────────────────────────────
        // Rankings button — right-rail trophy hex (Button_Basic_Rectangle_
        // H46 (2) at rel ≈ 0.90, 0.70). Opens RankingsPanel — synthetic
        // top-10 leaderboard sorted by total XP with the player highlighted.
        // ─────────────────────────────────────────────────────────────────
        private static void WireRankingsButton(GameObject root)
        {
            WireNamedRailHex(root,
                gameObjectName: "Button_Basic_Rectangle_H46 (2)",
                label: "RANKINGS",
                logLabel: "Rankings",
                onClick: () => {
                    try { Sparq.UI.RankingsPanel.Show(); }
                    catch (System.Exception ex)
                    { Debug.LogError($"[HomeLobbyPanel] RankingsPanel.Show failed: {ex.Message}"); }
                });
        }

        // Friends / buddy hex — the left-rail ghost icon (the one with the red
        // alert badge, GameObject "Button_Basic_Rectangle_H46" with no suffix).
        // Opens WorldPanel (lands on its Friends tab by default).
        //
        // The shared WireNamedRailHex approach (Button on the prefab hex root,
        // relying on raycast bubbling) would not register taps on this LEFT-rail
        // hex — likely a prefab raycast/CanvasGroup quirk that doesn't affect
        // the right-rail hexes. So this hex gets a BULLETPROOF overlay: a child
        // with its OWN Canvas (raised sortingOrder + GraphicRaycaster) so the
        // tap is evaluated on top of any lobby blocker. The sortingOrder is kept
        // modest (200) so it stays BELOW the popup panels (15000+) and can never
        // block WorldPanel once it opens.
        private static void WireFriendsButton(GameObject root)
        {
            // Create the polling hotspot FIRST and UNCONDITIONALLY — it uses a
            // fixed screen region, so it must exist even if the hex lookup below
            // fails. (This is the part that actually makes the tap work.)
            BuildStandaloneFriendsHit();

            Transform hex = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "Button_Basic_Rectangle_H46")
                { hex = rt.transform; break; }
            if (hex == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Friends ghost hex ('Button_Basic_Rectangle_H46') not found — hotspot still active.");
                return;
            }

            // Unblock ancestors (CanvasGroup / inactive) up the chain.
            Transform t = hex; int guard = 0;
            while (t != null && guard++ < 24)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; }
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }

            // "FRIENDS" caption inside the hex (skip if already present).
            const string LABEL = "Sparq_FriendsLabel";
            if (hex.Find(LABEL) == null)
            {
                var labelGO = new GameObject(LABEL, typeof(RectTransform));
                labelGO.transform.SetParent(hex, false);
                var lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0);
                lrt.anchoredPosition = new Vector2(0, 8);
                lrt.sizeDelta = new Vector2(0, 32);
                var tm = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
                tm.text = "FRIENDS";
                tm.fontSize = 22;
                tm.fontStyle = TMPro.FontStyles.Bold;
                tm.color = new Color(1f, 0.97f, 0.85f, 1f);
                tm.alignment = TMPro.TextAlignmentOptions.Center;
                tm.font = TMPro.TMP_Settings.defaultFontAsset;
                tm.raycastTarget = false;
                try { tm.outlineWidth = 0.22f; tm.outlineColor = new Color(0, 0, 0); } catch {}
            }

            Debug.Log("[HomeLobbyPanel] Wired Friends hex with bulletproof overlay → 'Button_Basic_Rectangle_H46'.");
        }

        // Click target for the FRIENDS ghost hex. The lobby prefab's ghost-hex
        // subtree does NOT participate in UI raycasting (a GlobalClickSniffer
        // confirmed "0 hits" at its screen position even with a dedicated
        // overlay canvas), so we bypass the UI event system entirely with a
        // polling component (FriendsHexHotspot) that compares Input.mousePosition
        // against the ghost's normalised region. Pure coordinate math — nothing
        // in the prefab can block it.
        private static void BuildStandaloneFriendsHit()
        {
            const string ROOT = "Sparq_FriendsHotspot";
            var prev = GameObject.Find(ROOT);
            if (prev != null) UnityEngine.Object.Destroy(prev);

            var go = new GameObject(ROOT);
            go.AddComponent<Sparq.UI.FriendsHexHotspot>();
            Debug.Log("[HomeLobbyPanel] FRIENDS hotspot (polling) created.");
        }

        /// <summary>Generic right-rail hex wirer — finds a button by name,
        /// re-enables raycasts, attaches a Button, wires onClick, and
        /// injects a small text label inside the hex.</summary>
        private static void WireNamedRailHex(GameObject root, string gameObjectName,
            string label, string logLabel, System.Action onClick)
        {
            Transform target = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == gameObjectName)
                { target = rt.transform; break; }
            if (target == null)
            {
                Debug.LogWarning($"[HomeLobbyPanel] {logLabel} hex ('{gameObjectName}') not found.");
                return;
            }
            foreach (var g in target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;

            // Ensure no ANCESTOR CanvasGroup is swallowing the click. A
            // CanvasGroup with blocksRaycasts=false or interactable=false on
            // any parent makes the whole subtree un-clickable — this is the
            // most common reason a correctly-wired Button "does nothing", and
            // would explain why one rail works while the other is dead.
            {
                Transform t = target;
                int guard = 0;
                while (t != null && guard++ < 24)
                {
                    var cg = t.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; }
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t = t.parent;
                }
            }

            // Reliable click target: ensure the hex ROOT has its own raycast
            // Image stretched to fill the whole hex. Some hexes (notably the
            // LEFT-rail ones) keep their art on a child "Bg" and have no graphic
            // on the root, so a sibling decoration (e.g. the enlarged hero
            // sprite) could sit on top and swallow the tap. A dedicated
            // full-rect catcher on top fixes that. Also raise the hex above
            // its siblings so nothing overlaps it.
            var rootImg = target.GetComponent<UnityEngine.UI.Image>();
            if (rootImg == null)
            {
                var catcher = new GameObject("Sparq_HexHit", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                catcher.transform.SetParent(target, false);
                var crt = catcher.GetComponent<RectTransform>();
                crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
                crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
                rootImg = catcher.GetComponent<UnityEngine.UI.Image>();
                rootImg.color = new Color(0, 0, 0, 0);   // invisible
                catcher.transform.SetAsFirstSibling();    // behind the icon art
            }
            rootImg.raycastTarget = true;
            target.SetAsLastSibling();   // ensure nothing overlaps the hex

            var img = rootImg;
            var btn = target.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            if (img != null) { btn.targetGraphic = img; img.raycastTarget = true; }
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"[HomeLobbyPanel] ✓ {logLabel} hex tapped.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onClick?.Invoke();
            });

            // Inject label inside the hex (bottom-anchored).
            string LABEL_NAME = "Sparq_" + logLabel + "Label";
            bool hasLabel = false;
            foreach (Transform c in target) if (c.name == LABEL_NAME) { hasLabel = true; break; }
            if (!hasLabel && !string.IsNullOrEmpty(label))
            {
                var labelGO = new GameObject(LABEL_NAME, typeof(RectTransform));
                labelGO.transform.SetParent(target, false);
                var lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0);
                lrt.anchoredPosition = new Vector2(0, 8);
                lrt.sizeDelta = new Vector2(0, 32);
                var tm = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
                tm.text = label;
                tm.fontSize = 22;
                tm.fontStyle = TMPro.FontStyles.Bold;
                tm.color = new Color(1f, 0.97f, 0.85f, 1f);
                tm.alignment = TMPro.TextAlignmentOptions.Center;
                tm.font = TMPro.TMP_Settings.defaultFontAsset;
                tm.raycastTarget = false;
                try { tm.outlineWidth = 0.22f; tm.outlineColor = new Color(0, 0, 0); } catch {}
            }

            Debug.Log($"[HomeLobbyPanel] Wired {logLabel} hex → '{gameObjectName}'.");
        }

        // ─────────────────────────────────────────────────────────────────
        // Guild button — the right-rail hex with the sword+shield icon
        // (Button_Basic_Rectangle_H46 (3) at rel ≈ 0.90, 0.62). Opens
        // GuildPanel — the empty-state landing + browse/create CTAs.
        // ─────────────────────────────────────────────────────────────────
        private static void WireGuildButton(GameObject root)
        {
            Transform target = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "Button_Basic_Rectangle_H46 (3)")
                { target = rt.transform; break; }
            if (target == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Guild hex (Button_Basic_Rectangle_H46 (3)) not found.");
                return;
            }

            // Re-enable raycasts on every child Graphic so the tap lands.
            foreach (var g in target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;

            var img = target.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = target.GetComponentInChildren<UnityEngine.UI.Image>(true);
            var btn = target.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            if (img != null) { btn.targetGraphic = img; img.raycastTarget = true; }
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] ✓ Guild hex tapped — opening GuildPanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.GuildPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] GuildPanel.Show failed: {ex.Message}"); }
            });

            // Add a small "GUILD" label under the hex so it's labeled like
            // the bottom-nav tabs. The right-rail buttons don't ship with
            // a child text node — inject a new TMP positioned below.
            const string LABEL_NAME = "Sparq_GuildLabel";
            bool hasLabel = false;
            foreach (Transform c in target) if (c.name == LABEL_NAME) { hasLabel = true; break; }
            if (!hasLabel)
            {
                // Label sits INSIDE the hex (anchored to its bottom edge,
                // 8px up) so the text is contained within the box rather
                // than hanging off underneath.
                var labelGO = new GameObject(LABEL_NAME, typeof(RectTransform));
                labelGO.transform.SetParent(target, false);
                var lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0);
                lrt.anchoredPosition = new Vector2(0, 8);
                lrt.sizeDelta = new Vector2(0, 32);
                var tm = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
                tm.text = "GUILD";
                tm.fontSize = 22;
                tm.fontStyle = TMPro.FontStyles.Bold;
                tm.color = new Color(1f, 0.97f, 0.85f, 1f);
                tm.alignment = TMPro.TextAlignmentOptions.Center;
                tm.font = TMPro.TMP_Settings.defaultFontAsset;
                tm.raycastTarget = false;
                try { tm.outlineWidth = 0.22f; tm.outlineColor = new Color(0, 0, 0); } catch {}
            }

            Debug.Log("[HomeLobbyPanel] Wired Guild hex → GuildPanel.");
        }

        // ─────────────────────────────────────────────────────────────────
        // Hide the chunky Alert_Diamond_Red / Alert_Diamond_Green badges
        // Layer Lab ships on the bottom-nav tabs (and the right-rail
        // guild hex). They're demo placeholders — not driven by real
        // notification state — and they make the nav look noisy. Real
        // notifications come from the small Alert_Dot_Red style already
        // used on the top-right hamburger.
        // ─────────────────────────────────────────────────────────────────
        private static void HideTabAlertDiamonds(GameObject root)
        {
            int hidden = 0;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == null) continue;
                var n = rt.gameObject.name;
                if (n == "Alert_Diamond_Red" || n == "Alert_Diamond_Green")
                {
                    // Only strip inside the bottom-nav tab menu + on the
                    // right-rail hexes; leave the top resource bar's
                    // Alert_Dot_Red badges alone.
                    bool inTabMenu = false;
                    bool inSideRail = false;
                    Transform p = rt.parent;
                    while (p != null)
                    {
                        var pn = p.gameObject.name;
                        if (pn == "TabMenu_BottomFlush_01") { inTabMenu = true; break; }
                        if (pn.StartsWith("Button_Basic_Rectangle_H46") ||
                            pn == "Button_Convex_Rectangle_01_l_Gray")
                        { inSideRail = true; break; }
                        p = p.parent;
                    }
                    if (inTabMenu || inSideRail)
                    {
                        rt.gameObject.SetActive(false);
                        hidden++;
                    }
                }
            }
            if (hidden > 0)
                Debug.Log($"[HomeLobbyPanel] Hid {hidden} placeholder Alert_Diamond_* badge(s).");
        }

        // ─────────────────────────────────────────────────────────────────
        // Pets tab — 5th visible hex. The Layer Lab prefab ships this slot
        // as Tab_Disable with a padlock overlay; we hide the lock, re-enable
        // raycasts, and wire it to open PetPanel.
        // ─────────────────────────────────────────────────────────────────
        private static void WirePetsTab(GameObject root)
        {
            // Unlock the visual — hide the Icon_Lock overlay inside
            // Tab_Disable so the hex no longer reads as "disabled".
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == null || rt.gameObject.name != "Tab_Disable") continue;
                foreach (var c in rt.GetComponentsInChildren<RectTransform>(true))
                    if (c != null && c.gameObject.name == "Icon_Lock") c.gameObject.SetActive(false);
                break;
            }

            // Wire via the shared helper. The existing icon is the dungeon
            // sprite (doesn't match "pet" keywords) — fall back to the
            // 5th visible-index slot, which is Tab_Disable.
            WireTabByIconOrIndex(root,
                new[] { "pet", "paw", "cat", "dog" },
                fallbackVisibleIndex: 4,
                label: "Pets",
                labelOverride: "PETS",
                onClick: () => {
                    // Route to the Tamagotchi care panel — that's the
                    // primary loop. The full roster / equipment / shop
                    // (PetPanel.Show) is reachable from inside via the
                    // "Manage Pets" button.
                    try { Sparq.UI.PetCarePanel.Show(); }
                    catch (System.Exception ex)
                    { Debug.LogError($"[HomeLobbyPanel] PetCarePanel.Show failed: {ex.Message}"); }
                });
        }

        // ─────────────────────────────────────────────────────────────────
        // Store tab — 1st visible hex (Layer Lab "Shop" icon, the little
        // red-roofed house). Wired to PopupManager.OpenShop().
        // ─────────────────────────────────────────────────────────────────
        // Top resource bar "+" buttons. The Layer Lab prefab ships these as
        // pure decoration (Bg + Border + Icon, no Button component). We walk
        // every ResourceBar_*/Button_Add under the prefab, add a Button if
        // missing, and route the tap to the StorePanel on the matching tab.
        private static void WireResourceBarButtons(GameObject root)
        {
            int wired = 0;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == null || rt.gameObject.name != "Button_Add") continue;

                // Identify which currency this Add button belongs to via parent name.
                string parentName = rt.parent != null ? rt.parent.name : "";
                Sparq.UI.StorePanel.Cat target;
                if      (parentName.Contains("Coin"))   target = Sparq.UI.StorePanel.Cat.Coins;
                else if (parentName.Contains("Gem"))    target = Sparq.UI.StorePanel.Cat.Gems;
                else if (parentName.Contains("Energy")) target = Sparq.UI.StorePanel.Cat.Featured;
                else continue;   // unknown — skip

                // Make sure raycasts land somewhere clickable.
                var img = rt.gameObject.GetComponent<Image>();
                if (img == null) img = rt.gameObject.GetComponentInChildren<Image>(true);
                if (img != null) img.raycastTarget = true;

                var btn = rt.gameObject.GetComponent<Button>();
                if (btn == null) btn = rt.gameObject.AddComponent<Button>();
                if (img != null) btn.targetGraphic = img;
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                var capturedTarget = target;
                btn.onClick.AddListener(() => {
                    try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                    try { Sparq.UI.StorePanel.Show(capturedTarget); }
                    catch (System.Exception ex)
                    { Debug.LogError($"[HomeLobbyPanel] Top-bar + tap → Store failed: {ex.Message}"); }
                });
                wired++;
            }
            Debug.Log($"[HomeLobbyPanel] Wired {wired} top-bar '+' buttons to the Store.");
        }

        private static void WireStoreTab(GameObject root)
        {
            WireTabByIconOrIndex(root,
                new[] { "shop", "store", "house", "home" },
                fallbackVisibleIndex: 0,
                label: "Store",
                labelOverride: "STORE",
                onClick: () => {
                    // Direct to the procedural StorePanel — the old
                    // PopupManager.OpenShop() relied on an unassigned prefab
                    // and silently did nothing.
                    try { Sparq.UI.StorePanel.Show(); }
                    catch (System.Exception ex)
                    { Debug.LogError($"[HomeLobbyPanel] StorePanel.Show failed: {ex.Message}"); }
                });
        }

        // ─────────────────────────────────────────────────────────────────
        // Weapons tab (3rd visible hex — the crossed-swords) → Equipment.
        // Bag tab (4th visible hex — the chest with red badge) → BagPanel.
        // Both addressed by SIBLING ORDER rather than name, because the
        // TabMenu_BottomFlush_01 prefab mixes Tab_Nomal, Tab_Nomal (1..2),
        // Tab_Select, Tab_Disable — names don't reflect visual order.
        // ─────────────────────────────────────────────────────────────────
        private static void WireWeaponsTab(GameObject root)
        {
            // Match the tab whose Icon sprite name contains any of these
            // keywords. Falls back to visible index 2 if no icon matches.
            WireTabByIconOrIndex(root,
                new[] { "sword", "weapon", "equip", "battle", "combat" },
                fallbackVisibleIndex: 2,
                label: "Weapons",
                labelOverride: "WEAPONS",
                onClick: () => {
                    try { Sparq.UI.EquipmentPanel.Show(); }
                    catch (System.Exception ex)
                    { Debug.LogError($"[HomeLobbyPanel] EquipmentPanel.Show failed: {ex.Message}"); }
                });
        }

        private static void WireBagTab(GameObject root)
        {
            WireTabByIconOrIndex(root,
                new[] { "chest", "bag", "inven", "loot", "treasure" },
                fallbackVisibleIndex: 3,
                label: "Bag",
                labelOverride: "BAG",
                onClick: () => {
                    try { Sparq.UI.BagPanel.Show(); }
                    catch (System.Exception ex)
                    { Debug.LogError($"[HomeLobbyPanel] BagPanel.Show failed: {ex.Message}"); }
                });
        }

        /// <summary>Find a tab by its Icon sprite name (case-insensitive
        /// keyword match) and wire it. Falls back to a visible-position
        /// index if no icon matches any keyword.</summary>
        private static void WireTabByIconOrIndex(GameObject root,
            string[] iconKeywords, int fallbackVisibleIndex,
            string label, string labelOverride, System.Action onClick)
        {
            Transform tabMenu = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "TabMenu_BottomFlush_01")
                { tabMenu = rt.transform; break; }
            if (tabMenu == null)
            {
                Debug.LogWarning($"[HomeLobbyPanel] 'TabMenu_BottomFlush_01' not found — {label} tab not wired.");
                return;
            }

            Canvas.ForceUpdateCanvases();
            var ordered = new List<(Transform t, float x, string sprite)>();
            for (int i = 0; i < tabMenu.childCount; i++)
            {
                var c = tabMenu.GetChild(i);
                if (c == null) continue;
                if (!c.gameObject.name.StartsWith("Tab_")) continue;
                var rt2 = c as RectTransform ?? c.GetComponent<RectTransform>();
                if (rt2 == null) continue;
                string spriteName = "";
                foreach (var im in c.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (im == null || im.gameObject.name != "Icon") continue;
                    if (im.sprite != null) spriteName = im.sprite.name;
                    break;
                }
                ordered.Add((c, rt2.position.x, spriteName));
            }
            ordered.Sort((a, b) => a.x.CompareTo(b.x));

            // Diagnostic dump.
            var posOrder = new System.Text.StringBuilder();
            for (int i = 0; i < ordered.Count; i++)
                posOrder.Append($"[{i}]={ordered[i].t.name}({ordered[i].sprite})@{ordered[i].x:F0}  ");
            Debug.Log($"[HomeLobbyPanel] {label} search — Tab order L→R: {posOrder}");

            // Keyword match first.
            Transform tab = null;
            string matchedBy = "";
            foreach (var entry in ordered)
            {
                if (string.IsNullOrEmpty(entry.sprite)) continue;
                var sl = entry.sprite.ToLowerInvariant();
                foreach (var kw in iconKeywords)
                {
                    if (string.IsNullOrEmpty(kw)) continue;
                    if (sl.Contains(kw.ToLowerInvariant()))
                    {
                        tab = entry.t;
                        matchedBy = $"icon-keyword '{kw}' → '{entry.sprite}'";
                        break;
                    }
                }
                if (tab != null) break;
            }

            // Fallback to position index.
            if (tab == null && fallbackVisibleIndex >= 0 && fallbackVisibleIndex < ordered.Count)
            {
                tab = ordered[fallbackVisibleIndex].t;
                matchedBy = $"fallback visible index {fallbackVisibleIndex}";
            }

            if (tab == null)
            {
                Debug.LogWarning($"[HomeLobbyPanel] No {label} tab found by icon or fallback index.");
                return;
            }

            // Don't clobber an already-wired tab (e.g. journal).
            var existing = tab.GetComponent<UnityEngine.UI.Button>();
            if (existing != null && existing.onClick.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning($"[HomeLobbyPanel] {label} target tab '{tab.name}' already has a wired Button — leaving it alone.");
                return;
            }

            foreach (var g in tab.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;

            var btn = existing ?? tab.gameObject.AddComponent<UnityEngine.UI.Button>();
            var img = tab.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = tab.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (img != null) btn.targetGraphic = img;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"[HomeLobbyPanel] ✓ {label} tab tapped ('{tab.name}').");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onClick?.Invoke();
            });

            if (!string.IsNullOrEmpty(labelOverride))
                SetTabLabel(tab, labelOverride);

            Debug.Log($"[HomeLobbyPanel] Wired {label} tab → '{tab.name}' (matched by {matchedBy}).");
        }

        private static void WireTabBySiblingIndex(GameObject root, int visibleIndex, string label, System.Action onClick, string labelOverride = null)
        {
            Transform tabMenu = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "TabMenu_BottomFlush_01")
                { tabMenu = rt.transform; break; }
            if (tabMenu == null)
            {
                Debug.LogWarning($"[HomeLobbyPanel] 'TabMenu_BottomFlush_01' not found — {label} tab not wired.");
                return;
            }

            // Force layout so each tab's world position reflects its real
            // on-screen X, then sort by X left-to-right. Sibling order in
            // the prefab is NOT visual order (Tab_Nomal / Tab_Select /
            // Tab_Disable are scattered) so this is the only reliable map.
            Canvas.ForceUpdateCanvases();
            var ordered = new List<(Transform t, float x)>();
            for (int i = 0; i < tabMenu.childCount; i++)
            {
                var c = tabMenu.GetChild(i);
                if (c == null) continue;
                if (!c.gameObject.name.StartsWith("Tab_")) continue;
                var rt2 = c as RectTransform ?? c.GetComponent<RectTransform>();
                if (rt2 == null) continue;
                ordered.Add((c, rt2.position.x));
            }
            ordered.Sort((a, b) => a.x.CompareTo(b.x));

            Transform tab = null;
            if (visibleIndex >= 0 && visibleIndex < ordered.Count) tab = ordered[visibleIndex].t;
            if (tab == null)
            {
                Debug.LogWarning($"[HomeLobbyPanel] No {label} hex found at visible index {visibleIndex} (found {ordered.Count} tabs).");
                return;
            }
            // Diagnostic so we can see exactly which tab took the wire,
            // and crucially WHICH ICON SPRITE the tab is displaying — the
            // prefab puts sword/chest sprites under arbitrary tab names.
            var posOrder = new System.Text.StringBuilder();
            for (int i = 0; i < ordered.Count; i++)
            {
                string spriteName = "?";
                foreach (var im in ordered[i].t.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (im == null || im.gameObject.name != "Icon") continue;
                    if (im.sprite != null) spriteName = im.sprite.name;
                    break;
                }
                posOrder.Append($"[{i}]={ordered[i].t.name}({spriteName})@{ordered[i].x:F0}  ");
            }
            Debug.Log($"[HomeLobbyPanel] Tab order L→R: {posOrder}");

            // Don't clobber the journal wire — only re-wire if no Button is
            // attached yet (the journal wire installs its own Button).
            var existing = tab.GetComponent<UnityEngine.UI.Button>();
            if (existing != null && existing.onClick.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning($"[HomeLobbyPanel] {label} hex (sibling {visibleIndex}, '{tab.name}') already has a wired Button — leaving it alone.");
                return;
            }

            foreach (var g in tab.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;

            var btn = existing ?? tab.gameObject.AddComponent<UnityEngine.UI.Button>();
            var img = tab.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = tab.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (img != null) btn.targetGraphic = img;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"[HomeLobbyPanel] ✓ {label} tab tapped (sibling {visibleIndex}, '{tab.name}').");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onClick?.Invoke();
            });

            // Replace the Layer Lab placeholder "TEXT" label with a real one.
            if (!string.IsNullOrEmpty(labelOverride))
                SetTabLabel(tab, labelOverride);

            Debug.Log($"[HomeLobbyPanel] Wired {label} tab (sibling index {visibleIndex} → '{tab.name}').");
        }

        /// <summary>Updates the TMP/Text label on a Layer Lab tab. The
        /// prefab tabs ship with a child text reading "TEXT" — we either
        /// rewrite it to a real label (e.g. "BAG") or hide it if labelText
        /// is empty. Safe if no text child exists.</summary>
        private static void SetTabLabel(Transform tab, string labelText)
        {
            try
            {
                // Try TMP first (the Layer Lab Fantasy Hero pack uses TMP).
                foreach (var tmp in tab.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (string.IsNullOrEmpty(labelText)) { tmp.gameObject.SetActive(false); continue; }
                    tmp.text = labelText;
                    tmp.fontStyle = TMPro.FontStyles.Bold;
                    tmp.gameObject.SetActive(true);
                }
                // Fallback to legacy Text if the prefab uses that instead.
                foreach (var t in tab.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    if (t == null) continue;
                    if (string.IsNullOrEmpty(labelText)) { t.gameObject.SetActive(false); continue; }
                    t.text = labelText;
                    t.fontStyle = FontStyle.Bold;
                    t.gameObject.SetActive(true);
                }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] SetTabLabel failed on '{tab.name}': {ex.Message}"); }
        }

        // ─────────────────────────────────────────────────────────────────
        // Daily Quests tile — repurposes the prefab's unused brown-bordered
        // "event ticket" frame (BaseFrame_Basic_Rectangle_H40_InnerLLIne_
        // Brown) in the middle of the upper row as the daily-quests entry
        // point. Tap → QuestsPanel.Show(). Also adds a "DAILY QUESTS"
        // label and drives the yellow fill bar from today's completion %.
        // ─────────────────────────────────────────────────────────────────
        private static void WireDailyQuestsTile(GameObject root)
        {
            Transform tile = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "BaseFrame_Basic_Rectangle_H40_InnerLLIne_Brown")
                { tile = rt.transform; break; }
            if (tile == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Daily-quests tile (brown frame) not found.");
                return;
            }

            // The chest tile that used to occupy the LEFT of this row was
            // removed, leaving the DAILY QUESTS frame floating right-of-centre.
            // Shift it left so it reads as centred in the freed space.
            var tileRT = tile as RectTransform;
            if (tileRT != null)
                tileRT.anchoredPosition += new Vector2(-150f, 0f);

            // Re-enable raycasts on every graphic so taps land anywhere on the tile.
            foreach (var g in tile.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;

            // Mount the Button on the tile itself.
            var img = tile.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = tile.GetComponentInChildren<UnityEngine.UI.Image>(true);
            var btn = tile.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = tile.gameObject.AddComponent<UnityEngine.UI.Button>();
            if (img != null) { btn.targetGraphic = img; img.raycastTarget = true; }
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] ✓ Daily-quests tile tapped — opening QuestsPanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.QuestsPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] QuestsPanel.Show failed: {ex.Message}"); }
            });

            // Inject "DAILY QUESTS" header at the top of the tile, and a
            // row of 3 small spoons below it that fills to reflect today's
            // Spoon Check level (0 grey if not checked, 1/2/3 gold).
            const string LABEL_NAME = "DailyQuestsLabel";
            bool hasLabel = false;
            foreach (Transform c in tile)
                if (c.name == LABEL_NAME) { hasLabel = true; break; }
            if (!hasLabel)
            {
                var labelGO = new GameObject(LABEL_NAME, typeof(RectTransform));
                labelGO.transform.SetParent(tile, false);
                var lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1);
                lrt.pivot = new Vector2(0.5f, 1);
                lrt.offsetMin = new Vector2(16, -52); lrt.offsetMax = new Vector2(-16, -6);
                var tm = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
                tm.text = "DAILY QUESTS";
                tm.fontSize = 32;
                tm.fontStyle = TMPro.FontStyles.Bold;
                tm.color = new Color(0.99f, 0.99f, 0.98f, 1f);
                tm.alignment = TMPro.TextAlignmentOptions.Center;
                tm.font = TMPro.TMP_Settings.defaultFontAsset;
                tm.raycastTarget = false;
                try { tm.outlineWidth = 0.25f; tm.outlineColor = new Color(0, 0, 0); } catch {}
            }

            EnsureDailyQuestsSpoons(tile);
            UpdateDailyQuestsFill(tile);
            Debug.Log("[HomeLobbyPanel] Wired daily-quests tile (brown frame → QuestsPanel).");
        }

        /// <summary>Builds 3 mini-spoons at the bottom of the daily-quests
        /// tile, with N filled in gold (today's Spoon level: Low=1, Med=2,
        /// High=3, none=0). Idempotent — only builds the row once.</summary>
        private static void EnsureDailyQuestsSpoons(Transform tile)
        {
            const string NAME = "DailyQuestsSpoons";
            foreach (Transform c in tile) if (c.name == NAME) return;

            // Today's energy → filled spoon count.
            int filled = 0;
            try
            {
                if (Sparq.UI.SpoonCheckPanel.CheckedToday())
                {
                    switch (Sparq.Systems.QuestManager.InferEnergyLevel())
                    {
                        case Sparq.Models.EnergyLevel.Low:    filled = 1; break;
                        case Sparq.Models.EnergyLevel.Medium: filled = 2; break;
                        case Sparq.Models.EnergyLevel.High:   filled = 3; break;
                    }
                }
            }
            catch {}

            var holderGO = new GameObject(NAME,
                typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
            holderGO.transform.SetParent(tile, false);
            var hrt = holderGO.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 0); hrt.anchorMax = new Vector2(0.5f, 0);
            hrt.pivot = new Vector2(0.5f, 0);
            hrt.anchoredPosition = new Vector2(0, 6);
            hrt.sizeDelta = new Vector2(160, 36);
            var hlg = holderGO.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = -6;                          // tight cluster, like the SpoonCheckPanel
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false; hlg.childControlHeight = false;

            // Filled = today's gold; empty = translucent grey so the slot still reads.
            var filledColor = new Color(0.96f, 0.66f, 0.10f, 1f);
            var emptyColor  = new Color(1f, 1f, 1f, 0.22f);
            for (int i = 0; i < 3; i++)
                BuildMiniSpoon(holderGO.transform, i < filled ? filledColor : emptyColor);
        }

        // Same shape as SpoonCheckPanel.BuildSpoon but small and inlined so
        // the lobby panel has no cross-panel dependency.
        private static void BuildMiniSpoon(Transform parent, Color color)
        {
            var sprite = LoadLobbySprite("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png");

            var spoon = new GameObject("Spoon",
                typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            spoon.transform.SetParent(parent, false);
            var le = spoon.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = 22; le.preferredHeight = 36;

            var bowl = new GameObject("Bowl",
                typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bowl.transform.SetParent(spoon.transform, false);
            var bRT = bowl.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 1); bRT.anchorMax = new Vector2(0.5f, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.anchoredPosition = new Vector2(0, 0);
            bRT.sizeDelta = new Vector2(22, 14);
            var bImg = bowl.GetComponent<UnityEngine.UI.Image>();
            if (sprite != null) bImg.sprite = sprite;
            bImg.color = color; bImg.raycastTarget = false;

            var handle = new GameObject("Handle",
                typeof(RectTransform), typeof(UnityEngine.UI.Image));
            handle.transform.SetParent(spoon.transform, false);
            var hRT = handle.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.5f, 1); hRT.anchorMax = new Vector2(0.5f, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -12);
            hRT.sizeDelta = new Vector2(7, 22);
            var hImg = handle.GetComponent<UnityEngine.UI.Image>();
            if (sprite != null) hImg.sprite = sprite;
            hImg.color = color; hImg.raycastTarget = false;
        }

        private static Sprite LoadLobbySprite(string path)
        {
            // SpriteLoader handles Resources-first (APK) + AssetDatabase (editor).
            // Was previously editor-only — silenced lobby decoration sprites in builds.
            try { return Sparq.Core.SpriteLoader.Load(path); }
            catch { return null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // Reminders button — the upper-right gray icon-button on the lobby
        // (Button_Convex_Rectangle_01_l_Gray with the red "9" badge). Wired
        // to open RemindPanel; the badge count is driven from RemindService.
        // ─────────────────────────────────────────────────────────────────
        private static void WireRemindersButton(GameObject root)
        {
            Transform target = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "Button_Convex_Rectangle_01_l_Gray")
                { target = rt.transform; break; }
            if (target == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Reminders button (Button_Convex_Rectangle_01_l_Gray) not found.");
                return;
            }

            // Re-enable raycasts on every child Graphic so the tap lands.
            foreach (var g in target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;

            var img = target.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = target.GetComponentInChildren<UnityEngine.UI.Image>(true);
            var btn = target.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            if (img != null) { btn.targetGraphic = img; img.raycastTarget = true; }
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] ✓ Reminders button tapped — opening RemindPanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.RemindPanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] RemindPanel.Show failed: {ex.Message}"); }
            });

            // Drive the red diamond badge from the count of pending reminders
            // (active customReminders). If the count is 0, hide the badge.
            UpdateRemindersBadge(target);

            // Auto-refresh the badge whenever a reminder is added / toggled
            // / deleted anywhere in the app.
            if (_remindBadgeHandler != null)
                Sparq.Systems.RemindService.OnChanged -= _remindBadgeHandler;
            _remindBadgeHandler = () => { if (target != null) UpdateRemindersBadge(target); };
            Sparq.Systems.RemindService.OnChanged += _remindBadgeHandler;

            Debug.Log("[HomeLobbyPanel] Wired Reminders button (Button_Convex_Rectangle_01_l_Gray → RemindPanel).");
        }

        private static System.Action _remindBadgeHandler;

        // ─────────────────────────────────────────────────────────────────
        // Player profile bar — the empty strip under the resource bar
        // (Layer Lab BaseFrame_Basic_Rectangle_H40_Divided). Wires the
        // existing PlayerProfileBar MonoBehaviour with a procedurally-built
        // avatar / name / level / XP-bar layout. Tap → AvatarPickerPanel
        // (preset gallery + upload option).
        // ─────────────────────────────────────────────────────────────────
        private static void WirePlayerProfileBar(GameObject root)
        {
            Transform bar = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt != null && rt.gameObject.name == "BaseFrame_Basic_Rectangle_H40_Divided")
                { bar = rt.transform; break; }
            if (bar == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Profile bar (BaseFrame_Basic_Rectangle_H40_Divided) not found.");
                return;
            }

            // Already wired on a previous Show()? Don't duplicate children.
            if (bar.GetComponent<Sparq.UI.PlayerProfileBar>() != null) return;

            // Re-enable the bar (Phase 1B / 1C of DeclutterLobbyPrefab hides
            // it because the prefab ships with placeholder text "99,999"
            // and a "3" level pill that match the declutter rules). Walk
            // ancestors in case a parent was also disabled.
            bar.gameObject.SetActive(true);
            for (var p = bar.parent; p != null && p != root.transform; p = p.parent)
                if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);

            // The Layer Lab bar ships at 98px tall — too short for an avatar
            // big enough to read. Bump the height (and pull it up slightly
            // toward the resource bar so it doesn't crowd the DAILY QUESTS
            // tile below).
            var barRT = bar.GetComponent<RectTransform>();
            if (barRT != null)
            {
                var sd = barRT.sizeDelta; sd.y = 150f; barRT.sizeDelta = sd;
            }

            // Hide the bar's leftover Layer Lab placeholder children so our
            // wired UI isn't competing visually with the demo trophy count,
            // blue "3" level pill, and decorative profile mask.
            string[] hidePlaceholders = {
                "InnerBg", "ProfileFrame_Empty", "Icon_Trophy",
                "ConvexFrame_Crimped_01_H65_Blue", "Alert_Dot_Red",
            };
            foreach (var rt in bar.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == null) continue;
                foreach (var pat in hidePlaceholders)
                    if (rt.gameObject.name == pat)
                    { rt.gameObject.SetActive(false); break; }
            }

            // Root tap-target: ensure an Image + Button so the whole bar opens
            // the avatar picker on tap.
            var rootImg = bar.GetComponent<UnityEngine.UI.Image>();
            if (rootImg == null) rootImg = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
            rootImg.raycastTarget = true;
            foreach (var g in bar.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null) g.raycastTarget = true;
            var btn = bar.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = bar.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = rootImg;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            // ── Procedural children (left → right): avatar, name+level, XP bar ──

            // Avatar — tappable on its own so it can open the avatar picker
            // independently of the bar's "open profile" tap. Sized to fit
            // the bar's 98px height (was 110, overflowing).
            var avatarGO = new GameObject("Sparq_Avatar",
                typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            avatarGO.transform.SetParent(bar, false);
            var aRT = avatarGO.GetComponent<RectTransform>();
            aRT.anchorMin = new Vector2(0, 0.5f); aRT.anchorMax = new Vector2(0, 0.5f);
            aRT.pivot = new Vector2(0, 0.5f);
            aRT.anchoredPosition = new Vector2(14, 0);
            aRT.sizeDelta = new Vector2(130, 130);
            var aImg = avatarGO.GetComponent<UnityEngine.UI.Image>();
            aImg.color = new Color(0.30f, 0.30f, 0.40f, 1f);
            aImg.raycastTarget = true;
            // preserveAspect=false so a tall hero portrait fills the square
            // tile instead of being a thin slice — slight squish is far more
            // readable at small sizes.
            aImg.preserveAspect = false;
            var aBtn = avatarGO.GetComponent<UnityEngine.UI.Button>();
            aBtn.targetGraphic = aImg;
            aBtn.interactable = true;
            aBtn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] Avatar tapped — opening AvatarPicker.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.AvatarPickerPanel.Open(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] AvatarPicker.Open failed: {ex.Message}"); }
            });

            // Gold "Lv N" chip overlapping the avatar's lower-right corner
            // (matches the Top Heroes badge style — level always visible at a glance).
            var lvChipGO = new GameObject("Sparq_LvChip",
                typeof(RectTransform), typeof(UnityEngine.UI.Image));
            lvChipGO.transform.SetParent(avatarGO.transform, false);
            var lcRT = lvChipGO.GetComponent<RectTransform>();
            lcRT.anchorMin = new Vector2(1, 0); lcRT.anchorMax = new Vector2(1, 0);
            lcRT.pivot = new Vector2(1, 0);
            lcRT.anchoredPosition = new Vector2(8, -6);
            lcRT.sizeDelta = new Vector2(82, 42);
            lvChipGO.GetComponent<UnityEngine.UI.Image>().color = new Color(0.99f, 0.78f, 0.20f, 1f);
            lvChipGO.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var lcTxtGO = new GameObject("L", typeof(RectTransform));
            lcTxtGO.transform.SetParent(lvChipGO.transform, false);
            var lctRT = lcTxtGO.GetComponent<RectTransform>();
            lctRT.anchorMin = Vector2.zero; lctRT.anchorMax = Vector2.one;
            lctRT.offsetMin = Vector2.zero; lctRT.offsetMax = Vector2.zero;
            var lcTmp = lcTxtGO.AddComponent<TMPro.TextMeshProUGUI>();
            int curLevel = 1;
            try { curLevel = Mathf.Max(1, Sparq.Core.SaveService.Data?.level ?? 1); } catch {}
            lcTmp.text = "Lv " + curLevel;
            lcTmp.fontSize = 28;
            lcTmp.fontStyle = TMPro.FontStyles.Bold;
            lcTmp.color = new Color(0.13f, 0.10f, 0.20f, 1f);
            lcTmp.alignment = TMPro.TextAlignmentOptions.Center;
            lcTmp.font = TMPro.TMP_Settings.defaultFontAsset;
            lcTmp.raycastTarget = false;

            // Player name — single full-height label in the middle column
            // (the level chip on the avatar replaces the separate level line).
            var nameGO = new GameObject("Sparq_Name", typeof(RectTransform));
            nameGO.transform.SetParent(bar, false);
            var nRT = nameGO.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(0.45f, 1);
            nRT.offsetMin = new Vector2(160, 4); nRT.offsetMax = new Vector2(0, -4);
            var nLbl = nameGO.AddComponent<TMPro.TextMeshProUGUI>();
            nLbl.text = "Player";
            nLbl.fontSize = 48;
            nLbl.fontStyle = TMPro.FontStyles.Bold;
            nLbl.color = new Color(1f, 0.97f, 0.85f, 1f);
            nLbl.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            nLbl.font = TMPro.TMP_Settings.defaultFontAsset;
            nLbl.raycastTarget = false;
            try { nLbl.outlineWidth = 0.22f; nLbl.outlineColor = new Color(0, 0, 0); } catch {}

            // The level label is kept as a hidden TMP — PlayerProfileBar.Refresh
            // writes to it; we just don't render it because the gold "Lv N"
            // chip on the avatar shows the same info more clearly.
            var lvlGO = new GameObject("Sparq_Lvl", typeof(RectTransform));
            lvlGO.transform.SetParent(bar, false);
            var lRT = lvlGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.zero;
            lRT.sizeDelta = Vector2.zero;
            var lLbl = lvlGO.AddComponent<TMPro.TextMeshProUGUI>();
            lLbl.text = "";
            lLbl.color = new Color(0, 0, 0, 0);
            lLbl.font = TMPro.TMP_Settings.defaultFontAsset;
            lLbl.raycastTarget = false;

            // XP bar background — right half of the bar.
            var xpBgGO = new GameObject("Sparq_XpBg",
                typeof(RectTransform), typeof(UnityEngine.UI.Image));
            xpBgGO.transform.SetParent(bar, false);
            var xbRT = xpBgGO.GetComponent<RectTransform>();
            xbRT.anchorMin = new Vector2(0.46f, 0.5f); xbRT.anchorMax = new Vector2(1, 0.5f);
            xbRT.pivot = new Vector2(0, 0.5f);
            xbRT.offsetMin = new Vector2(12, -40); xbRT.offsetMax = new Vector2(-26, 40);
            var xbImg = xpBgGO.GetComponent<UnityEngine.UI.Image>();
            xbImg.color = new Color(0.14f, 0.12f, 0.26f, 1f);
            xbImg.raycastTarget = false;

            // XP fill (Image type Filled, horizontal).
            var xpFillGO = new GameObject("Sparq_XpFill",
                typeof(RectTransform), typeof(UnityEngine.UI.Image));
            xpFillGO.transform.SetParent(xpBgGO.transform, false);
            var xfRT = xpFillGO.GetComponent<RectTransform>();
            xfRT.anchorMin = Vector2.zero; xfRT.anchorMax = Vector2.one;
            xfRT.offsetMin = new Vector2(3, 3); xfRT.offsetMax = new Vector2(-3, -3);
            var xfImg = xpFillGO.GetComponent<UnityEngine.UI.Image>();
            xfImg.color = new Color(1.0f, 0.78f, 0.20f, 1f);
            xfImg.type = UnityEngine.UI.Image.Type.Filled;
            xfImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            xfImg.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            xfImg.raycastTarget = false;

            // XP text overlay.
            var xpTxtGO = new GameObject("Sparq_XpText", typeof(RectTransform));
            xpTxtGO.transform.SetParent(xpBgGO.transform, false);
            var xtRT = xpTxtGO.GetComponent<RectTransform>();
            xtRT.anchorMin = Vector2.zero; xtRT.anchorMax = Vector2.one;
            xtRT.offsetMin = Vector2.zero; xtRT.offsetMax = Vector2.zero;
            var xtLbl = xpTxtGO.AddComponent<TMPro.TextMeshProUGUI>();
            xtLbl.text = "0 / 0";
            xtLbl.fontSize = 34;
            xtLbl.fontStyle = TMPro.FontStyles.Bold;
            xtLbl.color = new Color(1f, 0.97f, 0.85f, 1f);
            xtLbl.alignment = TMPro.TextAlignmentOptions.Center;
            xtLbl.font = TMPro.TMP_Settings.defaultFontAsset;
            xtLbl.raycastTarget = false;
            try { xtLbl.outlineWidth = 0.18f; xtLbl.outlineColor = new Color(0, 0, 0); } catch {}

            // Attach the existing PlayerProfileBar component for refresh-on-
            // save plumbing. openPickerOnTap=false so it doesn't install its
            // own AvatarPicker listener — we wire ProfilePanel to the bar
            // tap and AvatarPicker to the avatar tap separately (above).
            var ppb = bar.gameObject.AddComponent<Sparq.UI.PlayerProfileBar>();
            ppb.openPickerOnTap = false;
            ppb.avatarImage = aImg;
            ppb.nameLabel = nLbl;
            ppb.levelLabel = lLbl;
            ppb.xpFill = xfImg;
            ppb.xpLabel = xtLbl;
            ppb.rootButton = btn;

            // Avatar fallback — Resources/Avatars/ isn't shipped yet, so
            // PresetAvatars.LoadSprite() returns null for every preset id.
            // Assign the player's hero chibi portrait to defaultAvatar so
            // PlayerProfileBar.Refresh() uses it on every re-fire (otherwise
            // SaveService.OnSaved/OnLoaded would wipe a directly-set sprite).
            try
            {
                var heroDef = Sparq.Systems.HeroClassResolver.Resolve();
                var loaded = Sparq.UI.HeroPortrait.LoadIdle(heroDef, excludeWeapon: true);
                if (loaded.ok && loaded.sprite != null)
                    ppb.defaultAvatar = loaded.sprite;
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Avatar hero fallback failed: {ex.Message}"); }

            // Now run the initial Refresh — it'll pick custom upload first,
            // then preset, then fall back to ppb.defaultAvatar (the hero).
            ppb.Refresh();
            if (aImg.sprite != null) aImg.color = Color.white;

            // Bar tap (anywhere except the avatar, which has its own Button)
            // opens the full ProfilePanel — Sparq's "Lord Details" equivalent.
            btn.onClick.AddListener(() => {
                Debug.Log("[HomeLobbyPanel] Profile bar tapped — opening ProfilePanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try { Sparq.UI.ProfilePanel.Show(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HomeLobbyPanel] ProfilePanel.Show failed: {ex.Message}"); }
            });

            Debug.Log("[HomeLobbyPanel] Wired Player Profile Bar — avatar (→ AvatarPicker), bar (→ ProfilePanel).");
        }

        private static void UpdateRemindersBadge(Transform target)
        {
            try
            {
                // Pull from RemindService (the source of truth that the
                // alert runner also reads) rather than the legacy
                // PlayerData.customReminders list.
                int count = 0;
                var all = Sparq.Systems.RemindService.All();
                if (all != null) foreach (var r in all) if (r != null && r.enabled) count++;

                // Find the badge group and toggle visibility.
                Transform badge = null;
                foreach (var rt in target.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt != null && rt.gameObject.name == "Alert_Diamond_Red")
                    { badge = rt.transform; break; }
                }
                if (badge == null) return;
                badge.gameObject.SetActive(count > 0);

                // Update the count text if a TMP child exists; otherwise hunt
                // for a Text component.
                foreach (var tm in badge.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if (tm != null) tm.text = count.ToString();
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] Reminders badge update failed: {ex.Message}"); }
        }

        /// <summary>Updates the brown-frame's yellow slider fill to reflect
        /// today's completed / total quest ratio. Called once at lobby load.</summary>
        private static void UpdateDailyQuestsFill(Transform tile)
        {
            try
            {
                var data = Sparq.Core.SaveService.Data;
                int done  = data?.completedToday ?? 0;
                int total = data?.customTasks?.Count ?? 0;
                if (total <= 0) return;
                float ratio = Mathf.Clamp01((float)done / total);

                // The Layer Lab slider exposes Fill_Yellow whose width we can
                // drive by anchoring it via the existing Slider's value, OR
                // by directly resizing the fill if the Slider component is
                // present.
                var slider = tile.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                if (slider != null) { slider.value = ratio; return; }

                // Fallback: poke the Fill_Yellow rect directly.
                foreach (var rt in tile.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt != null && rt.gameObject.name == "Fill_Yellow")
                    {
                        rt.anchorMin = new Vector2(0, 0);
                        rt.anchorMax = new Vector2(ratio, 1);
                        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[HomeLobbyPanel] UpdateDailyQuestsFill failed: {ex.Message}"); }
        }

        // Move Karu's nameplate group (name + Lv pill + XP bar) to the user-
        // marked spot on the platform. Finds the nameplate by locating his
        // name TMP_Text in the lower half of the screen, then walking up to
        // find the ancestor that ALSO contains a Slider (the XP bar) — that's
        // the smallest container with the whole nameplate.
        private static void MoveKaruNameplate(GameObject lobbyRoot)
        {
            #if UNITY_EDITOR
            string playerName = "Karu";
            try { var d = Sparq.Core.SaveService.Data;
                  if (d != null && !string.IsNullOrEmpty(d.petName)) playerName = d.petName; } catch {}

            // Find lower-screen "Karu" text (the showcase one, not the top banner)
            TMP_Text karuNameTmp = null;
            foreach (var tm in lobbyRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tm == null) continue;
                string txt = (tm.text ?? "").Trim();
                if (txt != playerName) continue;
                Vector3[] corners = new Vector3[4];
                tm.rectTransform.GetWorldCorners(corners);
                Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
                if (screen.y < Screen.height * 0.5f) { karuNameTmp = tm; break; }
            }
            if (karuNameTmp == null)
            {
                Debug.LogWarning("[HomeLobbyPanel] Karu nameplate not found — skipping move.");
                return;
            }

            // Walk up TIGHT (max 3 hops) to find the smallest container with
            // both the name AND a Slider (the XP bar). 3 hops keeps us inside
            // the nameplate group instead of escaping to the whole lobby.
            RectTransform plate = null;
            Transform t = karuNameTmp.transform.parent;
            int hops = 0;
            while (t != null && hops < 3)
            {
                if (t.GetComponentInChildren<Slider>(true) != null)
                {
                    plate = t as RectTransform;
                    break;
                }
                t = t.parent;
                hops++;
            }
            // Fallback: just move the name's immediate parent if nothing matched
            if (plate == null) plate = karuNameTmp.transform.parent as RectTransform;
            if (plate == null) return;

            // Shift the nameplate group right + slightly up, where user marked
            Vector2 wasPos = plate.anchoredPosition;
            plate.anchoredPosition += new Vector2(220f, 80f);
            Debug.Log($"[HomeLobbyPanel] Moved Karu nameplate '{plate.name}': " +
                     $"{wasPos} -> {plate.anchoredPosition} (hops up={hops})");
            #endif
        }

        // ─────────────────────────────────────────────────────────────────
        // BUTTON WIRING
        // ─────────────────────────────────────────────────────────────────

        private static void WireButtons(GameObject root)
        {
            // ONLY wire EXISTING Button components — never add new ones.
            // Dump ALL buttons so we can see exactly what Layer Lab calls them.
            int wired = 0;
            var allBtns = root.GetComponentsInChildren<Button>(true);
            Debug.Log($"[HomeLobbyPanel] === All {allBtns.Length} buttons in lobby ===");
            foreach (var b in allBtns)
            {
                if (b == null) continue;
                var t = b.GetComponentInChildren<TMP_Text>(true);
                string tt = t != null ? t.text : "<no text>";
                var rt = b.GetComponent<RectTransform>();
                Vector3[] cc = new Vector3[4]; rt.GetWorldCorners(cc);
                Vector3 ctr = (cc[0] + cc[2]) * 0.5f;
                Debug.Log($"  Btn '{b.gameObject.name}' text='{tt}' worldCenter=({ctr.x:F0},{ctr.y:F0}) size=({rt.rect.width:F0}x{rt.rect.height:F0}) interactable={b.interactable}");
            }

            foreach (var btn in allBtns)
            {
                if (btn == null) continue;
                string n = btn.gameObject.name;
                var tmp = btn.GetComponentInChildren<TMP_Text>(true);
                string txt = (tmp != null ? tmp.text : "") ?? "";
                bool playish =
                    n.Contains("Play") || n.Contains("PLAY") ||
                    n.Contains("Battle") || n.Contains("BATTLE") ||
                    n.Contains("Start") || n.Contains("START") ||
                    n.Contains("Main") ||
                    txt.Trim().ToUpper().Contains("PLAY") ||
                    txt.Trim().ToUpper().Contains("BATTLE") ||
                    txt.Trim().ToUpper().Contains("START");
                if (!playish) continue;
                btn.onClick.RemoveAllListeners();
                int captured = wired;
                btn.onClick.AddListener(() => {
                    Debug.Log($"[HomeLobbyPanel] ✓ PLAY/Start button {captured} '{btn.gameObject.name}' fired.");
                    LaunchStageMap();
                });
                wired++;
                Debug.Log($"[HomeLobbyPanel] Wired PLAY-ish button: '{n}' text='{txt}'");
            }
            Debug.Log($"[HomeLobbyPanel] Wired {wired} PLAY/BATTLE button(s).");
        }

        private static void LaunchStageMap()
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            Hide();
            Sparq.UI.StageMapPanel.Show();
        }

        // ─────────────────────────────────────────────────────────────────
        // COMPANIONS — Blip (pet) + Pecky (owl perch) next to the hero
        // ─────────────────────────────────────────────────────────────────

        private static void SpawnCompanions(GameObject lobbyRoot)
        {
            #if UNITY_EDITOR
            Debug.Log("[HomeLobbyPanel] === SpawnCompanions START ===");

            // Find Karu's RectTransform (the "Character" Image inside the lobby
            // prefab). Companions are parented as SIBLINGS of Karu — same parent,
            // same anchors, same Mask membership — so positioning is just
            // "Karu.anchoredPosition + offset", no coordinate-frame guessing.
            RectTransform karuRT = null;
            foreach (var img in lobbyRoot.GetComponentsInChildren<Image>(true))
            {
                if (img != null && img.gameObject.name == "Character")
                {
                    karuRT = img.rectTransform;
                    break;
                }
            }
            if (karuRT == null)
            {
                Debug.LogError("[HomeLobbyPanel] 'Character' RectTransform not found — cannot anchor companions.");
                return;
            }
            Debug.Log($"[HomeLobbyPanel] Karu RectTransform: parent={karuRT.parent?.name}, " +
                     $"anchoredPosition={karuRT.anchoredPosition}, sizeDelta={karuRT.sizeDelta}, " +
                     $"anchors=[{karuRT.anchorMin}..{karuRT.anchorMax}], pivot={karuRT.pivot}");

            // ── Blip (pet droplet) — three-tier sprite resolution ──
            Sprite blipSp = LoadActivePetSprite();
            if (blipSp == null)
                blipSp = Sparq.UI.HeroPortrait.LoadCropped(
                    "Assets/2D Fantasy Monster Sprite Pack/Monsters/Droplet/Sweet-Droplet.png");
            if (blipSp == null)
                blipSp = FindSpriteFromGameObject("Mochi");
            // Spawn Blip at the EXACT position the user annotated (right of
            // Karu, on the platform). Top-level canvas child = guaranteed
            // visible, no parent Mask, no sibling-order ambiguity.
            // Blip — nudged left another 60px per user (was 280, now 220)
            Vector2 blipPos = new Vector2(220f, -150f);
            // Render scale grows with pet evolution stage — Hatchling 0.85× →
            // Elder 1.25×, so a long-cared-for pet visibly fills out on the lobby.
            float petScale = 1f;
            try
            {
                int petLvl = Sparq.Core.SaveService.Data?.petLevel ?? 1;
                petScale = Sparq.Systems.PetService.EvolutionScale(Sparq.Systems.PetService.EvolutionStage(petLvl));
            }
            catch {}
            float blipSize = 330f * petScale;
            var blipGO = SpawnCompanionAtCanvasReturning(
                name: "Blip",
                canvasRoot: _root.transform,
                sprite: blipSp,
                canvasPos: blipPos,
                size: new Vector2(blipSize, blipSize));
            if (blipGO != null) blipGO.transform.SetAsLastSibling();
            // Pet nameplate — sits ABOVE Drip's head (Drip is 330 tall, centred
            // on blipPos, so his head-top is ~+165; the plate clears that).
            SpawnPetHealthBar(blipPos + new Vector2(0f, 235f));

            // (Pecky removed from lobby per user — Karu + Blip only on home.)
            #endif
        }

        // Parent a companion as a SIBLING of Karu's Character Image. The
        // companion shares Karu's parent, anchors, and pivot — so positioning
        // is just "Karu.anchoredPosition + offset", no math, no guessing.
        // This is the cleanest way to put a UI element next to another UI
        // element regardless of any Mask/Layout/RectTransform parents.
        private static void SpawnAsKaruSibling(string name, RectTransform karuRT,
                                               Sprite sprite, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(karuRT.parent, false);   // sibling of Karu
            var rt = go.GetComponent<RectTransform>();
            // Inherit Karu's anchors + pivot exactly so coordinate space matches
            rt.anchorMin = karuRT.anchorMin;
            rt.anchorMax = karuRT.anchorMax;
            rt.pivot = karuRT.pivot;
            rt.anchoredPosition = karuRT.anchoredPosition + offset;
            rt.sizeDelta = size;
            // Render BEHIND Karu (sibling index just before him) so Karu stays
            // the visual focal point.
            int karuIdx = karuRT.GetSiblingIndex();
            rt.SetSiblingIndex(Mathf.Max(0, karuIdx));

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Debug.Log($"[HomeLobbyPanel] Spawned '{name}' as Karu sibling: " +
                     $"anchoredPos={rt.anchoredPosition}, size={size}");
        }

        // Pet nameplate — tries to clone Karu's nameplate group with strict
        // size constraints (so it can't accidentally grab the whole lobby).
        // Falls back to a procedural version that matches Karu's style if
        // cloning fails.
        private static void SpawnPetHealthBar(Vector2 canvasPos)
        {
            // ── Clone Karu's ACTUAL nameplate pieces so Drip's is the exact
            // same style. The lobby inventory told us the real structure:
            //   • "Slider_Level_01_Purple" — the XP bar + the "3" level pill,
            //     a direct child of the lobby root (a real Unity Slider).
            //   • a separate name TMP_Text showing the player name ("Karu").
            // We clone both and lay them out the same way.
            Transform karuSlider = null;
            if (_root != null)
            {
                foreach (var rt in _root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt != null && rt.gameObject.name == "Slider_Level_01_Purple")
                    { karuSlider = rt.transform; break; }
                }
            }

            if (karuSlider != null)
            {
                string petName2 = "Drip"; int level2 = 1; int hunger2 = 100;
                try { var p = Sparq.Systems.PetService.Active();
                      if (p != null) { if (!string.IsNullOrEmpty(p.nickname)) petName2 = p.nickname;
                                       level2 = p.level; hunger2 = p.hunger; } } catch {}

                // Container — positioned above Drip's head.
                var plate = new GameObject("Sparq_PetNameplate", typeof(RectTransform));
                plate.transform.SetParent(_root.transform, false);
                var plRT = plate.GetComponent<RectTransform>();
                plRT.anchorMin = new Vector2(0.5f, 0.5f);
                plRT.anchorMax = new Vector2(0.5f, 0.5f);
                plRT.pivot = new Vector2(0.5f, 0.5f);
                plRT.anchoredPosition = canvasPos;
                plRT.sizeDelta = new Vector2(440f, 150f);
                plate.transform.SetAsLastSibling();

                // ── Clone the bar + level pill (pixel-identical to Karu's) ──
                var barClone = Object.Instantiate(karuSlider.gameObject, plate.transform);
                barClone.name = "Drip_LevelBar";
                var bcRT = barClone.GetComponent<RectTransform>();
                Vector2 barSize = bcRT.rect.size;          // capture before re-anchoring
                bcRT.anchorMin = new Vector2(0.5f, 0f);
                bcRT.anchorMax = new Vector2(0.5f, 0f);
                bcRT.pivot = new Vector2(0.5f, 0f);
                bcRT.sizeDelta = barSize;
                bcRT.anchoredPosition = new Vector2(0f, 0f);
                bcRT.localScale = Vector3.one;
                // Set Drip's level on the pill + hunger on the bar fill.
                foreach (var tm in barClone.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tm == null) continue;
                    string txt = (tm.text ?? "").Trim();
                    if (txt.Contains("/")) tm.text = $"{hunger2}/100";
                    else if (txt.Length >= 1 && txt.Length <= 3)
                    {
                        bool digits = txt.Length > 0;
                        foreach (var c in txt) if (c < '0' || c > '9') { digits = false; break; }
                        if (digits) { tm.text = level2.ToString(); tm.ForceMeshUpdate(true); }
                    }
                }
                var sl = barClone.GetComponentInChildren<Slider>(true);
                if (sl != null)
                    sl.value = Mathf.Clamp01(hunger2 / 100f) * (sl.maxValue <= 0f ? 1f : sl.maxValue);
                // Nameplate shouldn't eat clicks.
                foreach (var g in barClone.GetComponentsInChildren<Graphic>(true))
                    if (g != null) g.raycastTarget = false;

                // ── Clone Karu's name text (same font/style) → "Drip" ──
                string karuNameText = "Karu";
                try { var d3 = Sparq.Core.SaveService.Data;
                      if (d3 != null && !string.IsNullOrEmpty(d3.petName)) karuNameText = d3.petName; } catch {}
                TMP_Text karuNameTmp = null;
                foreach (var tm in _root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tm == null) continue;
                    if ((tm.text ?? "").Trim() == karuNameText) { karuNameTmp = tm; break; }
                }
                if (karuNameTmp != null)
                {
                    var nameClone = Object.Instantiate(karuNameTmp.gameObject, plate.transform);
                    nameClone.name = "Drip_Name";
                    var ncRT = nameClone.GetComponent<RectTransform>();
                    Vector2 nameSize = ncRT.rect.size;
                    ncRT.anchorMin = new Vector2(0.5f, 1f);
                    ncRT.anchorMax = new Vector2(0.5f, 1f);
                    ncRT.pivot = new Vector2(0.5f, 1f);
                    ncRT.sizeDelta = nameSize;
                    ncRT.anchoredPosition = new Vector2(0f, 0f);
                    ncRT.localScale = Vector3.one;
                    var ncTmp = nameClone.GetComponent<TMP_Text>();
                    if (ncTmp != null) { ncTmp.text = petName2; ncTmp.raycastTarget = false; ncTmp.ForceMeshUpdate(true); }
                }
                else
                {
                    // No name text found — clean white fallback in matching spot.
                    var nm = MakeText(plate.transform, "Drip_Name", petName2,
                        40, FontStyles.Bold, Color.white);
                    var nmRT = nm.rectTransform;
                    nmRT.anchorMin = new Vector2(0.5f, 1f);
                    nmRT.anchorMax = new Vector2(0.5f, 1f);
                    nmRT.pivot = new Vector2(0.5f, 1f);
                    nmRT.anchoredPosition = new Vector2(0f, 0f);
                    nmRT.sizeDelta = new Vector2(320f, 48f);
                    nm.alignment = TextAlignmentOptions.Center;
                    nm.raycastTarget = false;
                    try { nm.outlineWidth = 0.22f; nm.outlineColor = new Color(0.10f, 0.05f, 0.20f, 1f); } catch {}
                }

                Debug.Log($"[HomeLobbyPanel] Drip nameplate cloned from Karu's pieces " +
                          $"(Slider_Level_01_Purple): name={petName2}, lvl={level2}, hunger={hunger2}");
                return;
            }

            Debug.LogWarning("[HomeLobbyPanel] Slider_Level_01_Purple not found — using procedural fallback.");

            // ── Fallback: procedural nameplate ──
            string petName = "Drip";
            int level = 1;
            int hunger = 100;
            try
            {
                var p = Sparq.Systems.PetService.Active();
                if (p != null)
                {
                    if (!string.IsNullOrEmpty(p.nickname)) petName = p.nickname;
                    level = p.level;
                    hunger = p.hunger;
                }
            }
            catch (System.Exception ex) { Debug.LogWarning($"[HomeLobbyPanel] PetService access: {ex.Message}"); }

            // ── Card container — wider so the hunger bar has room to grow ──
            var card = new GameObject("Sparq_PetHpBar", typeof(RectTransform));
            card.transform.SetParent(_root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = canvasPos;
            crt.sizeDelta = new Vector2(360f, 110f);   // pet-width, sits above Drip (matches Karu's compact plate)
            crt.SetAsLastSibling();

            // ── Pet name — clean centred text, cohesive with Karu's plate.
            // (Dropped the Layer Lab sky-flag prefab: its bright blue clashed
            // with the purple lobby theme and was the main "cheesy" offender.)
            var nameTxt = MakeText(card.transform, "Name", petName,
                40, FontStyles.Bold, new Color(0.93f, 0.89f, 1f));
            var nrt = nameTxt.rectTransform;
            nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1);
            nrt.pivot = new Vector2(0.5f, 1);
            nrt.anchoredPosition = new Vector2(0, 0);
            nrt.sizeDelta = new Vector2(0, 48);
            nameTxt.alignment = TextAlignmentOptions.Center;
            try { nameTxt.outlineWidth = 0.25f; nameTxt.outlineColor = new Color(0.20f, 0.06f, 0.30f, 1f); } catch {}
            nameTxt.ForceMeshUpdate(true);

            // ── Level pill — dropped down + wider per user
            var pill = new GameObject("Lvl", typeof(RectTransform), typeof(Image));
            pill.transform.SetParent(card.transform, false);
            var prt = pill.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(0, 0);
            prt.pivot = new Vector2(0, 0.5f);
            prt.anchoredPosition = new Vector2(0, 10);   // was 30 — dropped 20px
            prt.sizeDelta = new Vector2(60, 60);          // proper circle (was 72x60 — a stretched oval)
            var pillImg = pill.GetComponent<Image>();
            pillImg.sprite = MakeCircleSprite();
            pillImg.color = new Color(0.42f, 0.22f, 0.68f, 1f);   // deep purple matching Karu
            // Level number
            var pillTxt = MakeText(pill.transform, "Txt", level.ToString(),
                30, FontStyles.Bold, Color.white);
            var ptRT = pillTxt.rectTransform;
            ptRT.anchorMin = Vector2.zero; ptRT.anchorMax = Vector2.one;
            ptRT.offsetMin = Vector2.zero; ptRT.offsetMax = Vector2.zero;
            pillTxt.alignment = TextAlignmentOptions.Center;
            try { pillTxt.outlineWidth = 0.20f; pillTxt.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f); } catch {}

            // ── Hunger bar — taller + wider (matches the bigger card) ──
            var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(card.transform, false);
            var bbRT = barBg.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0, 0); bbRT.anchorMax = new Vector2(1, 0);
            bbRT.pivot = new Vector2(0.5f, 0);
            bbRT.anchoredPosition = new Vector2(40, -10);   // dropped 28px to align with pill center
            bbRT.sizeDelta = new Vector2(-100, 40);
            barBg.GetComponent<Image>().color = new Color(0.18f, 0.10f, 0.30f, 0.95f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBg.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0, 1);
            frt.pivot = new Vector2(0, 0.5f);
            frt.anchoredPosition = new Vector2(2, 0);
            float pct = Mathf.Clamp01(hunger / 100f);
            // Card is 360 wide, bar BG is card - 100 = 260. Subtract 4 for inset.
            frt.sizeDelta = new Vector2((260f - 4f) * pct, -6f);
            fill.GetComponent<Image>().color = new Color(0.62f, 0.46f, 0.86f, 1f);   // cohesive purple (was garish magenta)

            var barTxt = MakeText(barBg.transform, "Pct", $"{hunger}/100",
                18, FontStyles.Bold, Color.white);
            var btRT = barTxt.rectTransform;
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
            barTxt.alignment = TextAlignmentOptions.Center;

            Debug.Log($"[HomeLobbyPanel] Pet HP bar spawned: name={petName}, lvl={level}, hunger={hunger}/100");
        }

        // Quick procedural circle sprite for the level pill background.
        private static Sprite _petCircleSp;
        private static Sprite MakeCircleSprite()
        {
            if (_petCircleSp != null) return _petCircleSp;
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
            float r = sz * 0.5f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - r, dy = y - r;
                    bool inside = dx*dx + dy*dy <= r*r;
                    tex.SetPixel(x, y, inside ? Color.white : new Color(0,0,0,0));
                }
            tex.Apply();
            _petCircleSp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return _petCircleSp;
        }

        // Compute a UI rect's center position in the canvas's local space
        // (regardless of nested Mask/Transform parents).
        private static Vector2 WorldToCanvasLocal(RectTransform canvasRT, RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            // center in world space
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            // Convert to canvas local
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, null, out local);
            return local;
        }

        // Spawn a UI Image at a specific local position inside the canvas root.
        private static void SpawnCompanionAtCanvas(string name, Transform canvasRoot, Sprite sprite,
                                                   Vector2 canvasPos, Vector2 size)
        {
            SpawnCompanionAtCanvasReturning(name, canvasRoot, sprite, canvasPos, size);
        }

        // Returning variant — same as above but returns the spawned GameObject
        // so callers can re-order sibling index, attach children, etc.
        private static GameObject SpawnCompanionAtCanvasReturning(
            string name, Transform canvasRoot, Sprite sprite, Vector2 canvasPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = canvasPos;
            rt.sizeDelta = size;
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
                Debug.Log($"[HomeLobbyPanel] Spawned '{name}' WITH SPRITE at canvas {canvasPos}, size {size}");
            }
            else
            {
                img.color = new Color(1f, 0.2f, 0.6f, 1f);
                Debug.LogWarning($"[HomeLobbyPanel] '{name}' sprite was NULL — showing magenta debug box at {canvasPos}");
            }
            img.raycastTarget = false;
            return go;
        }

        // Try to extract the sprite from a SpriteRenderer on a named GameObject in the scene.
        private static Sprite FindSpriteFromGameObject(string objectName)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null || go.name != objectName) continue;
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return null;
        }

        // Load the active pet's species sprite via PetService.
        private static Sprite LoadActivePetSprite()
        {
            #if UNITY_EDITOR
            try
            {
                var active = Sparq.Systems.PetService.Active();
                if (active == null) return null;
                var sp = Sparq.Systems.PetService.FindSpecies(active.speciesId);
                if (sp == null || string.IsNullOrEmpty(sp.spritePath)) return null;
                // Alpha-crop so the pet fills its frame cleanly — same polish
                // pass the hero portrait gets (see HeroPortrait).
                return Sparq.UI.HeroPortrait.LoadCropped(sp.spritePath);
            }
            catch { return null; }
            #else
            return null;
            #endif
        }

        // ─────────────────────────────────────────────────────────────────
        // TODAY'S TRIAL CARD — daily quest with BEGIN button
        // ─────────────────────────────────────────────────────────────────

        // Today's-trial floating card — spawned on the lobby canvas above the
        // PLAY button. Reads from DailyTrialCard.Deck (same source the legacy
        // home used) so the rotating quest is identical between old and new.
        private static void SpawnTodaysTrialCard()
        {
            // Pick today's trial from the static deck (same logic as legacy card)
            var deck = Sparq.UI.DailyTrialCard.Deck;
            if (deck == null || deck.Length == 0) return;
            var trial = deck[System.DateTime.UtcNow.DayOfYear % deck.Length];

            // ── Card root — moved UP into the player banner's old spot
            //    (where "Karu 99,999" trophy used to be). That area is now
            //    free since the banner is hidden by declutter.
            HideLobbyChatBanner();

            var card = new GameObject("Sparq_TrialCard",
                typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 320f);   // original spot — upper area, between rails and platform
            crt.sizeDelta = new Vector2(820f, 180f);
            crt.localScale = new Vector3(0.85f, 0.85f, 1f);
            var cimg = card.GetComponent<Image>();
            cimg.color = new Color(1f, 0.96f, 0.84f, 1f);  // cream card
            crt.SetAsLastSibling();

            // ── Top label "TODAY'S TRIAL" ──
            var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(card.transform, false);
            var trt = topBar.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, 0);
            trt.sizeDelta = new Vector2(0, 44);
            topBar.GetComponent<Image>().color = new Color(1f, 0.82f, 0.30f, 1f);
            var topLbl = MakeText(topBar.transform, "Lbl", "TODAY'S TRIAL",
                26, FontStyles.Bold, new Color(0.10f, 0.05f, 0.20f));
            var trtLbl = topLbl.rectTransform;
            trtLbl.anchorMin = Vector2.zero; trtLbl.anchorMax = Vector2.one;
            trtLbl.offsetMin = Vector2.zero; trtLbl.offsetMax = Vector2.zero;

            // ── Glyph badge (kind icon) ──
            var glyph = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyph.transform.SetParent(card.transform, false);
            var grt = glyph.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(60, -10);
            grt.sizeDelta = new Vector2(86, 86);
            glyph.GetComponent<Image>().color = TintForKind(trial.kind);
            var glyphTxt = MakeText(glyph.transform, "T", trial.glyph,
                52, FontStyles.Bold, Color.white);
            var grtTxt = glyphTxt.rectTransform;
            grtTxt.anchorMin = Vector2.zero; grtTxt.anchorMax = Vector2.one;
            grtTxt.offsetMin = Vector2.zero; grtTxt.offsetMax = Vector2.zero;
            glyphTxt.outlineWidth = 0.22f;

            // ── Title + subtitle + reward ──
            var titleTm = MakeText(card.transform, "Title", trial.title,
                32, FontStyles.Bold, new Color(0.55f, 0.10f, 0.15f));
            var titleRT = titleTm.rectTransform;
            titleRT.anchorMin = new Vector2(0, 0.5f); titleRT.anchorMax = new Vector2(0, 0.5f);
            titleRT.pivot = new Vector2(0, 0.5f);
            titleRT.anchoredPosition = new Vector2(120, 18);
            titleRT.sizeDelta = new Vector2(420, 44);
            titleTm.alignment = TextAlignmentOptions.Left;

            var subTm = MakeText(card.transform, "Sub", trial.subtitle,
                22, FontStyles.Normal, new Color(0.30f, 0.20f, 0.20f));
            var subRT = subTm.rectTransform;
            subRT.anchorMin = new Vector2(0, 0.5f); subRT.anchorMax = new Vector2(0, 0.5f);
            subRT.pivot = new Vector2(0, 0.5f);
            subRT.anchoredPosition = new Vector2(120, -16);
            subRT.sizeDelta = new Vector2(420, 30);
            subTm.alignment = TextAlignmentOptions.Left;

            var rewardTm = MakeText(card.transform, "Reward", $"+{trial.xpReward} XP",
                22, FontStyles.Bold, new Color(0.85f, 0.50f, 0.10f));
            var rwRT = rewardTm.rectTransform;
            rwRT.anchorMin = new Vector2(0, 0); rwRT.anchorMax = new Vector2(0, 0);
            rwRT.pivot = new Vector2(0, 0);
            rwRT.anchoredPosition = new Vector2(120, 12);
            rwRT.sizeDelta = new Vector2(200, 28);
            rewardTm.alignment = TextAlignmentOptions.Left;

            // ── BEGIN button ──
            var btnGO = new GameObject("BeginBtn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(card.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-22, -10);
            brt.sizeDelta = new Vector2(160, 70);
            btnGO.GetComponent<Image>().color = new Color(1f, 0.78f, 0.22f, 1f);
            var bLbl = MakeText(btnGO.transform, "Lbl", "BEGIN",
                30, FontStyles.Bold, new Color(0.10f, 0.05f, 0.20f));
            var bLblRT = bLbl.rectTransform;
            bLblRT.anchorMin = Vector2.zero; bLblRT.anchorMax = Vector2.one;
            bLblRT.offsetMin = Vector2.zero; bLblRT.offsetMax = Vector2.zero;
            btnGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log($"[HomeLobbyPanel] ✓ Trial '{trial.title}' BEGIN tapped — opening QuestsPanel.");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                Hide();
                // Daily trials are ADHD-friendly real-life tasks dressed up as
                // RPG quests ("Walk the path", "Read the tomes", "Loosen the
                // armor"). The right destination is the QUESTS panel where
                // tasks are managed, NOT a combat map.
                try { Sparq.UI.QuestsPanel.Show(); }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[HomeLobbyPanel] QuestsPanel.Show failed: {ex.Message}");
                }
            });

            Debug.Log($"[HomeLobbyPanel] Trial card spawned: '{trial.title}' (kind={trial.kind}, xp={trial.xpReward}).");
        }

        // Layer Lab's Lobby includes a "Chat" placeholder banner ("Layerlab
        // looking friends add me!"). We hide it so our trial card has clean
        // real-estate above the bottom nav.
        private static void HideLobbyChatBanner()
        {
            if (_root == null) return;
            foreach (var go in _root.GetComponentsInChildren<RectTransform>(true))
            {
                if (go == null) continue;
                string n = go.gameObject.name;
                if (n == "Chat" || n.Contains("ChatBanner") || n.Contains("Chat_Layer"))
                {
                    go.gameObject.SetActive(false);
                    Debug.Log($"[HomeLobbyPanel] Hid Layer Lab chat banner: '{n}'");
                }
            }
        }

        private static Color TintForKind(Sparq.UI.DailyTrialCard.TrialKind kind)
        {
            switch (kind)
            {
                case Sparq.UI.DailyTrialCard.TrialKind.Combat: return new Color(0.85f, 0.30f, 0.30f);
                case Sparq.UI.DailyTrialCard.TrialKind.Body:   return new Color(0.30f, 0.70f, 0.45f);
                case Sparq.UI.DailyTrialCard.TrialKind.Mind:   return new Color(0.55f, 0.40f, 0.85f);
            }
            return Color.gray;
        }

        // ─────────────────────────────────────────────────────────────────
        // OWN UI BITS (back button, error message)
        // ─────────────────────────────────────────────────────────────────

        private static void BuildBackButton()
        {
            // Top-left back button so the user can return to the existing home
            // during the transition period (until the new lobby fully replaces it).
            var go = new GameObject("Sparq_BackBtn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20);
            rt.sizeDelta = new Vector2(160, 70);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.07f, 0.20f, 0.92f);

            var lbl = MakeText(go.transform, "Lbl", "← Back",
                34, FontStyles.Bold, new Color(1f, 0.92f, 0.55f));
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.outlineWidth = 0.22f;
            lbl.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f);

            go.GetComponent<Button>().onClick.AddListener(Hide);
        }

        private static void BuildErrorMessage(string msg)
        {
            var lbl = MakeText(_root.transform, "Err", msg, 32, FontStyles.Bold, Color.white);
            var rt = lbl.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.4f);
            rt.anchorMax = new Vector2(0.9f, 0.6f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────

        // ═══════════════════════════════════════════════════════════════════
        // VITALITY METER — wellness → power, surfaced on the lobby
        // ═══════════════════════════════════════════════════════════════════
        private static GameObject _vitBanner;
        private static RectTransform _vitFill;
        private static TMP_Text _vitPercentLabel, _vitBuffLabel;
        private static GameObject _vitPopup;

        // A tappable banner near the top of the lobby: today's self-care → squad
        // power. It's the first thing you see each morning, pulling you toward the
        // healthy actions. Live-refreshed so it ticks up as you act.
        private static void BuildVitalityBanner(GameObject root)
        {
            if (_root == null) return;
            // Rebuild cleanly if a previous one lingers.
            var existing = _root.transform.Find("Sparq_VitalityBanner");
            if (existing != null) Object.Destroy(existing.gameObject);

            // Near-opaque dark plate for strong text contrast over the busy sky.
            var go = MakeImg(_root.transform, "Sparq_VitalityBanner", new Color(0.05f, 0.07f, 0.11f, 0.98f));
            _vitBanner = go;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            // Sits in the clear "sky" band: below the DAILY QUESTS tile + reminders
            // button, above the hero, and narrow enough to clear the side hexes.
            rt.anchoredPosition = new Vector2(0, -540);
            rt.sizeDelta = new Vector2(640, 110);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.onClick.AddListener(ShowVitalityBreakdown);

            // Gold top accent line.
            var accent = MakeImg(go.transform, "Accent", new Color(0.96f, 0.66f, 0.10f, 0.95f));
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1); art.sizeDelta = new Vector2(0, 5); art.anchoredPosition = Vector2.zero;
            accent.GetComponent<Image>().raycastTarget = false;

            // Title (left).
            var title = MakeText(go.transform, "Title", "VITALITY", 32, FontStyles.Bold, new Color(1f, 0.92f, 0.65f));
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, 1); trt.anchoredPosition = new Vector2(24, -10); trt.sizeDelta = new Vector2(280, 40);
            title.alignment = TextAlignmentOptions.Left;
            try { title.outlineWidth = 0.25f; title.outlineColor = Color.black; } catch {}

            // Buff (right).
            _vitBuffLabel = MakeText(go.transform, "Buff", "", 28, FontStyles.Bold, new Color(0.55f, 1f, 0.60f));
            var brt = _vitBuffLabel.rectTransform;
            brt.anchorMin = new Vector2(1, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(1, 1); brt.anchoredPosition = new Vector2(-24, -10); brt.sizeDelta = new Vector2(380, 40);
            _vitBuffLabel.alignment = TextAlignmentOptions.Right;
            try { _vitBuffLabel.outlineWidth = 0.25f; _vitBuffLabel.outlineColor = Color.black; } catch {}

            // Progress bar background (pinned along the bottom) — taller for clarity.
            var barBg = MakeImg(go.transform, "BarBg", new Color(0f, 0f, 0f, 0.55f));
            var bgrt = barBg.GetComponent<RectTransform>();
            bgrt.anchorMin = new Vector2(0, 0); bgrt.anchorMax = new Vector2(1, 0);
            bgrt.offsetMin = new Vector2(24, 16); bgrt.offsetMax = new Vector2(-24, 60);
            barBg.GetComponent<Image>().raycastTarget = false;

            // Fill — width driven by anchorMax.x = vitality fraction.
            var fill = MakeImg(barBg.transform, "Fill", new Color(0.36f, 0.86f, 0.45f, 1f));
            _vitFill = fill.GetComponent<RectTransform>();
            _vitFill.anchorMin = new Vector2(0, 0); _vitFill.anchorMax = new Vector2(0, 1);
            _vitFill.pivot = new Vector2(0, 0.5f);
            _vitFill.offsetMin = Vector2.zero; _vitFill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().raycastTarget = false;

            // Percent text centred on the bar.
            _vitPercentLabel = MakeText(barBg.transform, "Pct", "", 26, FontStyles.Bold, Color.white);
            var prt = _vitPercentLabel.rectTransform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            _vitPercentLabel.alignment = TextAlignmentOptions.Center;
            try { _vitPercentLabel.outlineWidth = 0.3f; _vitPercentLabel.outlineColor = Color.black; } catch {}

            // Keep it live as the user completes actions and returns to the lobby.
            go.AddComponent<VitalityRefresher>();
            UpdateVitalityBanner();
        }

        internal static void UpdateVitalityBanner()
        {
            if (_vitBanner == null || _vitFill == null) return;
            // Compute Today() ONCE per refresh and derive everything from it —
            // BuffPercent/VitalityPercent each call Today() internally, so the
            // old "call all three" pattern hit Today() 3× per tick (= 6× per
            // second from the 0.5s refresher).
            float frac = 0f; int buff = 0, pct = 0;
            try
            {
                float today = Sparq.Systems.VitalityService.Today();
                frac = Mathf.Clamp01(today);
                buff = Mathf.RoundToInt(today * Sparq.Systems.VitalityService.MAX_BUFF * 100f);
                pct  = Mathf.RoundToInt(today * 100f);
            }
            catch {}
            _vitFill.anchorMax = new Vector2(frac, 1f);
            // Colour shifts from amber (low) toward green (high).
            var img = _vitFill.GetComponent<Image>();
            if (img != null)
                img.color = Color.Lerp(new Color(0.95f, 0.62f, 0.20f), new Color(0.36f, 0.86f, 0.45f), frac);
            if (_vitPercentLabel != null) _vitPercentLabel.text = $"{pct}%";
            if (_vitBuffLabel != null)
                _vitBuffLabel.text = buff > 0 ? $"+{buff}% squad power" : "tap: power up";
        }

        // Tap → breakdown of today's self-care, with a Spoon Check CTA.
        private static void ShowVitalityBreakdown()
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            if (_vitPopup != null) { Object.Destroy(_vitPopup); _vitPopup = null; }
            if (_root == null) return;

            // Dim — tap to close.
            var dim = MakeImg(_root.transform, "Sparq_VitalityPopup", new Color(0, 0, 0, 0.72f));
            _vitPopup = dim;
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.targetGraphic = dim.GetComponent<Image>();
            dimBtn.onClick.AddListener(CloseVitalityBreakdown);

            // Card.
            var card = MakeImg(dim.transform, "Card", new Color(0.12f, 0.15f, 0.20f, 1f));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(820, 760);

            int buff = 0, pct = 0; string breakdown = "";
            try
            {
                buff = Sparq.Systems.VitalityService.BuffPercent();
                pct  = Sparq.Systems.VitalityService.VitalityPercent();
                breakdown = Sparq.Systems.VitalityService.Breakdown();
            }
            catch {}

            var title = MakeText(card.transform, "Title", "TODAY'S VITALITY", 44, FontStyles.Bold, new Color(1f, 0.92f, 0.65f));
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 1); trt.anchorMax = new Vector2(0.5f, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(0, -34); trt.sizeDelta = new Vector2(760, 56);

            var big = MakeText(card.transform, "Big", $"<color=#7CFC8A>{pct}%</color>  =  +{buff}% squad power", 38, FontStyles.Bold, Color.white);
            var grt = big.rectTransform;
            grt.anchorMin = new Vector2(0.5f, 1); grt.anchorMax = new Vector2(0.5f, 1);
            grt.pivot = new Vector2(0.5f, 1); grt.anchoredPosition = new Vector2(0, -110); grt.sizeDelta = new Vector2(760, 56);
            big.richText = true;

            var list = MakeText(card.transform, "List", breakdown, 36, FontStyles.Bold, new Color(0.92f, 0.94f, 0.98f));
            var lrt = list.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 1); lrt.anchorMax = new Vector2(0.5f, 1);
            lrt.pivot = new Vector2(0.5f, 1); lrt.anchoredPosition = new Vector2(0, -200); lrt.sizeDelta = new Vector2(680, 320);
            list.alignment = TextAlignmentOptions.TopLeft;
            list.lineSpacing = 24f;

            // Spoon Check CTA.
            var cta = MakeImg(card.transform, "Cta", new Color(0.11f, 0.55f, 0.53f, 1f));
            var ctaRT = cta.GetComponent<RectTransform>();
            ctaRT.anchorMin = new Vector2(0.5f, 0); ctaRT.anchorMax = new Vector2(0.5f, 0);
            ctaRT.pivot = new Vector2(0.5f, 0); ctaRT.anchoredPosition = new Vector2(0, 120); ctaRT.sizeDelta = new Vector2(520, 92);
            var ctaBtn = cta.AddComponent<Button>();
            ctaBtn.targetGraphic = cta.GetComponent<Image>();
            ctaBtn.onClick.AddListener(() =>
            {
                CloseVitalityBreakdown();
                try { Sparq.UI.SpoonCheckPanel.Show(); } catch (System.Exception ex) { Debug.LogError(ex.Message); }
            });
            var ctaLbl = MakeText(cta.transform, "L", "Do a Spoon Check", 34, FontStyles.Bold, Color.white);
            var clrt = ctaLbl.rectTransform; clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
            clrt.offsetMin = Vector2.zero; clrt.offsetMax = Vector2.zero; ctaLbl.alignment = TextAlignmentOptions.Center;

            // Close.
            var close = MakeImg(card.transform, "Close", new Color(0.30f, 0.33f, 0.40f, 1f));
            var clRT = close.GetComponent<RectTransform>();
            clRT.anchorMin = new Vector2(0.5f, 0); clRT.anchorMax = new Vector2(0.5f, 0);
            clRT.pivot = new Vector2(0.5f, 0); clRT.anchoredPosition = new Vector2(0, 24); clRT.sizeDelta = new Vector2(520, 76);
            var closeBtn = close.AddComponent<Button>();
            closeBtn.targetGraphic = close.GetComponent<Image>();
            closeBtn.onClick.AddListener(CloseVitalityBreakdown);
            var closeLbl = MakeText(close.transform, "L", "Close", 32, FontStyles.Bold, Color.white);
            var xlrt = closeLbl.rectTransform; xlrt.anchorMin = Vector2.zero; xlrt.anchorMax = Vector2.one;
            xlrt.offsetMin = Vector2.zero; xlrt.offsetMax = Vector2.zero; closeLbl.alignment = TextAlignmentOptions.Center;
        }

        private static void CloseVitalityBreakdown()
        {
            if (_vitPopup != null) { Object.Destroy(_vitPopup); _vitPopup = null; }
            UpdateVitalityBanner();
        }

        // Ticks the lobby Vitality banner every half-second so it reflects
        // actions the player just completed without a full lobby rebuild.
        private class VitalityRefresher : MonoBehaviour
        {
            private float _t;
            private void Update()
            {
                _t += Time.unscaledDeltaTime;
                if (_t < 0.5f) return;
                _t = 0f;
                HomeLobbyPanel.UpdateVitalityBanner();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // STREAK-SAVED TOAST — surfaces the Grace Day shield safety net
        // ═══════════════════════════════════════════════════════════════════
        // Transient top banner on its own high-sort canvas (above the morning
        // popups) that fades out on its own. Non-blocking so taps pass through.
        private static void ShowStreakSavedToast()
        {
            var go = new GameObject("Sparq_StreakToast",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            var rrt = go.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canv = go.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16500;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != go && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 10;
            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;
            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false;   // taps pass through

            // Banner near the very top (clears the centred morning popups).
            var banner = MakeImg(go.transform, "Banner", new Color(0.08f, 0.16f, 0.10f, 0.98f));
            var brt = banner.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1); brt.anchorMax = new Vector2(0.5f, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.anchoredPosition = new Vector2(0, -46);
            brt.sizeDelta = new Vector2(840, 132);
            banner.GetComponent<Image>().raycastTarget = false;

            var accent = MakeImg(banner.transform, "Accent", new Color(0.45f, 0.95f, 0.55f, 0.95f));
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1); art.sizeDelta = new Vector2(0, 6); art.anchoredPosition = Vector2.zero;
            accent.GetComponent<Image>().raycastTarget = false;

            var t1 = MakeText(banner.transform, "T1", "STREAK SAVED!", 38, FontStyles.Bold, new Color(0.55f, 1f, 0.62f));
            var t1RT = t1.rectTransform;
            t1RT.anchorMin = new Vector2(0, 1); t1RT.anchorMax = new Vector2(1, 1);
            t1RT.pivot = new Vector2(0.5f, 1); t1RT.anchoredPosition = new Vector2(0, -16); t1RT.sizeDelta = new Vector2(800, 48);
            t1.alignment = TextAlignmentOptions.Center;
            try { t1.outlineWidth = 0.25f; t1.outlineColor = Color.black; } catch {}

            var t2 = MakeText(banner.transform, "T2", "A Grace Day shield absorbed your missed day.", 26, FontStyles.Normal, new Color(0.88f, 0.94f, 0.88f));
            var t2RT = t2.rectTransform;
            t2RT.anchorMin = new Vector2(0, 1); t2RT.anchorMax = new Vector2(1, 1);
            t2RT.pivot = new Vector2(0.5f, 1); t2RT.anchoredPosition = new Vector2(0, -72); t2RT.sizeDelta = new Vector2(800, 40);
            t2.alignment = TextAlignmentOptions.Center;

            go.AddComponent<LobbyToast>();
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete); } catch {}
        }

        // Holds a toast for a few seconds, fades it, then self-destroys.
        private class LobbyToast : MonoBehaviour
        {
            private float _t;
            private const float LIFE = 3.6f, FADE = 0.7f;
            private CanvasGroup _cg;
            private void Awake() { _cg = GetComponent<CanvasGroup>(); }
            private void Update()
            {
                _t += Time.unscaledDeltaTime;
                if (_cg != null && _t > LIFE - FADE)
                    _cg.alpha = Mathf.Clamp01((LIFE - _t) / FADE);
                if (_t >= LIFE) Destroy(gameObject);
            }
        }

        private static GameObject MakeImg(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null && existing.isActiveAndEnabled) return;
            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<EventSystem>();
            if (es != null) es.enabled = true;
        }
    }
}
