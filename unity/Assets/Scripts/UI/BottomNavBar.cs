using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// 5-tab bottom navigation bar matching the Sparq WebView:
    ///   Home | Journal | Remind | Feed | Profile
    /// Currently only Home is functional; others show "Coming Soon" toast.
    /// </summary>
    public class BottomNavBar : MonoBehaviour
    {
        public enum Tab { Home, Quests, Journal, Remind, Feed, Profile }

        [System.Serializable]
        public struct TabConfig
        {
            public Tab tab;
            public string label;
            public string icon; // unicode emoji as placeholder
            public Color tint;
        }

        private static readonly TabConfig[] TABS = new[]
        {
            new TabConfig { tab=Tab.Home,    label="Home",    icon="🏠", tint = new Color(1f, 0.6f, 0.3f) },
            new TabConfig { tab=Tab.Quests,  label="Quests",  icon="📜", tint = new Color(1f, 0.82f, 0.32f) },
            new TabConfig { tab=Tab.Journal, label="Journal", icon="📓", tint = new Color(0.65f, 0.55f, 0.85f) },
            new TabConfig { tab=Tab.Remind,  label="Remind",  icon="🔔", tint = new Color(1f, 0.85f, 0.4f) },
            new TabConfig { tab=Tab.Feed,    label="Feed",    icon="🌊", tint = new Color(0.4f, 0.85f, 1f) },
            new TabConfig { tab=Tab.Profile, label="Profile", icon="👤", tint = new Color(1f, 0.5f, 0.55f) },
        };

        public Tab activeTab = Tab.Home;
        private readonly Dictionary<Tab, GameObject> _tabIndicators = new();
        private readonly Dictionary<Tab, TMP_Text>   _tabLabels     = new();

        private void Start()
        {
            // CRITICAL: Without an EventSystem in the scene, NO button.onClick
            // fires anywhere — the symptom is "every button looks fine and does
            // nothing." Unity 6 doesn't auto-add one; ensure now before we wire
            // any tab listeners.
            EnsureEventSystem();

            // Editor-time onClick listeners often don't persist across scene
            // save/reload. Re-wire every Play so the buttons always respond.
            // We search both the BottomNav's children AND the entire scene as
            // a fallback in case the buttons live somewhere else.
            int wired = 0;
            wired += WireTab("Home",    Tab.Home);
            wired += WireTab("Quests",  Tab.Quests);
            wired += WireTab("Journal", Tab.Journal);
            wired += WireTab("Remind",  Tab.Remind);
            wired += WireTab("Feed",    Tab.Feed);
            wired += WireTab("Profile", Tab.Profile);
            Debug.Log($"[BottomNavBar] Wired {wired}/6 tab buttons.");

            // 'BottomNav' is a LEGACY 6-tab bar sitting invisibly behind the
            // visible nav — it kept catching fall-through clicks. Neutralise it
            // so it stops interfering. The bar the user actually sees + taps is
            // 'HomeNavButtons' (handled below).
            NeutralizeBottomNav();

            // Wire the top MAP/SHOP/BAG/PETS/WORLD buttons by targeting the
            // HomeNavButtons container directly — avoids matching popup buttons.
            var topNavRoot = GameObject.Find("HomeNavButtons");
            if (topNavRoot == null)
            {
                Debug.LogWarning("[TopNav] HomeNavButtons GameObject not found in scene.");
            }
            else
            {
                int topWired = 0;
                topWired += WireTopBtnIn(topNavRoot.transform, "MapBtn",
                    () => { Debug.Log("[TopNav] MAP clicked"); Sparq.UI.StageMapPanel.Show(); });
                topWired += WireTopBtnIn(topNavRoot.transform, "ShopBtn",
                    () => { Debug.Log("[TopNav] SHOP clicked"); try { Sparq.UI.PopupManager.Instance?.OpenShop(); } catch (System.Exception ex) { Debug.LogError($"SHOP: {ex.Message}"); } });
                // BagBtn (the top-right "bag" hex on HomeNavButtons) → BagPanel
                // (consumables / inventory). Weapons live on the bottom-nav
                // sword hex, wired separately in HomeLobbyPanel.WireWeaponsTab.
                topWired += WireTopBtnIn(topNavRoot.transform, "BagBtn",
                    () => { Debug.Log("[TopNav] BAG clicked"); Sparq.UI.BagPanel.Show(); });
                topWired += WireTopBtnIn(topNavRoot.transform, "PetsBtn",
                    () => { Debug.Log("[TopNav] PETS clicked"); Sparq.UI.PetPanel.Show(); });
                topWired += WireTopBtnIn(topNavRoot.transform, "WorldBtn", () =>
                {
                    Debug.Log("[TopNav] WORLD clicked");
                    // Always use the procedural WorldPanel — disable the legacy
                    // static SocialPanel prefab if present so it doesn't fight us.
                    foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (go != null && go.name == "SocialPanel") go.SetActive(false);
                    }
                    Sparq.UI.WorldPanel.Show();
                });
                Debug.Log($"[TopNav] Wired {topWired}/5 top buttons under HomeNavButtons.");
                // (Journal wiring moved to HomeLobbyPanel.WireJournalTab — the
                //  visible bottom bar is the lobby's TabMenu_BottomFlush_01,
                //  NOT HomeNavButtons, which is the TOP nav.)
            Sparq.UI.GlobalClickSniffer.Ensure();
            Sparq.UI.HomeNpcSpawner.Ensure();
            Sparq.UI.HomeChibiUpgrade.Ensure();   // swap Karu + Mochi to BattleOfHeroes / Azure chibi art
            Sparq.UI.HomeBgm.Ensure();            // catchy looping home soundtrack
            }

            // Auto-unblock VFX overlays that may be intercepting clicks.
            // (VignetteCanvas / RaysCanvas / SkyCanvas are visual-only.)
            UnblockVfxOverlay("VignetteCanvas");
            UnblockVfxOverlay("RaysCanvas");
            UnblockVfxOverlay("SkyCanvas");

            // Sweep the scene for invisible full-screen click blockers.
            int sweptCount = SweepInvisibleBlockers();
            if (sweptCount > 0) Debug.Log($"[TopNav] Disabled raycast on {sweptCount} invisible full-screen Image(s).");

            // Promote HomeNavButtons to its own canvas at HIGH sort
            if (topNavRoot != null)
            {
                var c = topNavRoot.GetComponent<UnityEngine.Canvas>();
                if (c == null) c = topNavRoot.AddComponent<UnityEngine.Canvas>();
                c.overrideSorting = true;
                c.sortingOrder = 5000;            // Way above anything else
                if (topNavRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    topNavRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("[TopNav] HomeNavButtons promoted to its own canvas at sort 5000");
            }

            // Aggressive: walk every Image with raycastTarget=true and check if
            // its world-space rect overlaps the top buttons. If so AND its
            // canvas sort is >= 5000, log it so we can see the culprit.
            DiagnoseTopBlockers(topNavRoot);

            // Add a click logger to every direct child of the top nav so we can
            // see in the Console which GameObject *did* get the click.
            if (topNavRoot != null)
            {
                foreach (var b in topNavRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    var et = b.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                    if (et == null) et = b.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var entry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                    string name = b.gameObject.name;
                    entry.callback.AddListener((_) => Debug.Log($"[TopNav] PointerDown on '{name}'"));
                    et.triggers.Add(entry);
                }
            }

            // Diagnostic — dump every interactive element in the bottom strip
            // of the screen so the real nav structure is unambiguous.
            #if UNITY_EDITOR
            DumpBottomRegion();
            #endif
        }

#if UNITY_EDITOR
        // One-shot diagnostic: every UI Graphic in the bottom ~22% of the
        // screen — name, path, screen pos, size, raycastTarget, owning Button,
        // canvas — written to a file so the bottom-nav structure is exact.
        private void DumpBottomRegion()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# Bottom-region dump — {System.DateTime.Now}  (screen {Screen.width}x{Screen.height})");
                float cut = Screen.height * 0.22f;
                var graphics = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                int n = 0;
                foreach (var g in graphics)
                {
                    if (g == null) continue;
                    var rt = g.rectTransform;
                    if (rt == null) continue;
                    Vector3[] cc = new Vector3[4]; rt.GetWorldCorners(cc);
                    Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, (cc[0] + cc[2]) * 0.5f);
                    if (sp.y > cut) continue;   // not in the bottom strip
                    var btn = g.GetComponentInParent<UnityEngine.UI.Button>();
                    var canv = g.canvas;
                    sb.AppendLine(
                        $"'{g.gameObject.name}'  type={g.GetType().Name}  active={g.gameObject.activeInHierarchy}  " +
                        $"screen=({sp.x:F0},{sp.y:F0})  size=({rt.rect.width:F0}x{rt.rect.height:F0})  " +
                        $"raycast={g.raycastTarget}  ownerBtn={(btn != null ? btn.gameObject.name : "-")}  " +
                        $"canvas={(canv != null ? canv.name + ":sort" + canv.sortingOrder : "-")}");
                    sb.AppendLine($"    path: {FullPath(g.transform)}");
                    n++;
                }
                System.IO.File.WriteAllText(
                    Application.dataPath + "/_sparq_bottomregion_dump.txt", sb.ToString());
                Debug.Log($"[BottomNavBar] Bottom-region dump ({n} graphics) → Assets/_sparq_bottomregion_dump.txt");
            }
            catch (System.Exception ex)
            { Debug.LogError($"[BottomNavBar] DumpBottomRegion failed: {ex.Message}"); }
        }

        private static string FullPath(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts);
        }
