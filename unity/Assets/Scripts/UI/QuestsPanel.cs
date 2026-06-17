// QuestsPanel.cs — Mission-style rebuild modelled on Layer Lab's
// GUI Pro-FantasyHero Mission.prefab. Three tabs (Daily / Weekly /
// Achievements), themed row cards (icon + title + progress bar + reward
// chip + action button), and the existing streak banner / refresh / add-
// quest plumbing preserved.
//
// Public API preserved (kept for other panels' calls):
//   QuestsPanel.Show()
//   QuestsPanel.Hide()
//   QuestsPanel.RebuildIfOpen()
//
// Data sources:
//   Daily        → Sparq.Systems.QuestManager.GetActiveQuests() (today's pick)
//   Weekly       → Sparq.Systems.QuestCatalog.WeeklyPool() (informational)
//   Achievements → Sparq.Systems.QuestCatalog.Achievements (informational)
//
// Copy comes from Resources/sparq-content.json via QuestContent.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sparq.Models;
using Sparq.Systems;

namespace Sparq.UI
{
    public static class QuestsPanel
    {
        // ── Palette — same bright dark-theme as the prior panel ──────────
        private static readonly Color CARD_BG    = new Color(0.22f, 0.20f, 0.40f, 1f); // deep indigo body
        private static readonly Color STROKE     = new Color(0.42f, 0.22f, 0.68f, 1f); // fuchsia edge
        private static readonly Color TITLE_BG   = new Color(0.42f, 0.22f, 0.68f, 1f);
        private static readonly Color TAB_OFF    = new Color(0.28f, 0.24f, 0.46f, 1f);
        private static readonly Color TAB_ON     = new Color(0.96f, 0.66f, 0.10f, 1f); // bright gold = active tab
        private static readonly Color ROW_TODO   = new Color(0.34f, 0.30f, 0.56f, 1f);
        private static readonly Color ROW_DONE   = new Color(0.30f, 0.55f, 0.40f, 1f);
        private static readonly Color FILL_BG    = new Color(0.18f, 0.16f, 0.34f, 1f);
        private static readonly Color FILL_FG    = new Color(1.00f, 0.82f, 0.30f, 1f);
        private static readonly Color CHIP_BG    = new Color(0.96f, 0.66f, 0.10f, 1f); // gold reward chip
        private static readonly Color GO_BTN     = new Color(0.40f, 0.85f, 0.55f, 1f); // green Go
        private static readonly Color DONE_BTN   = new Color(0.40f, 0.85f, 0.55f, 1f);
        private static readonly Color CLAIM_BTN  = new Color(1.00f, 0.82f, 0.30f, 1f); // gold Claim
        private static readonly Color INK        = new Color(0.13f, 0.10f, 0.20f, 1f);
        private static readonly Color CREAM      = new Color(1.00f, 0.97f, 0.85f, 1f);
        private static readonly Color GREY       = new Color(0.75f, 0.75f, 0.80f, 1f);

