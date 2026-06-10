using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// World / community panel — Friends, Feed, Leaderboard tabs.
    /// Built on-demand via Show(); torn down on close. Mock data for now;
    /// the data structures here mirror what a real backend would feed.
    /// </summary>
    public static class WorldPanel
    {
        // ───────── Mock data ─────────
        private struct Friend { public string name; public int level; public string status; public Color tint; public bool online; }
        private struct FeedPost { public string author; public string action; public string time; public Color tint; }
        private struct LeaderRow { public int rank; public string name; public int xp; public bool isYou; }

        private static readonly Friend[] FRIENDS = new[]
        {
            new Friend { name = "Aria",   level = 8, status = "Just slayed a boss",     tint = new Color(0.85f,0.40f,0.50f), online = true  },
            new Friend { name = "Bram",   level = 6, status = "Reading the Tomes",      tint = new Color(0.55f,0.85f,0.45f), online = true  },
            new Friend { name = "Ciel",   level = 5, status = "Logged spirit: Calm",    tint = new Color(0.55f,0.75f,1f),    online = false },
            new Friend { name = "Dax",    level = 9, status = "5-day streak",            tint = new Color(1f,0.65f,0.30f),    online = true  },
            new Friend { name = "Elin",   level = 4, status = "Walked the path",        tint = new Color(0.65f,0.55f,0.85f), online = false },
        };

        private static readonly FeedPost[] FEED = new[]
        {
            new FeedPost { author = "Dax",   action = "completed Forest Patrol",    time = "2m",  tint = new Color(0.85f,0.40f,0.45f) },
            new FeedPost { author = "Aria",  action = "earned a Legendary chest",   time = "12m", tint = new Color(1f,0.82f,0.32f)    },
            new FeedPost { author = "Bram",  action = "practiced Cycle of Four",    time = "26m", tint = new Color(0.55f,0.85f,1f)    },
            new FeedPost { author = "Elin",  action = "leveled up to 5",            time = "1h",  tint = new Color(0.65f,0.55f,0.85f) },
            new FeedPost { author = "Ciel",  action = "started a 3-day streak",     time = "3h",  tint = new Color(1f,0.65f,0.30f)    },
        };

        private static readonly LeaderRow[] LEADER = new[]
        {
            new LeaderRow { rank = 1, name = "Dax",   xp = 4280, isYou = false },
            new LeaderRow { rank = 2, name = "Aria",  xp = 3950, isYou = false },
            new LeaderRow { rank = 3, name = "Karu",  xp = 3120, isYou = true  },
            new LeaderRow { rank = 4, name = "Bram",  xp = 2810, isYou = false },
            new LeaderRow { rank = 5, name = "Elin",  xp = 1640, isYou = false },
        };

        // ── Charcoal fantasy palette (matches Store / Pet / Guild panels) ──
        // CREAM = light text on dark rows; DEEP_NAVY = dark text on bright pills.
        private static readonly Color CREAM     = new Color(1.00f, 0.97f, 0.85f, 1f);  // light text
        private static readonly Color GOLD      = new Color(1.00f, 0.80f, 0.30f, 1f);  // gold accent
        private static readonly Color DEEP_NAVY = new Color(0.12f, 0.10f, 0.16f, 1f);  // dark text for bright buttons
        private static readonly Color CARD_BG   = new Color(0.17f, 0.17f, 0.21f, 1f);  // charcoal card
        private static readonly Color ROW_BG    = new Color(0.26f, 0.26f, 0.32f, 1f);  // slate row
        private static readonly Color ROW_HI    = new Color(0.42f, 0.36f, 0.20f, 1f);  // gold-tinted highlight row
        private static readonly Color WOOD_BAR  = new Color(0.42f, 0.30f, 0.62f, 1f);  // purple title banner
        private static readonly Color WOOD_DARK = new Color(1.00f, 0.80f, 0.30f, 1f);  // gold section headers
        private static readonly Color BUBBLE_BORDER = new Color(0.10f, 0.10f, 0.13f, 1f);
        // Light secondary/status text on the dark rows (brightened for legibility).
        private static readonly Color SUBTEXT   = new Color(0.92f, 0.93f, 0.98f, 1f);
        // Light parchment for sub-modals (Add Friend / chat thread / rank card)
        // which were authored with dark text — keeps them readable as light
        // dialogs over the dark main panel.
        private static readonly Color MODAL_BG  = new Color(0.97f, 0.93f, 0.82f, 1f);

        public static void Show()
        {
            // Tear down any prior root
            var prev = GameObject.Find("WorldRoot");
            if (prev != null) Object.Destroy(prev);

            // TOP-LEVEL canvas at scene root — independent of UI Canvas so it
            // can't be hidden behind any home-screen UI chrome.
            var root = new GameObject("WorldRoot",
                typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(GraphicRaycaster));
            // No parent — scene root.
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var oc = root.GetComponent<Canvas>();
            oc.renderMode = RenderMode.ScreenSpaceOverlay;
            // Dynamic sort ABOVE every other canvas (was a fixed 9999, which
            // could render behind lobby/HUD canvases sitting at >= 9999 — that
            // made the panel "open" per the logs yet show nothing on screen).
            int maxSort = 15000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            oc.sortingOrder = maxSort + 20;

            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // Dim
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0,0,0,0.85f);
            var dimBtn = dim.GetComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => Object.Destroy(root));

            // Card with proper fantasy popup frame
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(1015, 1715);   // near-fullscreen card → max room for readable fonts

            // Warm cream page background — Layer Lab fantasy popup box (sliced),
            // tinted to the cream theme. Falls back to a rounded rect if the
            // sprite is missing.
            var bgImg = card.GetComponent<Image>();
            var popupBg = LoadLL(LL_POPUP_BG);
            if (popupBg != null) { bgImg.sprite = popupBg; bgImg.type = Image.Type.Sliced; bgImg.color = CARD_BG; }
            else { bgImg.sprite = LoadRoundedSprite(28); bgImg.type = Image.Type.Sliced; bgImg.color = CARD_BG; }

            // Ornate border overlay — the fantasy frame (corners + gilt edge).
            var border = new GameObject("FrameBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(card.transform, false);
            var bordRT = border.GetComponent<RectTransform>();
            bordRT.anchorMin = Vector2.zero; bordRT.anchorMax = Vector2.one;
            bordRT.offsetMin = new Vector2(-14, -14); bordRT.offsetMax = new Vector2(14, 14);
            var bordImg = border.GetComponent<Image>();
            var bordSp = LoadLL(LL_POPUP_BORDER);
            if (bordSp != null) { bordImg.sprite = bordSp; bordImg.type = Image.Type.Sliced; bordImg.color = Color.white; }
            else bordImg.color = new Color(0, 0, 0, 0);
            bordImg.raycastTarget = false;

            // ── Wood header bar across the top — inset from the edges so it
            //     doesn't sit flush against the screen / status bar ──
            var headerBar = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            headerBar.transform.SetParent(card.transform, false);
            var hbrt = headerBar.GetComponent<RectTransform>();
            hbrt.anchorMin = new Vector2(0, 1); hbrt.anchorMax = new Vector2(1, 1);
            hbrt.pivot = new Vector2(0.5f, 1f);
            hbrt.anchoredPosition = new Vector2(0, -90);
            hbrt.sizeDelta = new Vector2(-36, 110);
            var hbi = headerBar.GetComponent<Image>();
            hbi.sprite = LoadRoundedSprite(28);
            hbi.type = Image.Type.Sliced;
            hbi.color = WOOD_BAR;
            hbi.raycastTarget = false;

            // Header title text on the wood bar
            var header = MakeText(headerBar.transform, "Header", "WORLD",
                62, FontStyles.Bold, new Color(1f, 0.97f, 0.85f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
                Vector2.zero);
            header.alignment = TextAlignmentOptions.Center;
            header.outlineWidth = 0.30f;
            header.outlineColor = new Color(0.30f, 0.10f, 0.04f);

            // Close button — bold RED circle with a white X (universal "exit"),
            // bigger so it's an obvious way back to the lobby (the near-fullscreen
            // card leaves almost no dim to tap).
            var closeBtn = MakeBtn(headerBar.transform, "Close", "X",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-22, 0), new Vector2(86, 86),
                new Color(0.88f, 0.26f, 0.28f, 1f), Color.white, 48);
            var clImg = closeBtn.GetComponent<Image>();
            clImg.sprite = LoadRoundedSprite(40);
            clImg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => Object.Destroy(root));

            // BACK pill on the LEFT of the header — an explicit, labelled way
            // back to the main menu for players who don't read the X as "exit".
            var backBtn = MakeBtn(headerBar.transform, "Back", "‹ Back",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(70, 0), new Vector2(150, 64),
                new Color(0.30f, 0.20f, 0.45f, 1f), CREAM, 28);
            var bkImg = backBtn.GetComponent<Image>();
            bkImg.sprite = LoadRoundedSprite(16);
            bkImg.type = Image.Type.Sliced;
            backBtn.onClick.AddListener(() => Object.Destroy(root));

            // (Currency pills + scroll icon removed — keep the header clean.)

            // Tabs row — sits below the wood header
            var tabsRow = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsRow.transform.SetParent(card.transform, false);
            var trt = tabsRow.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -230);
            trt.sizeDelta = new Vector2(-72, 76);
            var thlg = tabsRow.GetComponent<HorizontalLayoutGroup>();
            thlg.padding = new RectOffset(0, 0, 0, 0);
            thlg.spacing = 10;
            thlg.childAlignment = TextAnchor.MiddleCenter;
            thlg.childForceExpandWidth = true;
            thlg.childForceExpandHeight = true;

            // ── Channel selector strip — between tabs and content ──
            var chanStrip = new GameObject("ChanStrip", typeof(RectTransform), typeof(Image), typeof(Button));
            chanStrip.transform.SetParent(card.transform, false);
            var csrt = chanStrip.GetComponent<RectTransform>();
            csrt.anchorMin = new Vector2(0, 1); csrt.anchorMax = new Vector2(1, 1);
            csrt.pivot = new Vector2(0.5f, 1);
            csrt.anchoredPosition = new Vector2(0, -320);
            csrt.sizeDelta = new Vector2(-100, 50);
            var csImg = chanStrip.GetComponent<Image>();
            csImg.sprite = LoadRoundedSprite(14);
            csImg.type = Image.Type.Sliced;
            // Solid dark strip so the cream text pops (was a muddy translucent
            // gold that washed the text out).
            csImg.color = new Color(0.10f, 0.10f, 0.13f, 1f);
            var chanTxt = MakeText(chanStrip.transform, "L", "Channel: World · English-1   ·   1,284 online",
                26, FontStyles.Bold, CREAM,
                Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, 0));
            chanTxt.alignment = TextAlignmentOptions.MidlineLeft;
            try { chanTxt.outlineWidth = 0.2f; chanTxt.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}

            // Content area — below the channel strip
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(card.transform, false);
            var conRT = content.GetComponent<RectTransform>();
            conRT.anchorMin = new Vector2(0, 1); conRT.anchorMax = new Vector2(1, 1);
            conRT.pivot = new Vector2(0.5f, 1);
            conRT.anchoredPosition = new Vector2(0, -380);
            conRT.sizeDelta = new Vector2(-56, 1140);   // taller content for the near-fullscreen card

            // Five section roots, only one visible at a time
            var friendsRoot   = BuildFriendsTab(content.transform);
            var guildChatRoot = BuildGuildChatTab(content.transform);
            var worldChatRoot = BuildWorldChatTab(content.transform);
            var guildsRoot    = BuildGuildsTab(content.transform);
            var leaderRoot    = BuildLeaderTab(content.transform);

            // Tab buttons — each gets a distinct color + small icon, like the
            // hexagonal tab pills in the reference (Chat blue / Clan red /
            // Ranking gold / Profile purple).
            Button bFriends   = MakeTabBtn(tabsRow.transform, "FriendsTab",   "Friends",  "HeartFull.png",
                new Color(0.40f, 0.78f, 0.95f, 1f), new Color(0.25f, 0.55f, 0.78f, 1f));
            Button bGuildChat = MakeTabBtn(tabsRow.transform, "GuildChatTab", "Clan",     "SwordT1.png",
                new Color(0.92f, 0.45f, 0.45f, 1f), new Color(0.70f, 0.22f, 0.22f, 1f));
            Button bWorldChat = MakeTabBtn(tabsRow.transform, "WorldChatTab", "World",    "Map.png",
                new Color(0.55f, 0.85f, 0.55f, 1f), new Color(0.30f, 0.62f, 0.30f, 1f));
            Button bGuilds    = MakeTabBtn(tabsRow.transform, "GuildsTab",    "Browse",   "Scroll.png",
                new Color(0.65f, 0.55f, 0.95f, 1f), new Color(0.40f, 0.30f, 0.70f, 1f));
            Button bLeader    = MakeTabBtn(tabsRow.transform, "LeaderTab",    "Heroes",   "HelmetT2.png",
                new Color(0.98f, 0.78f, 0.30f, 1f), new Color(0.78f, 0.50f, 0.10f, 1f));

            void Switch(int idx)
            {
                friendsRoot.SetActive(idx == 0);
                guildChatRoot.SetActive(idx == 1);
                worldChatRoot.SetActive(idx == 2);
                guildsRoot.SetActive(idx == 3);
                leaderRoot.SetActive(idx == 4);
                StyleTab(bFriends,    idx == 0);
                StyleTab(bGuildChat,  idx == 1);
                StyleTab(bWorldChat,  idx == 2);
                StyleTab(bGuilds,     idx == 3);
                StyleTab(bLeader,     idx == 4);
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            }
            bFriends.onClick.AddListener(() => Switch(0));
            bGuildChat.onClick.AddListener(() => Switch(1));
            bWorldChat.onClick.AddListener(() => Switch(2));
            bGuilds.onClick.AddListener(() => Switch(3));
            bLeader.onClick.AddListener(() => Switch(4));
            Switch(0);

            // ── Bottom quick-action strip: 4 colored cards ──
            BuildBottomCards(card.transform);
        }

        // 4 chunky colored quick-action cards at the bottom (Mail / Poll / Gift / Vote)
        private static void BuildBottomCards(Transform card)
        {
            var row = new GameObject("BottomCards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(card, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 0); rrt.anchorMax = new Vector2(1, 0);
            rrt.pivot = new Vector2(0.5f, 0);
            rrt.anchoredPosition = new Vector2(0, 24);
            rrt.sizeDelta = new Vector2(-40, 130);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            BuildQuickCard(row.transform, "Mail",  "Letter.png",   "Mail",
                new Color(0.55f, 0.78f, 0.95f), new Color(0.25f, 0.50f, 0.78f),
                "3 new letters",        new Color(0.40f, 0.78f, 0.95f));
            BuildQuickCard(row.transform, "Poll",  "TomeBlue.png", "Poll",
                new Color(0.95f, 0.85f, 0.40f), new Color(0.78f, 0.55f, 0.10f),
                "Cast your vote!",      GOLD);
            BuildQuickCard(row.transform, "Gift",  "Backpack.png", "Gift",
                new Color(0.95f, 0.55f, 0.55f), new Color(0.78f, 0.25f, 0.25f),
                "Daily gift opened",    new Color(0.95f, 0.55f, 0.55f));
            BuildQuickCard(row.transform, "Vote",  "ShieldSmallT1.png", "Vote",
                new Color(0.65f, 0.85f, 0.55f), new Color(0.30f, 0.62f, 0.30f),
                "Battle vote logged",   new Color(0.55f, 0.85f, 0.55f));
        }

        private static void BuildQuickCard(Transform parent, string name, string glyph, string label,
            Color top, Color shadow, string toast = "", Color toastColor = default)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
            card.transform.SetParent(parent, false);
            var hit = card.GetComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;

            // Shadow
            var sh = new GameObject("Sh", typeof(RectTransform), typeof(Image));
            sh.transform.SetParent(card.transform, false);
            var srt = sh.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(0, -6); srt.offsetMax = new Vector2(0, 0);
            var shImg = sh.GetComponent<Image>();
            shImg.sprite = LoadRoundedSprite(18);
            shImg.type = Image.Type.Sliced;
            shImg.color = shadow;
            shImg.raycastTarget = false;

            // Top
            var pill = new GameObject("Pill", typeof(RectTransform), typeof(Image));
            pill.transform.SetParent(card.transform, false);
            var prt = pill.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(0, 4); prt.offsetMax = new Vector2(0, 4);
            var pImg = pill.GetComponent<Image>();
            pImg.sprite = LoadRoundedSprite(18);
            pImg.type = Image.Type.Sliced;
            pImg.color = top;
            pImg.raycastTarget = false;

            // Glyph — try sprite first, fall back to text glyph
            Sprite cardIcon = (glyph != null && glyph.EndsWith(".png"))
                ? LoadFantasyIcon(glyph) : null;
            if (cardIcon != null)
            {
                var icGO = new GameObject("G", typeof(RectTransform), typeof(Image));
                icGO.transform.SetParent(pill.transform, false);
                var icrt = icGO.GetComponent<RectTransform>();
                icrt.anchorMin = new Vector2(0, 0.4f); icrt.anchorMax = new Vector2(1, 1);
                icrt.offsetMin = new Vector2(8, 4); icrt.offsetMax = new Vector2(-8, -4);
                var icImg = icGO.GetComponent<Image>();
                icImg.sprite = cardIcon;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
            }
            else
            {
                var g = MakeText(pill.transform, "G", glyph,
                    40, FontStyles.Bold, new Color(0.18f, 0.08f, 0.18f),
                    new Vector2(0, 0.4f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                g.alignment = TextAlignmentOptions.Center;
                g.outlineWidth = 0.22f;
                g.outlineColor = new Color(1f, 0.97f, 0.85f, 0.95f);
            }

            // Label — dark ink with cream halo
            var l = MakeText(pill.transform, "L", label,
                32, FontStyles.Bold, new Color(0.18f, 0.08f, 0.18f),
                new Vector2(0, 0), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            l.alignment = TextAlignmentOptions.Center;
            l.outlineWidth = 0.22f;
            l.outlineColor = new Color(1f, 0.97f, 0.85f, 0.95f);

            // Click → spawn a floater toast above the card
            string toastMsg = string.IsNullOrEmpty(toast) ? label : toast;
            Color  toastCol = toastColor.a > 0 ? toastColor : top;
            card.GetComponent<Button>().onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                try
                {
                    var canvas = card.GetComponentInParent<Canvas>();
                    if (canvas != null)
                        XPFloater.Spawn(canvas.transform,
                            card.transform.position + new Vector3(0, 80, 0),
                            toastMsg, toastCol);
                } catch {}
            });
        }

        // ───────── AI Translator ─────────
        private static bool _aiOn = true;

        // Full-width AI Auto-Translate toggle bar — added to every chat surface
        // (World chat, Clan chat, and the friend DM thread) so the translator
        // is available on ALL chats, not just per-message buttons.
        private static void BuildChatTranslatorBar(Transform parent, float yPos)
        {
            var bar = new GameObject("AITranslateBar", typeof(RectTransform), typeof(Image), typeof(Button));
            bar.transform.SetParent(parent, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, yPos);
            rt.sizeDelta = new Vector2(-16, 48);
            var img = bar.GetComponent<Image>();
            var btnSp = LoadLL(LL_BTN_CONVEX);
            if (btnSp != null) { img.sprite = btnSp; img.type = Image.Type.Sliced; }

            void Paint(Image i, TMP_Text t)
            {
                i.color = _aiOn ? new Color(0.30f, 0.62f, 0.42f, 1f) : new Color(0.30f, 0.30f, 0.36f, 1f);
                t.text  = _aiOn ? "AI Auto-Translate:  ON" : "AI Auto-Translate:  OFF";
                t.color = CREAM;
            }

            var lbl = MakeText(bar.transform, "Lbl", "", 24, FontStyles.Bold, CREAM,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            lbl.alignment = TextAlignmentOptions.Center;
            try { lbl.outlineWidth = 0.2f; lbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Paint(img, lbl);

            bar.GetComponent<Button>().onClick.AddListener(() =>
            {
                _aiOn = !_aiOn;
                Paint(img, lbl);
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            });
        }

        // ───────── Mock pending requests ─────────
        private struct PendingReq { public string name; public Color tint; public int level; }
        private static readonly System.Collections.Generic.List<PendingReq> PENDING =
            new System.Collections.Generic.List<PendingReq>
            {
                new PendingReq { name = "Finn",  tint = new Color(0.95f, 0.65f, 0.30f), level = 3 },
                new PendingReq { name = "Mira",  tint = new Color(0.55f, 0.85f, 0.65f), level = 7 },
            };

        // ───────── Tab content builders ─────────
        private static GameObject BuildFriendsTab(Transform parent)
        {
            var root = MakeTabRoot(parent, "FriendsContent");
            var list = MakeScrollList(root.transform);

            // "+ Add Friend" button at top
            BuildAddFriendButton(list, root);

            // Pending requests section
            if (PENDING.Count > 0)
            {
                BuildSectionHeader(list, $"Pending Requests ({PENDING.Count})");
                foreach (var p in PENDING) BuildPendingRow(list, p);
            }

            // Friends section
            BuildSectionHeader(list, "Friends");
            foreach (var f in FRIENDS) BuildFriendRow(list, f);
            return root;
        }

        private static void BuildSectionHeader(Transform parent, string title)
        {
            var hdr = new GameObject("SectionHdr", typeof(RectTransform), typeof(LayoutElement));
            hdr.transform.SetParent(parent, false);
            hdr.GetComponent<LayoutElement>().preferredHeight = 58;
            var tm = hdr.AddComponent<TextMeshProUGUI>();
            tm.text = title;
            tm.fontSize = 34;
            tm.fontStyle = FontStyles.Bold;
            tm.color = WOOD_DARK;                   // strong brown on cream
            tm.alignment = TextAlignmentOptions.MidlineLeft;
            tm.margin = new Vector4(12, 0, 12, 0);
        }

        private static void BuildAddFriendButton(Transform parent, GameObject tabRoot)
        {
            var row = new GameObject("AddFriend",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 92;
            row.GetComponent<Image>().color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.9f);

            MakeText(row.transform, "Lbl", "+ Add Friend",
                36, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            row.GetComponent<Button>().onClick.AddListener(() => ShowAddFriendModal(tabRoot));
        }

        private static void BuildPendingRow(Transform parent, PendingReq p)
        {
            var row = new GameObject("Pending_" + p.name,
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 148;
            StyleRow(row, ROW_HI);   // gold-tinted dark pending row

            var pAv = MakeCircleAvatar(row.transform, p.name.Substring(0, 1), p.tint);
            AddLevelBadge(pAv.transform, p.level);

            MakeText(row.transform, "Name", $"{p.name}  ·  Lv.{p.level}",
                36, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(134, -24), new Vector2(-268, 50))
                .alignment = TextAlignmentOptions.MidlineLeft;

            MakeText(row.transform, "Hint", "wants to be your friend",
                25, FontStyles.Italic, SUBTEXT,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(134, -82), new Vector2(-268, 36))
                .alignment = TextAlignmentOptions.MidlineLeft;

            // Accept (green) — white text for strong contrast on green.
            var accept = MakeBtn(row.transform, "Accept", "Accept",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-132, 0), new Vector2(124, 72),
                new Color(0.30f, 0.72f, 0.38f), Color.white, 28);
            accept.onClick.AddListener(() => RemovePendingAndDestroy(p, row));

            // Decline (red)
            var decline = MakeBtn(row.transform, "Decline", "Decline",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-8, 0), new Vector2(118, 72),
                new Color(0.85f, 0.40f, 0.45f), Color.white, 28);
            decline.onClick.AddListener(() => RemovePendingAndDestroy(p, row));
        }

        private static void RemovePendingAndDestroy(PendingReq p, GameObject row)
        {
            PENDING.RemoveAll(x => x.name == p.name);
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            Object.Destroy(row);
        }

        // ───────── Add Friend modal ─────────
        private static void ShowAddFriendModal(GameObject tabRoot)
        {
            var canvas = tabRoot.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var modal = new GameObject("AddFriendModal", typeof(RectTransform), typeof(Image), typeof(Button));
            modal.transform.SetParent(canvas.transform, false);
            var mrt = modal.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            modal.transform.SetAsLastSibling();

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(modal.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(540, 320);
            card.GetComponent<Image>().color = MODAL_BG;

            MakeText(card.transform, "Title", "Add Friend", 30, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -28), new Vector2(0, 36))
                .alignment = TextAlignmentOptions.Center;

            MakeText(card.transform, "Hint", "Search by username or scan their hero code",
                16, FontStyles.Italic, new Color(0.55f, 0.32f, 0.18f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 22))
                .alignment = TextAlignmentOptions.Center;

            // Username input field (mock — looks like input)
            var input = new GameObject("Input", typeof(RectTransform), typeof(Image));
            input.transform.SetParent(card.transform, false);
            var irt = input.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0, 24);
            irt.sizeDelta = new Vector2(440, 56);
            var addInImg = input.GetComponent<Image>();
            addInImg.sprite = LoadRoundedSprite(12); addInImg.type = Image.Type.Sliced;
            addInImg.color = new Color(1f, 0.84f, 0.50f, 1f);
            MakeText(input.transform, "Placeholder", "username...",
                20, FontStyles.Italic, new Color(0.55f, 0.32f, 0.18f, 0.85f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Send Request button
            var send = MakeBtn(card.transform, "Send", "Send Request",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-110, 50), new Vector2(200, 56),
                GOLD, DEEP_NAVY, 18);
            send.onClick.AddListener(() =>
            {
                try
                {
                    if (canvas != null)
                        XPFloater.Spawn(canvas.transform,
                            modal.transform.position + new Vector3(0, 80, 0),
                            "Friend request sent",
                            new Color(0.55f, 0.85f, 0.45f));
                } catch {}
                Object.Destroy(modal);
            });

            // Cancel
            var cancel = MakeBtn(card.transform, "Cancel", "Cancel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(110, 50), new Vector2(200, 56),
                new Color(0.4f, 0.4f, 0.5f), CREAM, 18);
            cancel.onClick.AddListener(() => Object.Destroy(modal));

            // Close on dim tap
            modal.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(modal));
        }

        // ───────── Guild Chat (your guild's room) ─────────
        private struct ChatMsg { public string author; public string text; public bool fromMe; public Color tint; }

        private static readonly ChatMsg[] GUILD_CHAT = new[]
        {
            new ChatMsg { author = "Aria", text = "anyone running the trial today?",   fromMe = false, tint = new Color(0.85f,0.40f,0.50f) },
            new ChatMsg { author = "Bram", text = "i'm in. need 1 more for the boss",  fromMe = false, tint = new Color(0.55f,0.85f,0.45f) },
            new ChatMsg { author = "Karu", text = "count me in — gimme 5 min",         fromMe = true,  tint = GOLD },
            new ChatMsg { author = "Dax",  text = "👀 finally hit lv.9!!",              fromMe = false, tint = new Color(1f,0.65f,0.30f) },
            new ChatMsg { author = "Aria", text = "let's go!! meeting at the portal",  fromMe = false, tint = new Color(0.85f,0.40f,0.50f) },
        };

        private static readonly ChatMsg[] WORLD_CHAT = new[]
        {
            new ChatMsg { author = "WyldOne", text = "anyone tried the phoenix trial?",   fromMe = false, tint = new Color(1f,0.55f,0.30f) },
            new ChatMsg { author = "Sera",    text = "it's brutal but the loot is wild",  fromMe = false, tint = new Color(0.65f,0.55f,0.85f) },
            new ChatMsg { author = "Karu",    text = "any tips for solo runs?",          fromMe = true,  tint = GOLD },
            new ChatMsg { author = "Tobi",    text = "stack DEF gear before facing boss", fromMe = false, tint = new Color(0.45f,0.85f,0.65f) },
            new ChatMsg { author = "Mira",    text = "join Phoenix Coven, we run dailies",fromMe = false, tint = new Color(0.55f,0.85f,0.65f) },
            new ChatMsg { author = "Niko",    text = "anyone speak Spanish? AI translate works great here", fromMe = false, tint = new Color(0.55f,0.75f,1f) },
        };

        private static GameObject BuildGuildChatTab(Transform parent)
            => BuildChatTab(parent, "GuildChatContent", "Quiet Forge · 12 online", GUILD_CHAT);

        private static GameObject BuildWorldChatTab(Transform parent)
            => BuildChatTab(parent, "WorldChatContent", "World · 1,284 online · AI translate per msg", WORLD_CHAT);

        private static GameObject BuildChatTab(Transform parent, string name, string subtitle, ChatMsg[] messages)
        {
            var root = MakeTabRoot(parent, name);

            // Subtitle strip at top
            var sub = new GameObject("Sub", typeof(RectTransform), typeof(Image));
            sub.transform.SetParent(root.transform, false);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, 0);
            srt.sizeDelta = new Vector2(0, 46);
            sub.GetComponent<Image>().color = new Color(WOOD_DARK.r, WOOD_DARK.g, WOOD_DARK.b, 0.85f);
            MakeText(sub.transform, "L", subtitle,
                20, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // AI Auto-Translate toggle bar — every chat gets one.
            BuildChatTranslatorBar(root.transform, -52);

            // Message list — proper ScrollRect so long histories scroll
            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(root.transform, false);
            var srt2 = scroll.GetComponent<RectTransform>();
            srt2.anchorMin = Vector2.zero; srt2.anchorMax = Vector2.one;
            srt2.offsetMin = new Vector2(8, 76); srt2.offsetMax = new Vector2(-8, -110);
            var srcomp = scroll.GetComponent<ScrollRect>();
            srcomp.horizontal = false; srcomp.vertical = true;
            srcomp.movementType = ScrollRect.MovementType.Elastic;
            srcomp.scrollSensitivity = 35f;

            var vp = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(scroll.transform, false);
            var vprt = vp.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero; vprt.anchorMax = Vector2.one;
            vprt.offsetMin = Vector2.zero; vprt.offsetMax = Vector2.zero;
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            vp.GetComponent<Mask>().showMaskGraphic = false;
            srcomp.viewport = vprt;

            var list = new GameObject("List",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(vp.transform, false);
            var lrt = list.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = Vector2.zero;
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            srcomp.content = lrt;

            foreach (var m in messages) BuildChatMsgRow(list.transform, m);

            // Input bar — warm cream pill with brown placeholder, brown send button
            var input = new GameObject("Input", typeof(RectTransform), typeof(Image));
            input.transform.SetParent(root.transform, false);
            var irt = input.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0); irt.anchorMax = new Vector2(1, 0);
            irt.pivot = new Vector2(0.5f, 0);
            irt.anchoredPosition = new Vector2(0, 8);
            irt.sizeDelta = new Vector2(-16, 60);
            var inImg = input.GetComponent<Image>();
            inImg.sprite = LoadRoundedSprite(14);
            inImg.type = Image.Type.Sliced;
            inImg.color = new Color(1f, 0.84f, 0.50f, 1f);       // honey input
            MakeText(input.transform, "P", "Tap to enter...",
                22, FontStyles.Italic, new Color(0.55f, 0.32f, 0.18f, 0.9f),
                new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(20, 0), new Vector2(-120, 36))
                .alignment = TextAlignmentOptions.MidlineLeft;
            var send = MakeBtn(input.transform, "Send", "Send",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-12, 0), new Vector2(100, 50),
                GOLD, DEEP_NAVY, 22);
            var sndImg = send.GetComponent<Image>();
            sndImg.sprite = LoadRoundedSprite(12); sndImg.type = Image.Type.Sliced;
            send.onClick.AddListener(() =>
            {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            });

            return root;
        }

        private static void BuildChatMsgRow(Transform parent, ChatMsg m)
        {
            var row = new GameObject("Msg_" + m.author,
                typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 88;

            // Avatar (left for others, right for me)
            var av = new GameObject("Av", typeof(RectTransform), typeof(Image));
            av.transform.SetParent(row.transform, false);
            var art = av.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(m.fromMe ? 1 : 0, 0.5f);
            art.anchorMax = new Vector2(m.fromMe ? 1 : 0, 0.5f);
            art.pivot     = new Vector2(m.fromMe ? 1 : 0, 0.5f);
            art.anchoredPosition = new Vector2(m.fromMe ? -8 : 8, 0);
            art.sizeDelta = new Vector2(60, 60);
            var avImg = av.GetComponent<Image>();
            avImg.sprite = LoadCircleSprite();
            avImg.color = m.tint;

            MakeText(av.transform, "L", m.author.Substring(0, 1),
                28, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Bubble — warm cream with brown border, dark-brown ink text
            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(row.transform, false);
            var brt = bubble.GetComponent<RectTransform>();
            float bubbleW = Mathf.Min(540f, 100f + m.text.Length * 11f);
            brt.anchorMin = new Vector2(m.fromMe ? 1 : 0, 0.5f);
            brt.anchorMax = new Vector2(m.fromMe ? 1 : 0, 0.5f);
            brt.pivot     = new Vector2(m.fromMe ? 1 : 0, 0.5f);
            brt.anchoredPosition = new Vector2(m.fromMe ? -78 : 78, 0);
            brt.sizeDelta = new Vector2(bubbleW, 70);
            var bubbleImg = bubble.GetComponent<Image>();
            bubbleImg.sprite = LoadRoundedSprite(14);
            bubbleImg.type = Image.Type.Sliced;
            // Light bubbles (dark ink reads on them) — "me" = honey, others = parchment.
            bubbleImg.color = m.fromMe ? new Color(1f, 0.86f, 0.56f, 1f) : MODAL_BG;

            // Soft brown border ring (rendered behind bubble fill)
            var bord = new GameObject("Border", typeof(RectTransform), typeof(Image));
            bord.transform.SetParent(bubble.transform, false);
            var bordRt = bord.GetComponent<RectTransform>();
            bordRt.anchorMin = Vector2.zero; bordRt.anchorMax = Vector2.one;
            bordRt.offsetMin = new Vector2(-2, -2); bordRt.offsetMax = new Vector2(2, 2);
            var bordImg = bord.GetComponent<Image>();
            bordImg.sprite = LoadRoundedSprite(15);
            bordImg.type = Image.Type.Sliced;
            bordImg.color = BUBBLE_BORDER;
            bordImg.raycastTarget = false;
            bord.transform.SetAsFirstSibling();

            MakeText(bubble.transform, "Author", m.author,
                17, FontStyles.Bold,
                m.fromMe ? new Color(0.40f, 0.18f, 0.04f) : new Color(0.55f, 0.30f, 0.10f),
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -2), new Vector2(-12, 18))
                .alignment = TextAlignmentOptions.MidlineLeft;

            var body = MakeText(bubble.transform, "Body", m.text,
                23, FontStyles.Bold, new Color(0.30f, 0.16f, 0.06f),  // dark brown ink on light bubble
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0, -8), new Vector2(-50, 40));
            body.alignment = TextAlignmentOptions.MidlineLeft;
            body.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Per-message AI translate button (small)
            string original = m.text;
            string translated = "[translated] " + m.text;
            bool[] state = { false };
            var trBtn = MakeBtn(bubble.transform, "AI", "AI",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-4, 4), new Vector2(36, 24),
                new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f), DEEP_NAVY, 11);
            trBtn.onClick.AddListener(() =>
            {
                state[0] = !state[0];
                body.text = state[0] ? translated : original;
                var trImg = trBtn.GetComponent<Image>();
                trImg.color = state[0]
                    ? new Color(0.55f, 0.85f, 0.45f, 0.95f)
                    : new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f);
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            });
        }

        private static GameObject BuildLeaderTab(Transform parent)
        {
            var root = MakeTabRoot(parent, "LeaderContent");

            // ── Hero showcase at the top — player's character card ──
            BuildHeroShowcase(root.transform);

            // Section divider + leaderboard underneath
            var divider = MakeText(root.transform, "DivLbl", "·  RANKINGS  ·",
                26, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -272), new Vector2(0, 34));
            divider.alignment = TextAlignmentOptions.Center;
            divider.characterSpacing = 8f;

            // List sits below the showcase
            var listGO = new GameObject("List",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGO.transform.SetParent(root.transform, false);
            var lrt = listGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(8, 8); lrt.offsetMax = new Vector2(-8, -310);
            var vlg = listGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            foreach (var r in LEADER) BuildLeaderRow(listGO.transform, r);
            return root;
        }

        // Big hero card at the top of the Heroes tab — shows the player's
        // character (Karu by default) with a portrait, level, name and stats.
        private static void BuildHeroShowcase(Transform parent)
        {
            // Card container
            var card = new GameObject("HeroCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = new Vector2(0, 0);
            crt.sizeDelta = new Vector2(-12, 250);
            var cImg = card.GetComponent<Image>();
            cImg.sprite = LoadRoundedSprite(20);
            cImg.type = Image.Type.Sliced;
            cImg.color = new Color(1f, 0.84f, 0.50f, 1f);
            // Border ring
            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(card.transform, false);
            var rrt = ring.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(-3, -3); rrt.offsetMax = new Vector2(3, 3);
            var rImg = ring.GetComponent<Image>();
            rImg.sprite = LoadRoundedSprite(22);
            rImg.type = Image.Type.Sliced;
            rImg.color = new Color(0.78f, 0.50f, 0.10f);
            rImg.raycastTarget = false;
            ring.transform.SetAsFirstSibling();

            // Portrait disc on the LEFT — try to load Karu's hero art
            var pFrame = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            pFrame.transform.SetParent(card.transform, false);
            var prt = pFrame.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0.5f); prt.anchorMax = new Vector2(0, 0.5f);
            prt.pivot = new Vector2(0, 0.5f);
            prt.anchoredPosition = new Vector2(20, 0);
            prt.sizeDelta = new Vector2(180, 180);
            var pImg = pFrame.GetComponent<Image>();
            pImg.sprite = LoadCircleSprite();
            pImg.color = GOLD;
            pImg.raycastTarget = false;
            // Inner color disc + character image
            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(pFrame.transform, false);
            var iRT = inner.GetComponent<RectTransform>();
            iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
            iRT.offsetMin = new Vector2(8, 8); iRT.offsetMax = new Vector2(-8, -8);
            var iImg = inner.GetComponent<Image>();
            iImg.sprite = LoadCircleSprite();
            iImg.color = new Color(0.30f, 0.18f, 0.42f);
            iImg.raycastTarget = false;
            // Try a real hero sprite — falls back to the initial letter
            Sprite heroSprite = LoadSprite("Assets/Art/Sparq/una-mage.png");
            if (heroSprite != null)
            {
                var hero = new GameObject("Hero", typeof(RectTransform), typeof(Image));
                hero.transform.SetParent(pFrame.transform, false);
                var hrt = hero.GetComponent<RectTransform>();
                hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
                hrt.offsetMin = new Vector2(14, 14); hrt.offsetMax = new Vector2(-14, -14);
                var himg = hero.GetComponent<Image>();
                himg.sprite = heroSprite;
                himg.preserveAspect = true;
                himg.raycastTarget = false;
            }
            else
            {
                MakeText(pFrame.transform, "K", "K",
                    100, FontStyles.Bold, new Color(1f, 0.97f, 0.85f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                    .alignment = TextAlignmentOptions.Center;
            }

            // Name + level badge
            var nameText = MakeText(card.transform, "Name", "Karu",
                42, FontStyles.Bold, new Color(0.30f, 0.16f, 0.06f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(110, -22),
                new Vector2(-220, 50));
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.outlineWidth = 0.20f;
            nameText.outlineColor = new Color(1f, 0.97f, 0.85f, 0.8f);

            // Try to read the player's actual save name
            try
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null && !string.IsNullOrEmpty(data.playerName))
                    nameText.text = data.playerName;
            } catch {}

            // Subtitle: title / class — darkened + bigger for contrast on gold.
            MakeText(card.transform, "Title", "✦  Mystic Adept  ✦",
                27, FontStyles.Bold, new Color(0.26f, 0.13f, 0.03f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(110, -72),
                new Vector2(-220, 34))
                .alignment = TextAlignmentOptions.MidlineLeft;

            // Three stat tiles in the bottom-right of the card
            int playerLevel = 1, playerXP = 0, playerHp = 100;
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null)
                {
                    playerLevel = Mathf.Max(1, d.level);
                    playerXP = Mathf.Max(0, d.totalXP);
                    playerHp = 100 + d.level * 12;
                }
            } catch {}
            // Stat tiles — moved further right (away from portrait) and using
            // deeply saturated colors so they don't blend with the honey card.
            BuildStatTile(card.transform, "♥",  $"{playerHp}",   "HP",
                new Vector2(280, -110), new Color(0.85f, 0.20f, 0.32f), new Color(0.55f, 0.08f, 0.15f));
            BuildStatTile(card.transform, "✦",  $"Lv {playerLevel}", "LVL",
                new Vector2(428, -110), new Color(0.55f, 0.30f, 0.78f), new Color(0.30f, 0.12f, 0.50f));
            BuildStatTile(card.transform, "★",  $"{playerXP}",   "XP",
                new Vector2(576, -110), new Color(0.20f, 0.62f, 0.65f), new Color(0.10f, 0.40f, 0.45f));

            // Tagline on the bottom of the card — darker + bigger.
            MakeText(card.transform, "Tag", "“Light my path, sharpen my blade.”",
                22, FontStyles.Italic, new Color(0.24f, 0.12f, 0.03f, 1f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(120, -208),
                new Vector2(-130, 32))
                .alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void BuildStatTile(Transform parent, string glyph, string value, string label,
            Vector2 anchorTopLeft, Color top, Color shadow)
        {
            var tile = new GameObject($"Stat_{label}", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            var rt = tile.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchorTopLeft;
            rt.sizeDelta = new Vector2(140, 72);
            var img = tile.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(14);
            img.type = Image.Type.Sliced;
            img.color = top;
            img.raycastTarget = false;
            // Shadow underneath
            var sh = new GameObject("Sh", typeof(RectTransform), typeof(Image));
            sh.transform.SetParent(tile.transform, false);
            var srt = sh.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(-2, -6); srt.offsetMax = new Vector2(2, -2);
            var shImg = sh.GetComponent<Image>();
            shImg.sprite = LoadRoundedSprite(14); shImg.type = Image.Type.Sliced;
            shImg.color = shadow;
            shImg.raycastTarget = false;
            sh.transform.SetAsFirstSibling();

            // High-contrast text on the saturated tile background
            var g = MakeText(tile.transform, "G", glyph,
                32, FontStyles.Bold, new Color(1f, 0.97f, 0.85f),
                new Vector2(0, 0), new Vector2(0.32f, 1), Vector2.zero, Vector2.zero);
            g.alignment = TextAlignmentOptions.Center;
            g.outlineWidth = 0.30f;
            g.outlineColor = new Color(0.10f, 0.05f, 0.10f, 0.95f);

            var v = MakeText(tile.transform, "V", value,
                30, FontStyles.Bold, new Color(1f, 0.98f, 0.90f),
                new Vector2(0.32f, 0.45f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            v.alignment = TextAlignmentOptions.MidlineLeft;
            v.outlineWidth = 0.30f;
            v.outlineColor = new Color(0.05f, 0.03f, 0.10f, 0.95f);

            var l = MakeText(tile.transform, "L", label,
                19, FontStyles.Bold, new Color(1f, 0.97f, 0.85f),
                new Vector2(0.32f, 0), new Vector2(1, 0.45f), Vector2.zero, Vector2.zero);
            l.alignment = TextAlignmentOptions.MidlineLeft;
            l.outlineWidth = 0.22f;
            l.outlineColor = new Color(0.05f, 0.03f, 0.10f, 0.95f);
        }

        // ───────── Guild tab ─────────
        private struct Guild { public string name; public int members; public int level; public string motto; public Color tint; }
        private static readonly Guild[] GUILDS = new[]
        {
            new Guild { name = "Dawn Patrol",    members = 24, level = 7, motto = "Rise early, slay quietly",   tint = new Color(1f,0.65f,0.30f) },
            new Guild { name = "Quiet Forge",    members = 12, level = 4, motto = "Mind first, blade second",   tint = new Color(0.55f,0.75f,1f) },
            new Guild { name = "Phoenix Coven",  members = 31, level = 9, motto = "From ashes, sharper",        tint = new Color(0.85f,0.30f,0.10f) },
            new Guild { name = "Sapling Circle", members = 8,  level = 2, motto = "Small steps, deep roots",    tint = new Color(0.55f,0.85f,0.45f) },
        };

        private static GameObject BuildGuildsTab(Transform parent)
        {
            var root = MakeTabRoot(parent, "GuildsContent");
            var list = MakeScrollList(root.transform);

            // Create / Search Guild button
            var create = new GameObject("CreateGuild",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            create.transform.SetParent(list, false);
            create.GetComponent<LayoutElement>().preferredHeight = 60;
            create.GetComponent<Image>().color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.9f);
            MakeText(create.transform, "Lbl", "+ Create or Find Guild",
                22, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            create.GetComponent<Button>().onClick.AddListener(() =>
            {
                var canvas = root.GetComponentInParent<Canvas>();
                if (canvas != null)
                    XPFloater.Spawn(canvas.transform,
                        create.transform.position + new Vector3(0, 60, 0),
                        "Guild creator coming soon",
                        GOLD);
            });

            BuildSectionHeader(list, "Active Guilds");
            foreach (var g in GUILDS) BuildGuildRow(list, g);
            return root;
        }

        private static void BuildGuildRow(Transform parent, Guild g)
        {
            var row = new GameObject("Guild_" + g.name,
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 104;
            StyleRow(row, ROW_BG);

            // Crest (square w/ initial)
            var crest = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            crest.transform.SetParent(row.transform, false);
            var crt = crest.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0.5f); crt.anchorMax = new Vector2(0, 0.5f);
            crt.pivot = new Vector2(0, 0.5f);
            crt.anchoredPosition = new Vector2(12, 0);
            crt.sizeDelta = new Vector2(76, 76);
            crest.GetComponent<Image>().color = g.tint;
            MakeText(crest.transform, "C", g.name.Substring(0, 1),
                34, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            MakeText(row.transform, "Name", $"{g.name}  ·  Lv.{g.level}",
                22, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(104, -14), new Vector2(-260, 30))
                .alignment = TextAlignmentOptions.MidlineLeft;

            MakeText(row.transform, "Motto", g.motto,
                17, FontStyles.Italic, new Color(0.55f, 0.32f, 0.18f),
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(104, -46), new Vector2(-260, 22))
                .alignment = TextAlignmentOptions.MidlineLeft;

            MakeText(row.transform, "Members", $"{g.members} members",
                15, FontStyles.Bold, WOOD_DARK,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(104, -72), new Vector2(-260, 18))
                .alignment = TextAlignmentOptions.MidlineLeft;

            // Join button
            var join = MakeBtn(row.transform, "Join", "Join",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-16, 0), new Vector2(96, 52),
                GOLD, DEEP_NAVY, 18);
            string gn = g.name;
            join.onClick.AddListener(() =>
            {
                var canvas = row.GetComponentInParent<Canvas>();
                if (canvas != null)
                    XPFloater.Spawn(canvas.transform,
                        join.transform.position + new Vector3(0, 60, 0),
                        $"Requested to join {gn}",
                        GOLD);
            });
        }

        private static GameObject MakeTabRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        // Real vertical scroll list (ScrollRect + masked viewport + content),
        // the same pattern the chat tabs scroll fine with. Returns the CONTENT
        // transform — callers add their rows to it; the ContentSizeFitter grows
        // it and the ScrollRect scrolls when it exceeds the viewport. Optional
        // insets carve room for a header/input bar around the scroll.
        private static Transform MakeScrollList(Transform parent,
            float insetTop = 0f, float insetBottom = 0f, float insetSide = 0f)
        {
            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(parent, false);
            var srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(insetSide, insetBottom);
            srt.offsetMax = new Vector2(-insetSide, -insetTop);
            var sr = scroll.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 35f;

            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(scroll.transform, false);
            var vprt = vp.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero; vprt.anchorMax = Vector2.one;
            vprt.offsetMin = Vector2.zero; vprt.offsetMax = Vector2.zero;
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            vp.GetComponent<Mask>().showMaskGraphic = false;
            sr.viewport = vprt;

            var list = new GameObject("List",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(vp.transform, false);
            var lrt = list.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = Vector2.zero;
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;     // honor LayoutElement.preferredHeight
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = lrt;
            return list.transform;
        }

        private static void BuildFriendRow(Transform parent, Friend f)
        {
            var row = new GameObject("Friend_" + f.name,
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 148;
            StyleRow(row, ROW_BG);
            string fn = f.name;
            row.GetComponent<Button>().onClick.AddListener(() => ShowChatThread(row, fn));

            // Avatar circle (initial letter) + level badge
            var av = MakeCircleAvatar(row.transform, f.name.Substring(0, 1), f.tint);
            AddLevelBadge(av.transform, f.level);

            // Online dot
            if (f.online)
            {
                var dot = new GameObject("OnlineDot", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(av.transform, false);
                var drt = dot.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(1, 0); drt.anchorMax = new Vector2(1, 0);
                drt.pivot = new Vector2(1, 0);
                drt.anchoredPosition = new Vector2(0, 0);
                drt.sizeDelta = new Vector2(14, 14);
                dot.GetComponent<Image>().color = new Color(0.45f, 0.95f, 0.45f);
            }

            MakeText(row.transform, "Name", $"{f.name}  ·  Lv.{f.level}",
                36, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(134, -24), new Vector2(-232, 50))
                .alignment = TextAlignmentOptions.MidlineLeft;

            MakeText(row.transform, "Status", f.status,
                25, FontStyles.Normal, SUBTEXT,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(134, -86), new Vector2(-232, 36))
                .alignment = TextAlignmentOptions.MidlineLeft;

            // Wave button
            var wave = MakeBtn(row.transform, "Wave", "Wave",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-78, 0), new Vector2(120, 66),
                GOLD, DEEP_NAVY, 28);
            wave.onClick.AddListener(() =>
            {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            });

            // 3-dot security menu
            string fn2 = f.name;
            var menu = MakeBtn(row.transform, "Menu", "...",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-12, 0), new Vector2(48, 66),
                new Color(0.30f, 0.20f, 0.40f), CREAM, 30);
            menu.onClick.AddListener(() => ShowSecurityMenu(row, fn2));
        }

        // ───────── Chat thread modal ─────────
        private static void ShowChatThread(GameObject anchor, string friendName)
        {
            var canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var modal = new GameObject("ChatThread", typeof(RectTransform), typeof(Image), typeof(Button));
            modal.transform.SetParent(canvas.transform, false);
            var mrt = modal.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            modal.transform.SetAsLastSibling();
            modal.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(modal));

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(modal.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(720, 1080);
            card.GetComponent<Image>().color = MODAL_BG;

            // Header (friend name + close)
            MakeText(card.transform, "Hdr", friendName,
                30, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -28), new Vector2(0, 36))
                .alignment = TextAlignmentOptions.Center;
            MakeText(card.transform, "Hint", "End-to-end encrypted · AI translate per message",
                14, FontStyles.Italic, new Color(0.55f, 0.32f, 0.18f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -56), new Vector2(0, 18))
                .alignment = TextAlignmentOptions.Center;
            var close = MakeBtn(card.transform, "Close", "X",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16),
                new Vector2(48, 48), new Color(0.4f, 0.2f, 0.6f), Color.white, 22);
            close.onClick.AddListener(() => Object.Destroy(modal));

            // AI Auto-Translate toggle — the friend DM gets one too.
            BuildChatTranslatorBar(card.transform, -84);

            // Mock message bubbles
            (string text, bool fromMe)[] msgs = new[]
            {
                ($"Hey {friendName}! Did you do the trial?", true),
                ("yeah! beat the goblins. you?", false),
                ("about to. wanna co-op tomorrow?", true),
                ("sure — meet at the portal at noon", false),
            };
            // Scroll inset: top 140 clears the header + translate bar, bottom 88
            // clears the input bar, sides 20 for margin.
            var msgList = MakeScrollList(card.transform, insetTop: 140f, insetBottom: 88f, insetSide: 20f);

            foreach (var m in msgs) BuildChatBubble(msgList, m.text, m.fromMe);

            // Input bar
            var inputBar = new GameObject("InputBar", typeof(RectTransform), typeof(Image));
            inputBar.transform.SetParent(card.transform, false);
            var irt = inputBar.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0); irt.anchorMax = new Vector2(1, 0);
            irt.pivot = new Vector2(0.5f, 0);
            irt.anchoredPosition = new Vector2(0, 16);
            irt.sizeDelta = new Vector2(-32, 60);
            var ibImg = inputBar.GetComponent<Image>();
            ibImg.sprite = LoadRoundedSprite(14); ibImg.type = Image.Type.Sliced;
            ibImg.color = new Color(1f, 0.84f, 0.50f, 1f);
            MakeText(inputBar.transform, "Placeholder", "Tap to enter...",
                18, FontStyles.Italic, new Color(0.55f, 0.32f, 0.18f, 0.85f),
                new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(20, 0), new Vector2(-110, 30))
                .alignment = TextAlignmentOptions.MidlineLeft;
            var sendBtn = MakeBtn(inputBar.transform, "Send", "Send",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-12, 0), new Vector2(86, 44),
                GOLD, DEEP_NAVY, 16);
            sendBtn.onClick.AddListener(() =>
            {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            });
        }

        private static void BuildChatBubble(Transform parent, string text, bool fromMe)
        {
            // Bubble width grows with text up to a cap; height grows with the
            // number of wrapped lines so long messages aren't clipped.
            const float maxW = 560f, charW = 15f, pad = 24f;
            float contentW = 60f + text.Length * charW;
            float bubbleW = Mathf.Min(maxW, contentW);
            int lines = Mathf.Max(1, Mathf.CeilToInt(contentW / (bubbleW - pad * 2f)));
            float bubbleH = 28f + lines * 38f;

            var wrap = new GameObject("Msg", typeof(RectTransform), typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            wrap.transform.SetParent(parent, false);
            wrap.GetComponent<LayoutElement>().preferredHeight = bubbleH + 10f;
            var hlg = wrap.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = fromMe ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            bubble.transform.SetParent(wrap.transform, false);
            var ble = bubble.GetComponent<LayoutElement>();
            ble.preferredWidth = bubbleW;
            ble.preferredHeight = bubbleH;
            var bImg = bubble.GetComponent<Image>();
            bImg.sprite = LoadRoundedSprite(16); bImg.type = Image.Type.Sliced;
            // Light honey for "me" (dark ink), dark slate for others (cream).
            bImg.color = fromMe ? new Color(1f, 0.86f, 0.56f, 1f) : new Color(0.32f, 0.32f, 0.40f, 1f);

            // Text inset from the bubble edges (sizeDelta negative = padding).
            var t = MakeText(bubble.transform, "T", text,
                24, FontStyles.Bold, fromMe ? new Color(0.22f, 0.12f, 0.03f) : CREAM,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-pad * 2f, -16f));
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            if (!fromMe) { try { t.outlineWidth = 0.18f; t.outlineColor = new Color(0,0,0,0.7f); } catch {} }
        }

        // ───────── Security menu (block / report / mute) ─────────
        private static void ShowSecurityMenu(GameObject anchor, string subjectName)
        {
            var canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var modal = new GameObject("SecurityMenu", typeof(RectTransform), typeof(Image), typeof(Button));
            modal.transform.SetParent(canvas.transform, false);
            var mrt = modal.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            modal.transform.SetAsLastSibling();
            modal.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(modal));

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(modal.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(440, 380);
            card.GetComponent<Image>().color = MODAL_BG;

            MakeText(card.transform, "Hdr", subjectName, 26, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -22), new Vector2(0, 32))
                .alignment = TextAlignmentOptions.Center;

            void AddOption(string label, Color color, string toast, float yOff)
            {
                var b = MakeBtn(card.transform, label, label,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, yOff), new Vector2(360, 56), color, CREAM, 18);
                b.onClick.AddListener(() =>
                {
                    try
                    {
                        if (canvas != null)
                            XPFloater.Spawn(canvas.transform,
                                modal.transform.position + new Vector3(0, 60, 0),
                                toast, color);
                    } catch {}
                    Object.Destroy(modal);
                });
            }

            AddOption("Mute",   new Color(0.5f, 0.5f, 0.6f), $"Muted {subjectName}", -76);
            AddOption("Block",  new Color(0.85f, 0.45f, 0.30f), $"Blocked {subjectName}", -148);
            AddOption("Report", new Color(0.85f, 0.30f, 0.40f), "Report submitted to moderators", -220);
            AddOption("Cancel", new Color(0.30f, 0.30f, 0.40f), "", -292);
        }

        private static void BuildFeedRow(Transform parent, FeedPost p)
        {
            var row = new GameObject("Post_" + p.author,
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 92;
            StyleRow(row, ROW_BG);

            MakeCircleAvatar(row.transform, p.author.Substring(0, 1), p.tint);

            var bodyText = MakeText(row.transform, "Body", $"<b>{p.author}</b> {p.action}",
                21, FontStyles.Normal, CREAM,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(108, -18), new Vector2(-220, 36));
            bodyText.alignment = TextAlignmentOptions.MidlineLeft;

            MakeText(row.transform, "Time", p.time,
                15, FontStyles.Bold, new Color(0.85f, 0.82f, 0.65f),
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-58, -18), new Vector2(82, 22))
                .alignment = TextAlignmentOptions.MidlineRight;

            // Per-message AI translate button (small globe pill)
            string original = $"<b>{p.author}</b> {p.action}";
            string translated = $"<b>{p.author}</b> [translated] {p.action}";
            bool[] isTranslated = { false };

            var trBtn = MakeBtn(row.transform, "Translate", "AI",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-14, 0), new Vector2(38, 38),
                new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f),
                DEEP_NAVY, 14);
            trBtn.onClick.AddListener(() =>
            {
                isTranslated[0] = !isTranslated[0];
                bodyText.text = isTranslated[0] ? translated : original;
                var img = trBtn.GetComponent<Image>();
                img.color = isTranslated[0]
                    ? new Color(0.55f, 0.85f, 0.45f, 0.95f)  // green when active
                    : new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f);
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            });
        }

        private static void BuildLeaderRow(Transform parent, LeaderRow r)
        {
            var row = new GameObject("Rank_" + r.rank,
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 104;
            StyleRow(row, r.isYou ? ROW_HI : ROW_BG);

            // Rank # — gold for 1st, bright cream otherwise (was dark = invisible on the dark row)
            MakeText(row.transform, "Rank", $"#{r.rank}",
                42, FontStyles.Bold, r.rank == 1 ? GOLD : CREAM,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(30, 0), new Vector2(110, 64))
                .alignment = TextAlignmentOptions.MidlineLeft;

            // Name — gold if it's you, cream otherwise
            MakeText(row.transform, "Name", r.isYou ? $"{r.name}  (you)" : r.name,
                34, FontStyles.Bold, r.isYou ? GOLD : CREAM,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(150, 0), new Vector2(-280, 48))
                .alignment = TextAlignmentOptions.MidlineLeft;

            // XP — gold, reads on the dark row
            MakeText(row.transform, "XP", $"{r.xp:N0} XP",
                30, FontStyles.Bold, GOLD,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-28, 0), new Vector2(220, 42))
                .alignment = TextAlignmentOptions.MidlineRight;
        }

        // ───────── Reusable bits ─────────

        // Small currency pill: dark wood capsule + colored glyph + amount.
        private static void BuildCurrencyPill(Transform parent, string name, string amount,
            Color fill, Color shadow, string glyph, Vector2 leftAnchorPos)
        {
            var pill = new GameObject(name, typeof(RectTransform), typeof(Image));
            pill.transform.SetParent(parent, false);
            var rt = pill.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = leftAnchorPos;
            rt.sizeDelta = new Vector2(120, 48);
            var img = pill.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(18);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.30f, 0.16f, 0.06f, 0.95f);
            img.raycastTarget = false;

            // Color disc on left
            var disc = new GameObject("Disc", typeof(RectTransform), typeof(Image));
            disc.transform.SetParent(pill.transform, false);
            var drt = disc.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0.5f); drt.anchorMax = new Vector2(0, 0.5f);
            drt.pivot = new Vector2(0, 0.5f);
            drt.anchoredPosition = new Vector2(2, 0);
            drt.sizeDelta = new Vector2(40, 40);
            var dImg = disc.GetComponent<Image>();
            dImg.sprite = LoadCircleSprite();
            dImg.color = fill;
            dImg.raycastTarget = false;
            // Inner brighter disc for shine
            var sh = MakeText(disc.transform, "G", glyph,
                26, FontStyles.Bold, shadow,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            sh.alignment = TextAlignmentOptions.Center;

            // Amount label
            var amt = MakeText(pill.transform, "Amt", amount,
                20, FontStyles.Bold, new Color(1f, 0.97f, 0.85f),
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(46, 0), new Vector2(-8, 0));
            amt.alignment = TextAlignmentOptions.MidlineLeft;
            amt.outlineWidth = 0.20f;
            amt.outlineColor = new Color(0.10f, 0.05f, 0.04f, 0.85f);
        }

        // Give a row a rounded cream background with a subtle brown rim so rows
        // pop against the cream card and text reads more clearly.
        private static void StyleRow(GameObject row, Color fill)
        {
            var img = row.GetComponent<Image>();
            // Beveled Layer Lab convex button sprite (sliced, full-bleed) tinted
            // to the row fill. Was the ItemFrame sprite, but its decorative
            // transparent padding shrank the visible row and clipped the status
            // text — the convex sprite fills edge-to-edge cleanly.
            var rowSp = LoadLL(LL_BTN_CONVEX);
            if (img != null)
            {
                if (rowSp != null) { img.sprite = rowSp; img.type = Image.Type.Sliced; }
                else { img.sprite = LoadRoundedSprite(14); img.type = Image.Type.Sliced; }
                img.color = fill;
            }
        }

        // Small gold level badge in the bottom-right corner of an avatar circle.
        private static void AddLevelBadge(Transform avatar, int level)
        {
            var badge = new GameObject("LvBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(avatar, false);
            var rt = badge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(2, -4);
            rt.sizeDelta = new Vector2(48, 22);
            var img = badge.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(10);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.78f, 0.50f, 0.10f);
            img.raycastTarget = false;
            // Inner highlight
            var inner = new GameObject("In", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(badge.transform, false);
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(2, 2); irt.offsetMax = new Vector2(-2, -2);
            var iImg = inner.GetComponent<Image>();
            iImg.sprite = LoadRoundedSprite(8);
            iImg.type = Image.Type.Sliced;
            iImg.color = new Color(0.98f, 0.78f, 0.30f);
            iImg.raycastTarget = false;
            var lbl = MakeText(inner.transform, "T", $"Lv{level}",
                13, FontStyles.Bold, new Color(0.18f, 0.05f, 0.10f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            lbl.alignment = TextAlignmentOptions.Center;
        }

        private static GameObject MakeCircleAvatar(Transform parent, string letter, Color tint)
        {
            // Outer circle frame
            var av = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            av.transform.SetParent(parent, false);
            var rt = av.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(16, 0);
            rt.sizeDelta = new Vector2(96, 96);

            var frame = LoadSprite("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/BaseFrame_Border_Circle_H58.png");
            var img = av.GetComponent<Image>();
            if (frame != null) { img.sprite = frame; img.preserveAspect = true; img.color = Color.white; }
            else img.color = tint;

            // Inner color disc
            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(av.transform, false);
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(8, 8); irt.offsetMax = new Vector2(-8, -8);
            var iimg = inner.GetComponent<Image>();
            iimg.sprite = LoadCircleSprite();
            iimg.color = tint;
            iimg.raycastTarget = false;

            var lblGO = new GameObject("Letter", typeof(RectTransform));
            lblGO.transform.SetParent(av.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = letter;
            tm.fontSize = 40;
            tm.fontStyle = FontStyles.Bold;
            tm.color = DEEP_NAVY;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;

            return av;
        }

        // Per-tab color memory so StyleTab can recolor on switch
        private static readonly System.Collections.Generic.Dictionary<Button, (Color top, Color shadow)> _tabPalette
            = new System.Collections.Generic.Dictionary<Button, (Color, Color)>();

        private static Button MakeTabBtn(Transform parent, string name, string label,
            string icon, Color topColor, Color shadowColor)
        {
            // Wrapper with relative-stretch layout; we'll add a shadow disc
            // below the pill and the pill itself on top.
            var wrap = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
            wrap.transform.SetParent(parent, false);
            var wImg = wrap.GetComponent<Image>();
            wImg.color = new Color(0, 0, 0, 0);            // invisible click hitbox
            wImg.raycastTarget = true;

            // Soft halo behind everything (visible only when active)
            var halo = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            halo.transform.SetParent(wrap.transform, false);
            var hrt = halo.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = new Vector2(-12, -12); hrt.offsetMax = new Vector2(12, 12);
            var hImg = halo.GetComponent<Image>();
            hImg.sprite = LoadRoundedSprite(28);
            hImg.type = Image.Type.Sliced;
            hImg.color = new Color(topColor.r, topColor.g, topColor.b, 0f);   // hidden by default
            hImg.raycastTarget = false;

            // Bottom shadow pill
            var shadow = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadow.transform.SetParent(wrap.transform, false);
            var srt = shadow.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(0, -6); srt.offsetMax = new Vector2(0, 0);
            var sImg = shadow.GetComponent<Image>();
            sImg.sprite = LoadRoundedSprite(22);
            sImg.type = Image.Type.Sliced;
            sImg.color = shadowColor;
            sImg.raycastTarget = false;

            // Top pill
            var pill = new GameObject("Pill", typeof(RectTransform), typeof(Image));
            pill.transform.SetParent(wrap.transform, false);
            var prt = pill.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(0, 4); prt.offsetMax = new Vector2(0, 4);
            var pImg = pill.GetComponent<Image>();
            pImg.sprite = LoadRoundedSprite(22);
            pImg.type = Image.Type.Sliced;
            pImg.color = topColor;
            pImg.raycastTarget = false;

            // Icon — try to load a real sprite from FantasyIconPack via the
            //  `icon` parameter (e.g. "HeartFull.png"). Falls back to a glyph
            //  if no sprite found.
            Sprite iconSprite = (icon != null && icon.EndsWith(".png"))
                ? LoadFantasyIcon(icon) : null;
            if (iconSprite != null)
            {
                var icGO = new GameObject("Ic", typeof(RectTransform), typeof(Image));
                icGO.transform.SetParent(pill.transform, false);
                var icrt = icGO.GetComponent<RectTransform>();
                icrt.anchorMin = new Vector2(0, 0); icrt.anchorMax = new Vector2(0.32f, 1);
                icrt.offsetMin = new Vector2(8, 6); icrt.offsetMax = new Vector2(-2, -6);
                var icImg = icGO.GetComponent<Image>();
                icImg.sprite = iconSprite;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
            }
            else
            {
                var ic = MakeText(pill.transform, "Ic", icon,
                    30, FontStyles.Bold, new Color(0.18f, 0.08f, 0.18f),
                    new Vector2(0, 0), new Vector2(0.32f, 1), Vector2.zero, Vector2.zero);
                ic.alignment = TextAlignmentOptions.Center;
                ic.outlineWidth = 0.22f;
                ic.outlineColor = new Color(1f, 0.97f, 0.85f, 0.95f);
            }

            // Label — dark ink with cream halo for max contrast
            var lbl = MakeText(pill.transform, "Lbl", label,
                30, FontStyles.Bold, new Color(0.18f, 0.08f, 0.18f),
                new Vector2(0.28f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.outlineWidth = 0.22f;
            lbl.outlineColor = new Color(1f, 0.97f, 0.85f, 0.95f);

            var btn = wrap.GetComponent<Button>();
            btn.targetGraphic = wImg;
            btn.transition = Selectable.Transition.None;
            _tabPalette[btn] = (topColor, shadowColor);
            return btn;
        }

        private static void StyleTab(Button b, bool active)
        {
            if (b == null) return;
            if (!_tabPalette.TryGetValue(b, out var pal)) return;
            var pill   = b.transform.Find("Pill");
            var shadow = b.transform.Find("Shadow");
            var halo   = b.transform.Find("Halo");
            if (halo != null)
            {
                var hImg = halo.GetComponent<Image>();
                if (hImg != null)
                    hImg.color = active
                        ? new Color(pal.top.r, pal.top.g, pal.top.b, 0.55f)
                        : new Color(pal.top.r, pal.top.g, pal.top.b, 0f);
            }
            if (pill != null)
            {
                var img = pill.GetComponent<Image>();
                if (img != null)
                    img.color = active
                        ? new Color(Mathf.Clamp01(pal.top.r * 1.15f), Mathf.Clamp01(pal.top.g * 1.15f), Mathf.Clamp01(pal.top.b * 1.15f), 1f)
                        : pal.top;
            }
            if (shadow != null)
            {
                var img = shadow.GetComponent<Image>();
                if (img != null)
                {
                    var c = pal.shadow;
                    img.color = active ? new Color(c.r, c.g, c.b, 1f) : new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 1f);
                }
            }
            // Lift active tab slightly: shrink shadow, raise pill
            var pillRT = pill as RectTransform;
            var shadRT = shadow as RectTransform;
            if (pillRT != null)
            {
                pillRT.offsetMin = new Vector2(0, active ? 6 : 4);
                pillRT.offsetMax = new Vector2(0, active ? 6 : 4);
            }
            if (shadRT != null)
            {
                shadRT.offsetMin = new Vector2(0, active ? -8 : -6);
            }
            foreach (var tm in b.GetComponentsInChildren<TMP_Text>(true))
            {
                tm.fontStyle = FontStyles.Bold;
            }
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
            return tm;
        }

        // Rounded-corner sprite cache, generated procedurally so we don't depend
        // on any specific UI asset pack.
        private static System.Collections.Generic.Dictionary<int, Sprite> _roundedCache
            = new System.Collections.Generic.Dictionary<int, Sprite>();
        private static Sprite LoadRoundedSprite(int radius)
        {
            if (_roundedCache.TryGetValue(radius, out var sp) && sp != null) return sp;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside; int dx = 0, dy = 0;
                if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inside = dx*dx+dy*dy <= radius*radius; }
                else if (x >= size-radius && y < radius) { dx = x-(size-radius-1); dy = radius-y; inside = dx*dx+dy*dy <= radius*radius; }
                else if (x < radius && y >= size-radius) { dx = radius-x; dy = y-(size-radius-1); inside = dx*dx+dy*dy <= radius*radius; }
                else if (x >= size-radius && y >= size-radius) { dx = x-(size-radius-1); dy = y-(size-radius-1); inside = dx*dx+dy*dy <= radius*radius; }
                else inside = true;
                tex.SetPixel(x, y, inside ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _roundedCache[radius] = sp;
            return sp;
        }

        private static Sprite _circleSprite;
        private static Sprite LoadCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.46f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _circleSprite;
        }

        private static Sprite LoadSprite(string path)
        {
            #if UNITY_EDITOR
            return Sparq.Core.SpriteLoader.Load(path);
            #else
            return null; // runtime fallback — these sprites need to be in Resources for runtime load
            #endif
        }

        // ── Layer Lab fantasy frame/button sprites ───────────────────────
        private const string LL_POPUP_BG     = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Popup/Popup_02_White_Bg.png";
        private const string LL_POPUP_BORDER = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Popup/Popup_02_White_Border.png";
        private const string LL_POPUP_DECO   = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Popup/Popup_02_White_DecoBorder_Top.png";
        private const string LL_BTN_CONVEX   = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Convex_Rectangle_01_Gray.png";
        private const string LL_ROW_FRAME    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/ItemFrame_Square_02_Single_Bg_Yellow.png";
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> _llCache
            = new System.Collections.Generic.Dictionary<string, Sprite>();

        // Generic Layer Lab sprite loader (by full asset path), importing as a
        // Sprite the first time and caching the result.
        private static Sprite LoadLL(string path)
        {
            if (_llCache.TryGetValue(path, out var c) && c != null) return c;
            #if UNITY_EDITOR
            try
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null && !Application.isPlaying &&
                    imp.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp != null) _llCache[path] = sp;
                return sp;
            }
            catch { return null; }
            #else
            return null;
            #endif
        }

        // Load a FantasyIconPack icon by file name, importing it as a Sprite the
        // first time. Returns null if the asset is missing.
        private static Sprite LoadFantasyIcon(string fileName)
        {
            string path = $"Assets/FantasyIconPack/256/{fileName}";
            #if UNITY_EDITOR
            // Editor-only: fix sprite import settings the first time.
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && !Application.isPlaying &&
                imp.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                imp.textureType = UnityEditor.TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            #endif
            // Runs in both Editor + Player builds. Previously wrapped in
            // #if UNITY_EDITOR — that's why World/Chat tab buttons rendered
            // their iconPath strings as text fallback instead of icons.
            return Sparq.Core.SpriteLoader.Load(path);
        }

        private static Button MakeBtn(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd,
            Color bg, Color fg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            var bImg = go.GetComponent<Image>();
            // Beveled Layer Lab convex button sprite (sliced), tinted to bg.
            var btnSp = LoadLL(LL_BTN_CONVEX);
            if (btnSp != null) { bImg.sprite = btnSp; bImg.type = Image.Type.Sliced; }
            bImg.color = bg;
            var lbl = MakeText(go.transform, "Lbl", label, fontSize, FontStyles.Bold, fg,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            lbl.alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }
    }
}