#endif

        // Find every UI.Image that fills the whole screen and is nearly invisible
        // (alpha < 0.05) but has raycastTarget on. Turn off raycastTarget so clicks
        // pass through. Anything actually visible stays untouched.
        private int SweepInvisibleBlockers()
        {
            int swept = 0;
            foreach (var img in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!img.raycastTarget) continue;
                if (img.color.a >= 0.05f) continue;
                var rt = img.rectTransform;
                if (rt == null) continue;
                // Heuristic: stretches across the full canvas (anchors at corners)
                bool stretchedX = Mathf.Approximately(rt.anchorMin.x, 0) && Mathf.Approximately(rt.anchorMax.x, 1);
                bool stretchedY = Mathf.Approximately(rt.anchorMin.y, 0) && Mathf.Approximately(rt.anchorMax.y, 1);
                if (!(stretchedX && stretchedY)) continue;
                // Skip dims that are part of a panel we ourselves manage
                string n = img.gameObject.name.ToLower();
                if (n.Contains("dim")) { /* dims are usually intentional */ }
                img.raycastTarget = false;
                swept++;
                Debug.Log($"[TopNav] Disabled raycast on transparent overlay '{img.gameObject.name}' (parent: {img.transform.parent?.name})");
            }
            return swept;
        }

        // Logs and unblocks anything that could be intercepting clicks on the
        // top buttons region. Walks every Canvas and reports their sort, then
        // walks every raycast-targeting Image overlapping the top-button rect.
        private static void DiagnoseTopBlockers(GameObject topNavRoot)
        {
            if (topNavRoot == null) return;
            var topRT = topNavRoot.GetComponent<RectTransform>();
            if (topRT == null) return;
            // Cache top buttons' screen-space rect
            Vector3[] corners = new Vector3[4];
            topRT.GetWorldCorners(corners);
            var topRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);

            // List ALL canvases sorted by sortOrder descending
            var canvases = UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            System.Array.Sort(canvases, (a, b) => b.sortingOrder.CompareTo(a.sortingOrder));
            Debug.Log("[TopNav] === ALL CANVASES (sort desc) ===");
            foreach (var c in canvases)
            {
                Debug.Log($"  '{c.name}' sort={c.sortingOrder} override={c.overrideSorting} raycast={c.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null}");
            }

            // Now find every Image that overlaps the top button rect and has
            // raycastTarget=true. Auto-disable any whose canvas sort >= 5000
            // and isn't part of HomeNavButtons.
            int killed = 0;
            foreach (var img in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!img.raycastTarget) continue;
                if (img.transform.IsChildOf(topNavRoot.transform)) continue;
                var rt = img.rectTransform;
                if (rt == null) continue;
                Vector3[] icorners = new Vector3[4];
                rt.GetWorldCorners(icorners);
                var ir = Rect.MinMaxRect(icorners[0].x, icorners[0].y, icorners[2].x, icorners[2].y);
                if (!ir.Overlaps(topRect)) continue;

                var imgCanvas = img.GetComponentInParent<UnityEngine.Canvas>();
                int sort = imgCanvas != null ? imgCanvas.sortingOrder : 0;
                if (sort >= 5000)
                {
                    Debug.LogWarning($"[TopNav] BLOCKER: '{img.gameObject.name}' on canvas '{imgCanvas?.name}' sort={sort} — disabling raycastTarget");
                    img.raycastTarget = false;
                    killed++;
                }
                else
                {
                    Debug.Log($"[TopNav] overlap: '{img.gameObject.name}' on canvas '{imgCanvas?.name}' sort={sort} (below top, OK)");
                }
            }
            if (killed > 0) Debug.Log($"[TopNav] Disabled {killed} raycast blocker(s) above sort 5000");
        }

        private static void UnblockVfxOverlay(string goName)
        {
            var go = GameObject.Find(goName);
            if (go == null) return;
            // CRITICAL: ensure the canvas has a GraphicRaycaster — earlier code
            // may have destroyed it. Without it, any UI parented under this
            // canvas (like HomeNavButtons) becomes uninteractable.
            if (go.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            // Just turn off raycastTarget on each VFX Graphic so they don't
            // intercept clicks. Don't touch any functional UI children.
            // Heuristic: Images directly on this canvas OR on its fx layers.
            foreach (var g in go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                // Skip anything that's part of HomeNavButtons or another sub-tree
                bool isFunctionalChild = false;
                Transform t = g.transform;
                while (t != null && t != go.transform)
                {
                    if (t.name == "HomeNavButtons" || t.name == "BottomNav"
                        || t.GetComponent<UnityEngine.UI.Button>() != null)
                    { isFunctionalChild = true; break; }
                    t = t.parent;
                }
                if (isFunctionalChild) continue;
                g.raycastTarget = false;
            }
        }

        // Recursively find a Button by exact name inside a parent and wire it.
        private int WireTopBtnIn(Transform root, string exactName, UnityEngine.Events.UnityAction action)
        {
            var btn = FindBtnRecursive(root, exactName);
            if (btn == null)
            {
                Debug.LogWarning($"[TopNav] '{exactName}' not found under {root.name}");
                return 0;
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            // Force the button to be interactable
            btn.interactable = true;
            btn.gameObject.SetActive(true);
            // Ensure the button's own Image is a raycast target (so clicks register)
            var btnImg = btn.GetComponent<UnityEngine.UI.Image>();
            if (btnImg != null)
            {
                btnImg.raycastTarget = true;
                btnImg.enabled = true;
            }
            // Walk every parent up to the root and ensure CanvasGroups don't block,
            // any Canvas is enabled, and any GraphicRaycaster is enabled.
            Transform t = btn.transform;
            int chainLen = 0;
            var chain = new System.Text.StringBuilder($"chain for {exactName}: ");
            while (t != null)
            {
                var cg = t.GetComponent<UnityEngine.CanvasGroup>();
                if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; chain.Append($" [CG:{t.name} on]"); }
                var c = t.GetComponent<UnityEngine.Canvas>();
                if (c != null)
                {
                    c.enabled = true;
                    chain.Append($" [Canvas:{t.name} sort={c.sortingOrder} enabled={c.enabled}]");
                }
                var gr = t.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (gr != null) { gr.enabled = true; chain.Append($" [GR:{t.name} on]"); }
                t = t.parent;
                chainLen++;
                if (chainLen > 20) break;
            }
            // CRITICAL: turn off raycast on every CHILD graphic so they don't
            // intercept clicks before the parent Button can handle them.
            foreach (var g in btn.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                if (g.gameObject == btn.gameObject) continue;
                g.raycastTarget = false;
            }
            Debug.Log($"[TopNav] Wired {exactName} | active={btn.gameObject.activeInHierarchy} interactable={btn.interactable} | {chain}");
            return 1;
        }

        // 'BottomNav' is a LEGACY 6-tab bar sitting invisibly behind the live
        // nav (sort ~100, covered by HomeNavButtons + the lobby). It kept
        // catching fall-through clicks (the sniffer reported it). We don't use
        // it — dump it for the record, then kill every raycast target on it so
        // it stops interfering with the visible bar.
        private void NeutralizeBottomNav()
        {
            var navGO = GameObject.Find("BottomNav");
            if (navGO == null) return;
            var nav = navGO.transform;

            #if UNITY_EDITOR
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# BottomNav dump — {System.DateTime.Now}");
                DumpRec(nav, nav, sb);
                System.IO.File.WriteAllText(
                    Application.dataPath + "/_sparq_bottomnav_dump.txt", sb.ToString());
            }
            catch (System.Exception ex)
            { Debug.LogError($"[BottomNavBar] Dump failed: {ex.Message}"); }
            #endif

            int killed = 0;
            foreach (var g in nav.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (g != null && g.raycastTarget) { g.raycastTarget = false; killed++; }
            Debug.Log($"[BottomNavBar] Neutralised phantom 'BottomNav' — disabled {killed} raycast target(s).");
        }

        // The VISIBLE bottom bar is 'HomeNavButtons'. The user circled its 2nd
        // hex and wants it to open the Journal. Wire by POSITION (exactly what
        // was pointed at), make every hex clickable, and log the full
        // left-to-right mapping so it's verifiable.
        private void RouteHomeNavToJournal(Transform homeNavRoot)
        {
            var btns = new System.Collections.Generic.List<UnityEngine.UI.Button>(
                homeNavRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true));
            if (btns.Count == 0)
            {
                Debug.LogWarning("[TopNav] No buttons under HomeNavButtons — cannot wire Journal.");
                return;
            }

            foreach (var b in btns)
            {
                if (b == null) continue;
                b.interactable = true;
                var img = b.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.raycastTarget = true;
            }

            btns.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            for (int i = 0; i < btns.Count; i++)
                Debug.Log($"[TopNav] homeNav[{i}] = '{btns[i].gameObject.name}' x={btns[i].transform.position.x:F0}");

            const int journalIdx = 1;   // 2nd hex from the left — change if it's the wrong one
            if (journalIdx < btns.Count)
            {
                var jb = btns[journalIdx];
                jb.onClick.RemoveAllListeners();
                jb.onClick.AddListener(() => {
                    Debug.Log($"[TopNav] JOURNAL clicked ('{jb.gameObject.name}')");
                    try { Sparq.UI.JournalPanel.Show(); }
                    catch (System.Exception ex) { Debug.LogError($"JournalPanel: {ex.Message}"); }
                });
                Debug.Log($"[TopNav] Wired Journal → homeNav[{journalIdx}] = '{jb.gameObject.name}'.");
            }
        }

        #if UNITY_EDITOR
        // Recursively append a one-line summary of every descendant.
        private static void DumpRec(Transform t, Transform root, System.Text.StringBuilder sb)
        {
            int depth = 0; var p = t.parent;
            while (p != null && p != root.parent) { depth++; p = p.parent; }
            var img = t.GetComponent<UnityEngine.UI.Image>();
            var btn = t.GetComponent<UnityEngine.UI.Button>();
            var rt = t as RectTransform;
            string sz = rt != null ? $"{rt.rect.width:F0}x{rt.rect.height:F0}" : "-";
            sb.AppendLine($"{new string(' ', depth * 2)}'{t.name}'  active={t.gameObject.activeInHierarchy}  " +
                $"btn={(btn != null)}  img={(img != null)}  " +
                $"raycast={(img != null && img.raycastTarget)}  x={t.position.x:F0}  size={sz}");
            for (int i = 0; i < t.childCount; i++) DumpRec(t.GetChild(i), root, sb);
        }
        #endif

        private static UnityEngine.UI.Button FindBtnRecursive(Transform t, string name)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (c.name == name)
                {
                    var b = c.GetComponent<UnityEngine.UI.Button>();
                    if (b != null) return b;
                    b = c.GetComponentInChildren<UnityEngine.UI.Button>(true);
                    if (b != null) return b;
                }
                var deep = FindBtnRecursive(c, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private int WireTopBtn(string keyword, UnityEngine.Events.UnityAction action)
        {
            // Find by exact name first (PetsBtn, MapBtn etc.)
            string capitalized = char.ToUpper(keyword[0]) + keyword.Substring(1) + "Btn";
            foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.gameObject.name == capitalized)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(action);
                    return 1;
                }
            }
            // Fallback: keyword match (Tab_Map, Map, etc.) — but skip bottom-nav lookalikes
            foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = b.gameObject.name.ToLower();
                if (n.Contains(keyword) && !n.Contains("popup") && !n.Contains("close")
                    && !n.StartsWith("tab_"))   // tab_ prefix = bottom nav
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(action);
                    return 1;
                }
            }
            return 0;
        }

        private int WireTab(string nameKey, Tab tab)
        {
            // 1. Walk this object's hierarchy first (preferred location)
            var btn = FindButtonByName(transform, nameKey);
            // 2. Fallback: search the entire scene by tab name
            if (btn == null)
            {
                foreach (var b in Object.FindObjectsByType<UnityEngine.UI.Button>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string n = b.gameObject.name.ToLower();
                    // Match anything containing the tab name (Tab_Quests, QuestsBtn, etc.)
                    if (n.Contains(nameKey.ToLower())
                        && !n.Contains("tracker") && !n.Contains("addquest"))
                    {
                        btn = b; break;
                    }
                }
            }
            if (btn == null) return 0;
            btn.onClick.RemoveAllListeners();
            Tab captured = tab;
            btn.onClick.AddListener(() => OnTabClicked(captured));
            return 1;
        }

        private static UnityEngine.UI.Button FindButtonByName(Transform t, string nameKey)
        {
            string key = nameKey.ToLower();
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child.name.ToLower().Contains(key))
                {
                    var b = child.GetComponent<UnityEngine.UI.Button>();
                    if (b != null) return b;
                    b = child.GetComponentInChildren<UnityEngine.UI.Button>(true);
                    if (b != null) return b;
                }
                // Recurse
                var deep = FindButtonByName(child, nameKey);
                if (deep != null) return deep;
            }
            return null;
        }

        public void OnTabClicked(Tab tab)
        {
            // Quests tab: ALWAYS toggle the popup, regardless of activeTab.
            if (tab == Tab.Quests)
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.QuestsPanel.Show();
                activeTab = tab;
                UpdateActiveVisual();
                return;
            }

            // Journal tab: toggle the Journal popup.
            if (tab == Tab.Journal)
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.JournalPanel.Show();
                activeTab = tab;
                UpdateActiveVisual();
                return;
            }

            // Feed tab: toggle the Feed popup.
            if (tab == Tab.Feed)
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.FeedPanel.Show();
                activeTab = tab;
                UpdateActiveVisual();
                return;
            }

            // Remind tab: toggle the Remind popup.
            if (tab == Tab.Remind)
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.RemindPanel.Show();
                activeTab = tab;
                UpdateActiveVisual();
                return;
            }

            // Profile tab: toggle the Profile popup.
            if (tab == Tab.Profile)
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.ProfilePanel.Show();
                activeTab = tab;
                UpdateActiveVisual();
                return;
            }

            if (tab == activeTab) return;
            activeTab = tab;
            UpdateActiveVisual();

            if (tab == Tab.Home) return; // already on home
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);

            // Other tabs: coming-soon toast
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                XPFloater.Spawn(canvas.transform, transform.position + new Vector3(0, 200, 0),
                    $"{tab} — coming soon!", new Color(1f, 0.85f, 0.4f));
            }
        }

        public void RegisterTab(Tab tab, GameObject indicator, TMP_Text label)
        {
            _tabIndicators[tab] = indicator;
            _tabLabels[tab] = label;
        }

        private void UpdateActiveVisual()
        {
            foreach (var kv in _tabIndicators)
            {
                if (kv.Value != null) kv.Value.SetActive(kv.Key == activeTab);
            }
            foreach (var kv in _tabLabels)
            {
                if (kv.Value != null)
                {
                    kv.Value.color = (kv.Key == activeTab) ? new Color(1f, 0.92f, 0.35f) : new Color(0.7f, 0.65f, 0.85f);
                    kv.Value.fontStyle = (kv.Key == activeTab) ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        public static TabConfig[] GetTabs() => TABS;

        // Defensive — make absolutely sure an EventSystem exists in the scene.
        // Unity 6's empty-scene template no longer ships one, and procedural UI
        // gives no warning when it's missing — buttons just fail silently.
        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing != null && existing.isActiveAndEnabled)
            {
                Debug.Log("[BottomNavBar] EventSystem already present.");
                return;
            }

            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Try the new Input System's UI module first (reflection — avoids a
                // hard package dependency); fall back to StandaloneInputModule.
                go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<UnityEngine.EventSystems.EventSystem>();
            if (es != null) es.enabled = true;
            Debug.Log("[BottomNavBar] EventSystem ensured — UI clicks should now fire.");
        }
    }
}