        // ── Light theme — used when QuestsPanel is rendered as the HOME ──
        // Mirrors the Finch-style reference the user picked: cream page bg,
        // white rounded quest cards, soft-grey icon container, dark title
        // text, amber lightning-bolt + number for XP, green checkmark for the
        // complete button.
        private static readonly Color L_BG          = new Color(0.94f, 0.94f, 0.94f, 1f);
        private static readonly Color L_CARD        = new Color(1.00f, 1.00f, 1.00f, 1f);
        private static readonly Color L_ICON_PAD    = new Color(0.92f, 0.92f, 0.94f, 1f);
        private static readonly Color L_TITLE_BAR   = new Color(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color L_TITLE_TEXT  = new Color(0.16f, 0.18f, 0.22f, 1f);
        private static readonly Color L_BODY_TEXT   = new Color(0.20f, 0.22f, 0.26f, 1f);
        private static readonly Color L_SUB_TEXT    = new Color(0.40f, 0.42f, 0.46f, 1f);
        private static readonly Color L_BOLT        = new Color(0.95f, 0.62f, 0.20f, 1f);
        private static readonly Color L_CHECK_BG    = new Color(0.94f, 0.94f, 0.96f, 1f);
        private static readonly Color L_CHECK_GREEN = new Color(0.30f, 0.78f, 0.42f, 1f);
        private static readonly Color L_SECTION     = new Color(0.36f, 0.40f, 0.46f, 1f);

        // ── Tab state ────────────────────────────────────────────────────
        public enum Tab { Daily, Weekly, Achievements }
        private static Tab _currentTab = Tab.Daily;

        // ── Roots / runtime refs ─────────────────────────────────────────
        private static GameObject _root;
        private static Transform _listParent;
        private static TMP_Text _streakText, _progressText;
        private static Image _tabImgDaily, _tabImgWeekly, _tabImgAch;
        private static TMP_Text _tabTxtDaily, _tabTxtWeekly, _tabTxtAch;
        private static MonoBehaviour _runner;

        // Category → icon path map (uses the casual icon pack already in the project).
        private static readonly Dictionary<QuestCategory, string> CATEGORY_ICONS = new Dictionary<QuestCategory, string>
        {
            { QuestCategory.Pause,       "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png" },
            { QuestCategory.Focus,       "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Lightning01_Blue.png" },
            { QuestCategory.Reflection,  "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Misc_Documents_Map01.png" },
            { QuestCategory.Initiation,  "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Equipment_Weapon_Sword02.png" },
            { QuestCategory.Movement,    "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Lightning01_Blue.png" },
            { QuestCategory.Sleep,       "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Misc_ETC_Calendar01.png" },
            { QuestCategory.Social,      "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Misc_ETC_Chat01.png" },
            { QuestCategory.Finance,     "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Coin01_Gold.png" },
            { QuestCategory.Sensory,     "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png" },
            { QuestCategory.Meta,        "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Materials_CogWheel01.png" },
            { QuestCategory.Recovery,    "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png" },
            { QuestCategory.SocialDrama, "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Misc_ETC_Chat01.png" },
            { QuestCategory.Safety,      "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png" },
            { QuestCategory.Floor,       "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Star01_Gold.png" },
        };
        private const string DEFAULT_ICON = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Misc_Documents_Mission01.png";
        private const string ACHIEVEMENT_ICON = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Collectibles_Trophy01_Gold.png";

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        // When true, the panel renders as the HOME SCREEN rather than as a
        // modal popup over the lobby:
        //   - no dim backdrop, no tap-to-close
        //   - low sortingOrder so the BottomNavBar (at 5000) sits above it
        //   - no close-X button on the title bar
        //   - Hide() becomes a no-op (HOME is permanent until the user
        //     navigates to another tab)
        //   - card fills the full screen with only status-bar + nav-bar
        //     reservations at the edges
        // Set by Show(asHome:true) and read by BuildTitleBar / Hide.
        private static bool _isHomeMode;

        public static void Show() => Show(asHome: false);

        public static void Show(bool asHome)
        {
            // If we're already rendered AS HOME, ANY re-Show — whether the
            // caller passed asHome:true (HOME tab) or asHome:false (the
            // BottomNavBar's QUESTS tab calls Show() with no arg) — should
            // just rebuild the list, not destroy + rebuild the canvas. The
            // home view IS the quest view; there's nothing to switch to.
            if (_root != null && _isHomeMode)
            {
                RebuildList();
                return;
            }
            if (_root != null) { Hide(force: true); }
            _isHomeMode = asHome;
            EnsureEventSystem();
            try { QuestManager.Instance?.CheckDailyReset(); } catch {}

            _root = new GameObject("Sparq_QuestsPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>(); Stretch(rrt);
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            if (asHome)
            {
                // Sit BELOW BottomNavBar (sort 5000) so the nav stays tappable
                // on top of us. Above 0 so popups/toasts still cover us.
                canv.sortingOrder = 100;
            }
            else
            {
                // Modal: float above everything currently on screen.
                int maxSort = 15000;
                foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                        maxSort = other.sortingOrder;
                canv.sortingOrder = maxSort + 20;
            }
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop only in modal mode — at HOME the panel IS the screen,
            // there's nothing to dim. We also drop the tap-to-close behaviour.
            if (!asHome)
            {
                var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
                Stretch(dim.GetComponent<RectTransform>());
                dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
                dim.GetComponent<Button>().onClick.AddListener(Hide);
            }

            // Card layout: modal uses inset margins for the dim border;
            // home mode fills the screen with just status-bar + nav-bar
            // reservations so the list maximises vertical real-estate.
            var stroke = MakeRounded("Stroke", _root.transform, asHome ? L_BG : STROKE);
            var srt = stroke.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            var card = MakeRounded("Card", _root.transform, asHome ? L_BG : CARD_BG);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 1);
            if (asHome)
            {
                // 130px top = status-bar + TopCurrencyBar room (110 + status),
                // 200px bottom = BottomNavBar room.
                srt.offsetMin = new Vector2(0,  200); srt.offsetMax = new Vector2(0,  -130);
                crt.offsetMin = new Vector2(8,  208); crt.offsetMax = new Vector2(-8, -138);
            }
            else
            {
                srt.offsetMin = new Vector2(36, 140); srt.offsetMax = new Vector2(-36, -90);
                crt.offsetMin = new Vector2(44, 148); crt.offsetMax = new Vector2(-44, -98);
            }

            BuildTitleBar(card.transform);
            // The reference home design (Finch-style) drops the streak banner
            // and the Daily/Weekly/Achievements tab strip — sections inside
            // the scroll list take over the grouping role. Skip those two in
            // home mode to keep the view scannable.
            if (!asHome)
            {
                BuildStreakBanner(card.transform);
                BuildTabStrip(card.transform);
            }
            BuildScrollList(card.transform);
            if (!asHome) BuildBottomBar(card.transform);

            Subscribe();
            RebuildList();

            Debug.Log($"[QuestsPanel] Opened (asHome={asHome}).");
        }

        public static void Hide() => Hide(force: false);

        public static void Hide(bool force)
        {
            // In HOME mode the panel is the home screen — refuse to close
            // unless the caller explicitly forces it (e.g., re-show with
            // different asHome flag, or a navigation tab switching us out).
            if (_isHomeMode && !force) return;
            Unsubscribe();
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _listParent = null;
            _streakText = _progressText = null;
            _tabImgDaily = _tabImgWeekly = _tabImgAch = null;
            _tabTxtDaily = _tabTxtWeekly = _tabTxtAch = null;
        }

        public static void RebuildIfOpen()
        {
            if (_root == null) return;
            RebuildList();
            UpdateBanner();
        }

        // ─────────────────────────────────────────────────────────────────
        // BUILDERS — title bar, streak banner, tab strip, scroll list, bottom bar
        // ─────────────────────────────────────────────────────────────────

        private static void BuildTitleBar(Transform card)
        {
            var bar = NewGO("TitleBar", card, typeof(Image));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, 120);
            bar.GetComponent<Image>().color = _isHomeMode ? L_TITLE_BAR : TITLE_BG;

            // Title text: in home mode, surface the "X goals left for today!"
            // status that the reference uses instead of a static "QUESTS" label.
            string titleText = _isHomeMode ? GetGoalsRemainingText() : "QUESTS";
            var title = MakeText(bar.transform, "Title", titleText,
                                 _isHomeMode ? 42 : 56,
                                 FontStyles.Bold,
                                 _isHomeMode ? L_TITLE_TEXT : CREAM);
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;
            if (!_isHomeMode)
                try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0.20f, 0.08f, 0.30f); } catch {}

            // Skip the close-X in home mode — HOME is permanent; the user
            // navigates away via the BottomNavBar, not by closing this card.
            if (!_isHomeMode)
            {
                var close = NewGO("Close", bar.transform, typeof(Image), typeof(Button));
                var xrt = close.GetComponent<RectTransform>();
                xrt.anchorMin = new Vector2(1, 0.5f); xrt.anchorMax = new Vector2(1, 0.5f);
                xrt.pivot = new Vector2(1, 0.5f);
                xrt.anchoredPosition = new Vector2(-20, 0);
                xrt.sizeDelta = new Vector2(78, 78);
                close.GetComponent<Image>().color = new Color(0.82f, 0.26f, 0.26f, 1f);
                var xl = MakeText(close.transform, "X", "X", 44, FontStyles.Bold, Color.white);
                Stretch(xl.rectTransform); xl.alignment = TextAlignmentOptions.Center;
                close.GetComponent<Button>().onClick.AddListener(() => Hide());
            }
        }

        private static void BuildStreakBanner(Transform card)
        {
            var banner = NewGO("StreakBanner", card, typeof(Image));
            var rt = banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -120);
            rt.sizeDelta = new Vector2(0, 70);
            banner.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.34f, 1f);

            _streakText = MakeText(banner.transform, "Streak", "", 30, FontStyles.Bold, CREAM);
            var stRT = _streakText.rectTransform;
            stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(0.5f, 1);
            stRT.offsetMin = new Vector2(28, 0); stRT.offsetMax = new Vector2(-10, 0);
            _streakText.alignment = TextAlignmentOptions.MidlineLeft;

            _progressText = MakeText(banner.transform, "Progress", "", 30, FontStyles.Bold, CREAM);
            var pgRT = _progressText.rectTransform;
            pgRT.anchorMin = new Vector2(0.5f, 0); pgRT.anchorMax = new Vector2(1, 1);
            pgRT.offsetMin = new Vector2(10, 0); pgRT.offsetMax = new Vector2(-28, 0);
            _progressText.alignment = TextAlignmentOptions.MidlineRight;

            UpdateBanner();
        }

        private static void BuildTabStrip(Transform card)
        {
            var strip = NewGO("TabStrip", card, typeof(HorizontalLayoutGroup));
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -198);
            rt.sizeDelta = new Vector2(-24, 96);
            var hlg = strip.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(12, 12, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            (_tabImgDaily,  _tabTxtDaily)  = BuildTab(strip.transform, "DAILY",        Tab.Daily);
            (_tabImgWeekly, _tabTxtWeekly) = BuildTab(strip.transform, "WEEKLY",       Tab.Weekly);
            (_tabImgAch,    _tabTxtAch)    = BuildTab(strip.transform, "ACHIEVEMENTS", Tab.Achievements);
            ApplyTabStyles();
        }

        private static (Image bg, TMP_Text lbl) BuildTab(Transform parent, string label, Tab tab)
        {
            var tabGO = NewGO("Tab_" + tab, parent, typeof(Image), typeof(Button));
            var img = tabGO.GetComponent<Image>();
            var btn = tabGO.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;
            btn.onClick.AddListener(() => SetTab(tab));

            var lbl = MakeText(tabGO.transform, "L", label, 30, FontStyles.Bold, CREAM);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            return (img, lbl);
        }

        private static void SetTab(Tab t)
        {
            if (_currentTab == t) return;
            _currentTab = t;
            ApplyTabStyles();
            RebuildList();
        }

        private static void ApplyTabStyles()
        {
            void Style(Image bg, TMP_Text lbl, bool on)
            {
                if (bg  != null) bg.color  = on ? TAB_ON : TAB_OFF;
                if (lbl != null) lbl.color = on ? INK    : CREAM;
            }
            Style(_tabImgDaily,  _tabTxtDaily,  _currentTab == Tab.Daily);
            Style(_tabImgWeekly, _tabTxtWeekly, _currentTab == Tab.Weekly);
            Style(_tabImgAch,    _tabTxtAch,    _currentTab == Tab.Achievements);
        }

        private static void BuildScrollList(Transform card)
        {
            var scrollGO = NewGO("Scroll", card, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            // Home mode skips streak banner (70px), tab strip (~90px), and
            // bottom bar (~160px) — reclaim that space so the quest list
            // fills the screen. The title bar (120px) is still there.
            if (_isHomeMode)
            { srRT.offsetMin = new Vector2(16, 20); srRT.offsetMax = new Vector2(-16, -130); }
            else
            { srRT.offsetMin = new Vector2(20, 140); srRT.offsetMax = new Vector2(-20, -300); }
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var viewport = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            var vpRT = viewport.GetComponent<RectTransform>(); Stretch(vpRT);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", viewport.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            ctRT.sizeDelta = new Vector2(0, ctRT.sizeDelta.y);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRT; sr.content = ctRT;
            _listParent = content.transform;
        }

        private static void BuildBottomBar(Transform card)
        {
            var bar = NewGO("BottomBar", card, typeof(HorizontalLayoutGroup));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 24);
            rt.sizeDelta = new Vector2(-40, 100);
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            BuildBottomButton(bar.transform, "+ ADD QUEST", new Color(0.40f, 0.85f, 0.55f, 1f), () => {
                try { CustomQuestCreator.Show(); } catch (System.Exception ex)
                { Debug.LogError($"[QuestsPanel] CustomQuestCreator.Show failed: {ex.Message}"); }
            });
            BuildBottomButton(bar.transform, "REFRESH",     new Color(0.42f, 0.72f, 1.00f, 1f), () => {
                try { QuestManager.Instance?.ForceRefresh(); } catch {}
                RebuildList(); UpdateBanner();
            });
        }

        private static void BuildBottomButton(Transform parent, string label, Color bg, System.Action onClick)
        {
            var go = NewGO("BBtn_" + label, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            img.color = bg; img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            var lbl = MakeText(go.transform, "L", label, 30, FontStyles.Bold, INK);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        // ─────────────────────────────────────────────────────────────────
        // LIST CONTENT — switches by current tab
        // ─────────────────────────────────────────────────────────────────

        private static void RebuildList()
        {
            if (_listParent == null) return;
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);

            switch (_currentTab)
            {
                case Tab.Daily:        BuildDailyRows();        break;
                case Tab.Weekly:       BuildWeeklyRows();       break;
                case Tab.Achievements: BuildAchievementRows();  break;
            }
        }

        private static void BuildDailyRows()
        {
            // First-open + brand-new-tester fix: if no active quests have
            // been rolled yet, ForceRefresh seeds today's set so the menu
            // isn't "No quests yet — tap REFRESH" on first sight.
            var quests = QuestManager.Instance?.GetActiveQuests();
            if ((quests == null || quests.Count == 0) && QuestManager.Instance != null)
            {
                QuestManager.Instance.ForceRefresh();
                quests = QuestManager.Instance.GetActiveQuests();
            }

            var activeIds = new System.Collections.Generic.HashSet<string>();
            if (quests != null)
                foreach (var q in quests)
                    if (!string.IsNullOrEmpty(q.questId)) activeIds.Add(q.questId);
            var pool = Sparq.Systems.QuestCatalog.DailyPool(includeSensitive: false);

            // Home (light) mode renders section headers + interleaves the
            // active interactive quests with their preview-only siblings.
            // Modal (legacy) mode keeps the old "active first, preview after"
            // layout without section dividers.
            if (_isHomeMode)
            {
                // ── Start the day ───────────────────────────────────────────
                BuildSectionHeader("Start the day");
                int morningCount = 0;
                if (quests != null)
                    foreach (var q in quests)
                        if (IsMorningCategory(LookupCategory(q))) { BuildQuestRow(q); morningCount++; }
                foreach (var q in pool)
                {
                    if (activeIds.Contains(q.id)) continue;
                    if (!IsMorningCategory(q.category)) continue;
                    BuildInfoRow(q.id, QuestContent.GetShortLabel(q.id), q.xp, q.category, faded: true);
                    morningCount++;
                }
                if (morningCount == 0) BuildEmpty("No morning quests available.");

                // ── Any time ────────────────────────────────────────────────
                BuildSectionHeader("Any time");
                if (quests != null)
                    foreach (var q in quests)
                        if (!IsMorningCategory(LookupCategory(q))) BuildQuestRow(q);
                foreach (var q in pool)
                {
                    if (activeIds.Contains(q.id)) continue;
                    if (IsMorningCategory(q.category)) continue;
                    BuildInfoRow(q.id, QuestContent.GetShortLabel(q.id), q.xp, q.category, faded: true);
                }
                return;
            }

            // Legacy modal layout: active first, then the rest.
            if (quests != null)
                foreach (var q in quests) BuildQuestRow(q);
            foreach (var q in pool)
            {
                if (activeIds.Contains(q.id)) continue;
                BuildInfoRow(q.id,
                             QuestContent.GetShortLabel(q.id),
                             q.xp, q.category, faded: true);
            }
        }

        // ── Section header (Finch-style) — used only in home mode ────────
        private static void BuildSectionHeader(string label)
        {
            var row = NewGO("Section", _listParent, typeof(Image), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = 76;
            row.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var t = MakeText(row.transform, "T", label, 28, FontStyles.Bold, L_SECTION);
            var tRT = t.rectTransform;
            tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
            tRT.offsetMin = new Vector2(16, 0); tRT.offsetMax = new Vector2(-16, 0);
            t.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // Map a customTask back to a catalog Category so we can group it.
        private static QuestCategory LookupCategory(Sparq.Core.CustomTask q)
        {
            if (q == null || string.IsNullOrEmpty(q.questId)) return QuestCategory.Floor;
            var def = QuestCatalog.Get(q.questId);
            return def != null ? def.category : QuestCategory.Floor;
        }

        // Categories that belong under "Start the day". Initiation (e.g.
        // two_minute_start, open_curtains) and Sensory (sensory_reset,
        // transition_buffer — useful for the morning-routine transition) fit
        // here. Everything else lives in "Any time".
        private static bool IsMorningCategory(QuestCategory cat)
        {
            return cat == QuestCategory.Initiation
                || cat == QuestCategory.Sensory
                || cat == QuestCategory.Sleep;
        }

        // "X goals left for today!" status text for the home-mode title bar.
        private static string GetGoalsRemainingText()
        {
            var quests = QuestManager.Instance?.GetActiveQuests();
            if (quests == null) return "Today's quests";
            int left = 0;
            foreach (var q in quests) if (q != null && !q.done) left++;
            if (left == 0) return "All goals done — nice work!";
            return left == 1 ? "1 goal left for today!" : $"{left} goals left for today!";
        }

        private static void BuildWeeklyRows()
        {
            var weekly = QuestCatalog.WeeklyPool();
            if (weekly == null || weekly.Count == 0)
            {
                BuildEmpty("No weekly missions available.");
                return;
            }
            foreach (var q in weekly) BuildInfoRow(q.id, QuestContent.GetShortLabel(q.id), q.xp, q.category, false);
        }

        private static void BuildAchievementRows()
        {
            foreach (var kv in QuestCatalog.Achievements)
            {
                var ach = kv.Value;
                var title = QuestContent.GetAchievementTitle(ach.id);
                var hint  = QuestContent.GetAchievementCondition(ach.id);
                BuildAchievementRow(ach.id, title, hint, ach.xp);
            }
        }

        // ── Row: daily quest (interactive, from customTasks) ─────────────
        private static void BuildQuestRow(Sparq.Core.CustomTask quest)
        {
            // Look up the catalog entry for category/icon. Falls back to a
            // generic mission icon if this is a legacy custom quest.
            var def = !string.IsNullOrEmpty(quest.questId) ? QuestCatalog.Get(quest.questId) : null;
            string title = !string.IsNullOrEmpty(quest.questId)
                ? QuestContent.GetShortLabel(quest.questId)
                : (string.IsNullOrEmpty(quest.name) ? "Quest" : quest.name);
            string iconPath = def != null && CATEGORY_ICONS.TryGetValue(def.category, out var p) ? p : DEFAULT_ICON;

            var row = MakeRow(quest.done ? ROW_DONE : ROW_TODO);
            BuildRowIcon(row.transform, iconPath, FILL_FG);
            BuildRowTitle(row.transform, title);
            BuildRowFill(row.transform, quest.done ? 1f : 0f);
            BuildRowXpChip(row.transform, quest.xp);

            // Action button — Go (todo) or Done (already complete).
            if (quest.done)
            {
                BuildRowAction(row.transform, "✓", DONE_BTN, null);
            }
            else
            {
                BuildRowAction(row.transform, "Go", GO_BTN, () => {
                    // Spoon Check is a special quest: tap → open the picker
                    // rather than auto-complete.
                    if (quest.questId == "spoon_check")
                    {
                        try { SpoonCheckPanel.Show(); }
                        catch (System.Exception ex)
                        { Debug.LogError($"[QuestsPanel] SpoonCheckPanel.Show failed: {ex.Message}"); }
                        return;
                    }
                    OnQuestComplete(quest, row.transform);
                });
            }
        }

        // ── Row: informational (weekly mission) — no completion state ────
        private static void BuildInfoRow(string questId, string title, int xp, QuestCategory cat, bool faded)
        {
            string iconPath = CATEGORY_ICONS.TryGetValue(cat, out var p) ? p : DEFAULT_ICON;
            var row = MakeRow(faded ? new Color(0.22f, 0.20f, 0.40f, 0.6f) : ROW_TODO);
            BuildRowIcon(row.transform, iconPath, FILL_FG);
            BuildRowTitle(row.transform, title);
            BuildRowFill(row.transform, 0f);
            BuildRowXpChip(row.transform, xp);
            BuildRowAction(row.transform, "Soon", new Color(0.55f, 0.55f, 0.60f, 1f), null);
        }

        // ── Row: achievement ─────────────────────────────────────────────
        private static void BuildAchievementRow(string achId, string title, string hint, int xp)
        {
            var row = MakeRow(ROW_TODO);
            BuildRowIcon(row.transform, ACHIEVEMENT_ICON, new Color(1.00f, 0.82f, 0.30f, 1f));
            BuildRowTitle(row.transform, title, hint);
            BuildRowFill(row.transform, 0f);   // unlock progress not tracked yet
            BuildRowXpChip(row.transform, xp);
            BuildRowAction(row.transform, "—", new Color(0.55f, 0.55f, 0.60f, 1f), null);
        }

        // ── Empty-state card ─────────────────────────────────────────────
        private static void BuildEmpty(string text)
        {
            var card = NewGO("Empty", _listParent, typeof(Image), typeof(LayoutElement));
            card.GetComponent<LayoutElement>().preferredHeight = 110;
            card.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var t = MakeText(card.transform, "T", text, 30, FontStyles.Normal, CREAM);
            Stretch(t.rectTransform);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
        }

        // ─────────────────────────────────────────────────────────────────
        // ROW BUILDERS — the Mission-template look
        // ─────────────────────────────────────────────────────────────────

        private static GameObject MakeRow(Color bg)
        {
            var row = NewGO("Row", _listParent, typeof(Image), typeof(LayoutElement));
            // Light home cards are slimmer than the legacy stacked rows.
            row.GetComponent<LayoutElement>().preferredHeight = _isHomeMode ? 140 : 168;
            row.GetComponent<Image>().color = _isHomeMode ? L_CARD : bg;
            return row;
        }

        private static void BuildRowIcon(Transform row, string iconPath, Color tint)
        {
            // Soft squircle backdrop behind icon. Light theme: pale grey square
            // matching the reference; original theme: deep indigo.
            var pad = NewGO("IconPad", row, typeof(Image));
            var pRT = pad.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0, 0.5f); pRT.anchorMax = new Vector2(0, 0.5f);
            pRT.pivot = new Vector2(0, 0.5f);
            pRT.anchoredPosition = new Vector2(20, 0);
            pRT.sizeDelta = new Vector2(108, 108);
            pad.GetComponent<Image>().color = _isHomeMode
                ? L_ICON_PAD
                : new Color(0.18f, 0.16f, 0.34f, 0.85f);
            pad.GetComponent<Image>().raycastTarget = false;

            var ico = NewGO("Icon", pad.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            Stretch(iRT); iRT.offsetMin = new Vector2(12, 12); iRT.offsetMax = new Vector2(-12, -12);
            var iImg = ico.GetComponent<Image>();
            var sp = LoadSprite(iconPath);
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            // In light theme we keep the icon's native color (full saturation),
            // since the soft-grey pad is the only thing tinting visually.
            iImg.color = _isHomeMode ? Color.white : tint;
            iImg.raycastTarget = false;
        }

        private static void BuildRowTitle(Transform row, string title, string subtitle = null)
        {
            // Title — bold dark text in light theme (against the white card),
            // bold cream + outline in legacy theme (against the indigo card).
            var t = MakeText(row, "Title", title, 34, FontStyles.Bold,
                             _isHomeMode ? L_TITLE_TEXT : CREAM);
            if (!_isHomeMode)
                try { t.outlineWidth = 0.18f; t.outlineColor = new Color(0, 0, 0, 0.7f); } catch {}
            var tRT = t.rectTransform;
            if (_isHomeMode)
            {
                // Light theme: title is vertically centered (no fill bar below).
                tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
                tRT.pivot = new Vector2(0, 0.5f);
                tRT.offsetMin = new Vector2(150, 0); tRT.offsetMax = new Vector2(-220, 0);
                t.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                tRT.anchorMin = new Vector2(0, 0.5f); tRT.anchorMax = new Vector2(1, 1);
                tRT.pivot = new Vector2(0, 0.5f);
                tRT.offsetMin = new Vector2(160, -10); tRT.offsetMax = new Vector2(-200, -14);
                t.alignment = TextAlignmentOptions.BottomLeft;
            }
            t.textWrappingMode = TextWrappingModes.Normal;

            if (!string.IsNullOrEmpty(subtitle))
            {
                // Sub — was 22 italic at 75% (hard to read). Now 26 non-italic at
                // 92% opacity so it's legible without competing with the title.
                var s = MakeText(row, "Sub", subtitle, 26, FontStyles.Normal, new Color(1f, 0.97f, 0.85f, 0.92f));
                var sRT = s.rectTransform;
                sRT.anchorMin = new Vector2(0, 0.5f); sRT.anchorMax = new Vector2(1, 0.5f);
                sRT.pivot = new Vector2(0, 1);
                sRT.offsetMin = new Vector2(160, -36); sRT.offsetMax = new Vector2(-200, -4);
                s.alignment = TextAlignmentOptions.TopLeft;
                s.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static void BuildRowFill(Transform row, float ratio)
        {
            // Light home theme has no fill bar — the row is a clean white
            // card; progress is implied by the checkmark, not a gauge.
            if (_isHomeMode) return;

            ratio = Mathf.Clamp01(ratio);
            var bgGO = NewGO("FillBg", row, typeof(Image));
            var brt = bgGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0, 0);
            brt.anchoredPosition = new Vector2(160, 18);
            brt.sizeDelta = new Vector2(-340, 28);   // leave space for the right-side action button
            bgGO.GetComponent<Image>().color = FILL_BG;
            bgGO.GetComponent<Image>().raycastTarget = false;

            var fgGO = NewGO("FillFg", bgGO.transform, typeof(Image));
            var frt = fgGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(ratio, 1);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fgGO.GetComponent<Image>().color = FILL_FG;
            fgGO.GetComponent<Image>().raycastTarget = false;
        }

        private static void BuildRowXpChip(Transform row, int xp)
        {
            if (_isHomeMode)
            {
                // Light theme: number + lightning-bolt sprite inline next to
                // the checkmark. Replaces an earlier unicode "⚡" attempt that
                // rendered as an empty square on the project's TMP font.
                // Asset is the Layer Lab casual icon pack bolt; we tint amber.
                const string BOLT = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Lightning01_Blue.png";

                var lbl = MakeText(row, "Xp", xp.ToString(),
                    28, FontStyles.Bold, L_BODY_TEXT);
                var lRT = lbl.rectTransform;
                lRT.anchorMin = new Vector2(1, 0.5f); lRT.anchorMax = new Vector2(1, 0.5f);
                lRT.pivot = new Vector2(1, 0.5f);
                lRT.anchoredPosition = new Vector2(-180, 0);
                lRT.sizeDelta = new Vector2(70, 50);
                lbl.alignment = TextAlignmentOptions.MidlineRight;

                var boltGO = NewGO("Bolt", row, typeof(Image));
                var bRT = boltGO.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(1, 0.5f); bRT.anchorMax = new Vector2(1, 0.5f);
                bRT.pivot = new Vector2(1, 0.5f);
                bRT.anchoredPosition = new Vector2(-138, 0);
                bRT.sizeDelta = new Vector2(36, 36);
                var bImg = boltGO.GetComponent<Image>();
                var sp = LoadSprite(BOLT);
                if (sp != null) { bImg.sprite = sp; bImg.preserveAspect = true; }
                bImg.color = L_BOLT;        // tint amber for the Finch look
                bImg.raycastTarget = false;
                return;
            }
            var chip = NewGO("XpChip", row, typeof(Image));
            var crt = chip.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1);
            crt.anchoredPosition = new Vector2(-18, -14);
            crt.sizeDelta = new Vector2(140, 50);
            chip.GetComponent<Image>().color = CHIP_BG;
            chip.GetComponent<Image>().raycastTarget = false;

            var lblOld = MakeText(chip.transform, "L", $"+{xp} XP", 28, FontStyles.Bold, INK);
            Stretch(lblOld.rectTransform); lblOld.alignment = TextAlignmentOptions.Center;
        }

        private static void BuildRowAction(Transform row, string label, Color color, System.Action onClick)
        {
            if (_isHomeMode)
            {
                // Light theme: a rounded soft-grey square containing a green
                // ✓. Tapping completes the quest. Empty label / disabled is
                // a faded ✓ — visual distinction comes from the icon, not a
                // separate "Soon" word.
                bool done   = label == "✓";
                bool active = onClick != null && !done;

                var btn = NewGO("Action", row, typeof(Image), typeof(Button));
                var brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
                brt.pivot = new Vector2(1, 0.5f);
                brt.anchoredPosition = new Vector2(-20, 0);
                brt.sizeDelta = new Vector2(96, 96);
                var img = btn.GetComponent<Image>();
                img.color = L_CHECK_BG; img.raycastTarget = true;
                var b = btn.GetComponent<Button>();
                b.targetGraphic = img; b.interactable = active;

                var check = MakeText(btn.transform, "Chk", "✓", 56, FontStyles.Bold,
                    done                 ? L_CHECK_GREEN
                    : active             ? L_CHECK_GREEN
                                         : new Color(0.78f, 0.80f, 0.84f, 1f));
                Stretch(check.rectTransform);
                check.alignment = TextAlignmentOptions.Center;
                if (onClick != null) b.onClick.AddListener(() => onClick.Invoke());
                return;
            }
            var btnL = NewGO("Action", row, typeof(Image), typeof(Button));
            var brtL = btnL.GetComponent<RectTransform>();
            brtL.anchorMin = new Vector2(1, 0); brtL.anchorMax = new Vector2(1, 0);
            brtL.pivot = new Vector2(1, 0);
            brtL.anchoredPosition = new Vector2(-18, 14);
            brtL.sizeDelta = new Vector2(160, 80);
            var imgL = btnL.GetComponent<Image>();
            imgL.color = color; imgL.raycastTarget = true;
            var bL = btnL.GetComponent<Button>();
            bL.targetGraphic = imgL; bL.interactable = onClick != null;
            var lblL = MakeText(btnL.transform, "L", label, 32, FontStyles.Bold, INK);
            Stretch(lblL.rectTransform); lblL.alignment = TextAlignmentOptions.Center;
            if (onClick != null) bL.onClick.AddListener(() => onClick.Invoke());
        }

        // ─────────────────────────────────────────────────────────────────
        // EVENT HANDLERS
        // ─────────────────────────────────────────────────────────────────

        private static void Subscribe()
        {
            var mgr = QuestManager.Instance;
            if (mgr == null) return;
            mgr.OnQuestCompleted += OnQuestCompletedEvent;
            mgr.OnDailyReset     += OnDailyResetEvent;
        }

        private static void Unsubscribe()
        {
            var mgr = QuestManager.Instance;
            if (mgr == null) return;
            mgr.OnQuestCompleted -= OnQuestCompletedEvent;
            mgr.OnDailyReset     -= OnDailyResetEvent;
        }

        private static void OnQuestCompletedEvent(Sparq.Core.CustomTask q) { RebuildList(); UpdateBanner(); }
        private static void OnDailyResetEvent() { RebuildList(); UpdateBanner(); }

        private static void OnQuestComplete(Sparq.Core.CustomTask q, Transform rowTransform)
        {
            QuestManager.Instance?.CompleteQuest(q);
            EnsureRunner();
            if (_runner != null && rowTransform != null)
                _runner.StartCoroutine(BurstAt(rowTransform.position));
        }

        private static void UpdateBanner()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            int streak  = data.streak;
            int shields = data.streakShields;
            int done = 0, total = data.customTasks?.Count ?? 0;
            if (data.customTasks != null) foreach (var t in data.customTasks) if (t.done) done++;

            if (_streakText != null)
                _streakText.text = streak > 0
                    ? (shields > 0 ? $"{streak}d streak  •  {shields}🛡" : $"{streak}d streak")
                    : "Log a quest to start a streak";

            if (_progressText != null)
                _progressText.text = $"Today  {done}/{total}";
        }

        // ─────────────────────────────────────────────────────────────────
        // PARTICLE BURST (kept from the previous panel)
        // ─────────────────────────────────────────────────────────────────

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("QuestsPanelRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<BurstRunner>();
        }

        private class BurstRunner : MonoBehaviour {}

        private static IEnumerator BurstAt(Vector3 worldPos)
        {
            if (_root == null) yield break;
            int n = 10;
            for (int i = 0; i < n; i++)
            {
                var p = NewGO("Spark", _root.transform, typeof(Image));
                var rt = p.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(16, 16);
                rt.position = worldPos + Vector3.up * 10;
                p.GetComponent<Image>().color = FILL_FG;

                float ang = (i / (float)n) * Mathf.PI * 2;
                Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * 120;
                float life = 0.5f;
                float t = 0;
                while (t < life)
                {
                    if (p == null) yield break;
                    t += Time.deltaTime;
                    rt.position += dir * Time.deltaTime;
                    var c = p.GetComponent<Image>().color;
                    c.a = 1f - (t / life);
                    p.GetComponent<Image>().color = c;
                    yield return null;
                }
                if (p != null) UnityEngine.Object.Destroy(p);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // PRIMITIVES
        // ─────────────────────────────────────────────────────────────────

        private static GameObject MakeRounded(string name, Transform parent, Color color)
        {
            var go = NewGO(name, parent, typeof(Image));
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static Sprite LoadSprite(string assetPath) => Sparq.Core.SpriteLoader.Load(assetPath);

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
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
