using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Chat panel — three tabs, modelled on Layer Lab's Chat.prefab look:
    ///   • INDIVIDUAL — a DM inbox: list of 1-on-1 conversations; tap one to
    ///                  open that private thread (with a back button).
    ///   • WORLD      — public channel chat.
    ///   • GUILD      — guild/clan channel chat.
    ///
    /// Built procedurally but skinned with the real Layer Lab sprites
    /// (Popup_Box frame, Label_Bubble message bubbles, InputField bar, tab
    /// art) so it matches the polished template — while we keep full control
    /// of the tab logic, DM navigation, and the safety pipeline.
    ///
    /// Every outgoing message is run through RateLimiter + ContentModerator,
    /// exactly like ChatSender — chat is moderated.
    /// </summary>
    public static class ChatPanel
    {
        // ── Layer Lab sprite paths ──
        private const string POPUP_BG     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg.png";
        private const string POPUP_BORDER = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Border.png";
        private const string BUBBLE_BG    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Label/Label_Bubble_01_Bg.png";
        private const string INPUT_BG     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/UI_Etc/InputField_02_Bg.png";
        private const string CIRCLE_BG    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png";

        // ── Palette (matches the 3_Chat.png template) ──
        private static readonly Color CARD_DARK   = new Color(0.16f, 0.13f, 0.25f, 1f);
        private static readonly Color TITLEBAR    = new Color(0.22f, 0.18f, 0.34f, 1f);
        private static readonly Color TAB_ON      = new Color(0.55f, 0.40f, 0.92f, 1f);
        private static readonly Color TAB_OFF     = new Color(0.27f, 0.23f, 0.40f, 1f);
        private static readonly Color BUBBLE_THEM = new Color(0.42f, 0.36f, 0.58f, 1f);
        private static readonly Color BUBBLE_YOU  = new Color(1f, 0.80f, 0.36f, 1f);
        private static readonly Color INK         = new Color(0.10f, 0.06f, 0.16f);
        private static readonly Color GOLD        = new Color(1f, 0.82f, 0.30f);
        private static readonly Color SEND_BLUE   = new Color(0.36f, 0.56f, 0.95f, 1f);
        private static readonly Color ROW_BG      = new Color(0.21f, 0.17f, 0.32f, 1f);
        private static readonly Color CREAM       = new Color(0.96f, 0.95f, 1f);

        // ── Data model ──
        private class Msg { public string author; public string text; public bool fromMe; public string time; public Color tint; }
        private class Convo { public string name; public Color tint; public string lastMsg; public string time; public int unread; public List<Msg> thread; }

        // Channel message logs (mock — a real backend would feed these)
        private static List<Msg> _world;
        private static List<Msg> _guild;
        private static List<Convo> _dms;

        // ── Runtime state ──
        private static GameObject _root;
        private static Transform  _contentFrame;       // swappable view host
        private static GameObject _inputBar;           // shared bottom input row
        private static TMP_InputField _input;
        private static int _tab = 1;                   // 0=Individual 1=World 2=Guild
        private static Convo _openDm;                  // non-null when a DM thread is open
        private static System.Collections.Generic.List<(Image bg, TMP_Text lbl, int idx)> _tabs;
        private static ScrollRect _activeScroll;       // current view's scroll (for auto-scroll on send)
        private static Transform  _activeList;         // current view's message-list content
        private static System.Action _sendHandler;     // what the send button does in the current view

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);
            EnsureData();
            EnsureEventSystem();
            _openDm = null;

            // Overlay canvas
            _root = new GameObject("Sparq_ChatPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop — tap to close
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card — Layer Lab popup frame, tinted dark navy
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(980, 1620);
            var cardImg = card.GetComponent<Image>();
            var bgSp = LoadSprite(POPUP_BG);
            if (bgSp != null) { cardImg.sprite = bgSp; cardImg.type = Image.Type.Sliced; }
            cardImg.color = CARD_DARK;

            // Ornate border on top
            var border = NewGO("Border", card.transform, typeof(Image));
            Stretch(border.GetComponent<RectTransform>());
            var brImg = border.GetComponent<Image>();
            var brSp = LoadSprite(POPUP_BORDER);
            if (brSp != null) { brImg.sprite = brSp; brImg.type = Image.Type.Sliced; brImg.color = Color.white; }
            else brImg.color = new Color(0.55f, 0.42f, 0.30f, 0.5f);
            brImg.raycastTarget = false;

            BuildTitleBar(card.transform);
            BuildTabRow(card.transform);

            // Content frame — between tab row and input bar
            var cf = NewGO("ContentFrame", card.transform);
            var cfRT = cf.GetComponent<RectTransform>();
            cfRT.anchorMin = new Vector2(0, 0); cfRT.anchorMax = new Vector2(1, 1);
            cfRT.offsetMin = new Vector2(28, 150);     // leave room for input bar
            cfRT.offsetMax = new Vector2(-28, -290);   // below title + tabs
            _contentFrame = cf.transform;

            BuildInputBar(card.transform);

            SwitchTab(1);   // open on World by default
            Debug.Log("[ChatPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
            _contentFrame = null; _inputBar = null; _input = null;
            _tabs = null; _activeScroll = null; _activeList = null;
            _sendHandler = null; _openDm = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // TITLE + TABS
        // ─────────────────────────────────────────────────────────────────

        private static void BuildTitleBar(Transform card)
        {
            var bar = NewGO("TitleBar", card, typeof(Image));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -14);
            rt.sizeDelta = new Vector2(-36, 120);
            bar.GetComponent<Image>().color = TITLEBAR;

            var title = MakeText(bar.transform, "Title", "CHAT", 56, FontStyles.Bold, GOLD);
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0.20f, 0.08f, 0.02f); } catch {}

            // Close X
            var close = NewGO("Close", bar.transform, typeof(Image), typeof(Button));
            var xrt = close.GetComponent<RectTransform>();
            xrt.anchorMin = new Vector2(1, 0.5f); xrt.anchorMax = new Vector2(1, 0.5f);
            xrt.pivot = new Vector2(1, 0.5f);
            xrt.anchoredPosition = new Vector2(-20, 0);
            xrt.sizeDelta = new Vector2(78, 78);
            close.GetComponent<Image>().color = new Color(0.82f, 0.26f, 0.26f, 1f);
            var xl = MakeText(close.transform, "X", "X", 44, FontStyles.Bold, Color.white);
            Stretch(xl.rectTransform); xl.alignment = TextAlignmentOptions.Center;
            close.GetComponent<Button>().onClick.AddListener(Hide);
        }

        private static void BuildTabRow(Transform card)
        {
            var row = NewGO("TabRow", card);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -150);
            rt.sizeDelta = new Vector2(-56, 110);

            _tabs = new System.Collections.Generic.List<(Image, TMP_Text, int)>();
            string[] names = { "Individual", "World", "Guild" };
            float w = 304f, gap = 14f;
            float startX = -(w + gap);
            for (int i = 0; i < 3; i++)
            {
                var tab = NewGO("Tab_" + names[i], row.transform, typeof(Image), typeof(Button));
                var trt = tab.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = new Vector2(startX + i * (w + gap), 0);
                trt.sizeDelta = new Vector2(w, 88);
                var img = tab.GetComponent<Image>();
                img.color = TAB_OFF;
                var lbl = MakeText(tab.transform, "L", names[i], 30, FontStyles.Bold, CREAM);
                Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
                int idx = i;
                tab.GetComponent<Button>().onClick.AddListener(() => SwitchTab(idx));
                _tabs.Add((img, lbl, i));
            }
        }

        private static void SwitchTab(int idx)
        {
            _tab = idx;
            _openDm = null;
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            if (_tabs != null)
                foreach (var (bg, lbl, i) in _tabs)
                {
                    bool on = i == idx;
                    bg.color = on ? TAB_ON : TAB_OFF;
                    lbl.color = on ? Color.white : new Color(0.78f, 0.74f, 0.88f);
                }

            // Rebuild the content frame for the selected tab
            ClearContent();
            if (idx == 0) BuildInboxView();
            else if (idx == 1) BuildChannelView(_world, "World · 1,284 online", true);
            else BuildChannelView(_guild, "Guild · Quiet Forge · 12 online", false);
        }

        // ─────────────────────────────────────────────────────────────────
        // CHANNEL VIEW (World / Guild) + DM THREAD share this layout
        // ─────────────────────────────────────────────────────────────────

        private static void BuildChannelView(List<Msg> log, string subtitle, bool isWorld)
        {
            // Subtitle strip
            var sub = MakeText(_contentFrame, "Sub", subtitle, 24, FontStyles.Italic,
                new Color(0.74f, 0.70f, 0.88f));
            var srt = sub.rectTransform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -4);
            srt.sizeDelta = new Vector2(-20, 40);
            sub.alignment = TextAlignmentOptions.Center;

            var (scroll, listContent) = BuildScrollList(_contentFrame, topInset: 52f);
            _activeScroll = scroll; _activeList = listContent;

            foreach (var m in log) AddBubble(listContent, m);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;

            // Input bar sends into THIS channel
            _sendHandler = () => {
                string raw = _input != null ? (_input.text ?? "").Trim() : "";
                if (TrySend(raw, out Msg sent))
                {
                    log.Add(sent);
                    AddBubble(_activeList, sent);
                    Canvas.ForceUpdateCanvases();
                    if (_activeScroll != null) _activeScroll.verticalNormalizedPosition = 0f;
                }
            };
            ShowInputBar(true, backToInbox: false);
        }

        // ─────────────────────────────────────────────────────────────────
        // INDIVIDUAL — DM inbox list
        // ─────────────────────────────────────────────────────────────────

        private static void BuildInboxView()
        {
            var (scroll, listContent) = BuildScrollList(_contentFrame, topInset: 0f);
            _activeScroll = scroll; _activeList = listContent;

            foreach (var c in _dms) AddInboxRow(listContent, c);

            ShowInputBar(false, backToInbox: false);   // no input on the inbox list itself
        }

        private static void AddInboxRow(Transform parent, Convo c)
        {
            var row = NewGO("Convo_" + c.name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = 132;
            row.GetComponent<Image>().color = ROW_BG;

            // Avatar circle
            var av = NewGO("Avatar", row.transform, typeof(Image));
            var avRT = av.GetComponent<RectTransform>();
            avRT.anchorMin = new Vector2(0, 0.5f); avRT.anchorMax = new Vector2(0, 0.5f);
            avRT.pivot = new Vector2(0, 0.5f);
            avRT.anchoredPosition = new Vector2(20, 0);
            avRT.sizeDelta = new Vector2(92, 92);
            var avImg = av.GetComponent<Image>();
            var circ = LoadSprite(CIRCLE_BG);
            if (circ != null) avImg.sprite = circ;
            avImg.color = c.tint;
            avImg.raycastTarget = false;
            var initial = MakeText(av.transform, "I", c.name.Substring(0, 1).ToUpper(),
                42, FontStyles.Bold, Color.white);
            Stretch(initial.rectTransform); initial.alignment = TextAlignmentOptions.Center;

            // Name + last message
            var nm = MakeText(row.transform, "Name", c.name, 32, FontStyles.Bold, CREAM);
            var nmRT = nm.rectTransform;
            nmRT.anchorMin = new Vector2(0, 1); nmRT.anchorMax = new Vector2(1, 1);
            nmRT.pivot = new Vector2(0, 1);
            nmRT.anchoredPosition = new Vector2(132, -16);
            nmRT.sizeDelta = new Vector2(-280, 44);
            nm.alignment = TextAlignmentOptions.MidlineLeft;

            var last = MakeText(row.transform, "Last", c.lastMsg, 25, FontStyles.Normal,
                new Color(0.72f, 0.68f, 0.84f));
            var lastRT = last.rectTransform;
            lastRT.anchorMin = new Vector2(0, 0); lastRT.anchorMax = new Vector2(1, 0);
            lastRT.pivot = new Vector2(0, 0);
            lastRT.anchoredPosition = new Vector2(132, 18);
            lastRT.sizeDelta = new Vector2(-280, 44);
            last.alignment = TextAlignmentOptions.MidlineLeft;
            last.textWrappingMode = TextWrappingModes.NoWrap;
            last.overflowMode = TextOverflowModes.Ellipsis;

            // Time + unread badge
            var tm = MakeText(row.transform, "Time", c.time, 22, FontStyles.Normal,
                new Color(0.62f, 0.58f, 0.74f));
            var tmRT = tm.rectTransform;
            tmRT.anchorMin = new Vector2(1, 1); tmRT.anchorMax = new Vector2(1, 1);
            tmRT.pivot = new Vector2(1, 1);
            tmRT.anchoredPosition = new Vector2(-22, -18);
            tmRT.sizeDelta = new Vector2(140, 34);
            tm.alignment = TextAlignmentOptions.MidlineRight;

            if (c.unread > 0)
            {
                var badge = NewGO("Unread", row.transform, typeof(Image));
                var bRT = badge.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(1, 0); bRT.anchorMax = new Vector2(1, 0);
                bRT.pivot = new Vector2(1, 0);
                bRT.anchoredPosition = new Vector2(-22, 22);
                bRT.sizeDelta = new Vector2(48, 48);
                var circ2 = LoadSprite(CIRCLE_BG);
                if (circ2 != null) badge.GetComponent<Image>().sprite = circ2;
                badge.GetComponent<Image>().color = new Color(0.92f, 0.30f, 0.32f, 1f);
                var bt = MakeText(badge.transform, "N", c.unread.ToString(), 26, FontStyles.Bold, Color.white);
                Stretch(bt.rectTransform); bt.alignment = TextAlignmentOptions.Center;
            }

            var captured = c;
            row.GetComponent<Button>().onClick.AddListener(() => OpenDmThread(captured));
        }

        private static void OpenDmThread(Convo c)
        {
            _openDm = c;
            c.unread = 0;
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            ClearContent();

            // Header: avatar + name
            var hdr = MakeText(_contentFrame, "DmHdr", c.name, 30, FontStyles.Bold, GOLD);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -4);
            hRT.sizeDelta = new Vector2(-20, 44);
            hdr.alignment = TextAlignmentOptions.Center;

            var (scroll, listContent) = BuildScrollList(_contentFrame, topInset: 56f);
            _activeScroll = scroll; _activeList = listContent;
            foreach (var m in c.thread) AddBubble(listContent, m);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;

            _sendHandler = () => {
                string raw = _input != null ? (_input.text ?? "").Trim() : "";
                if (TrySend(raw, out Msg sent))
                {
                    c.thread.Add(sent);
                    c.lastMsg = sent.text;
                    c.time = sent.time;
                    AddBubble(_activeList, sent);
                    Canvas.ForceUpdateCanvases();
                    if (_activeScroll != null) _activeScroll.verticalNormalizedPosition = 0f;
                }
            };
            ShowInputBar(true, backToInbox: true);
        }

        // ─────────────────────────────────────────────────────────────────
        // MESSAGE BUBBLE
        // ─────────────────────────────────────────────────────────────────

        private static void AddBubble(Transform listContent, Msg m)
        {
            // Row holds avatar + bubble; height grows with wrapped text.
            var row = NewGO("Msg", listContent, typeof(LayoutElement));
            var le = row.GetComponent<LayoutElement>();
            int lines = Mathf.Max(1, Mathf.CeilToInt(m.text.Length / 34f));
            le.preferredHeight = 70 + lines * 34;

            // Avatar (incoming only)
            if (!m.fromMe)
            {
                var av = NewGO("Av", row.transform, typeof(Image));
                var avRT = av.GetComponent<RectTransform>();
                avRT.anchorMin = new Vector2(0, 1); avRT.anchorMax = new Vector2(0, 1);
                avRT.pivot = new Vector2(0, 1);
                avRT.anchoredPosition = new Vector2(4, -8);
                avRT.sizeDelta = new Vector2(64, 64);
                var circ = LoadSprite(CIRCLE_BG);
                if (circ != null) av.GetComponent<Image>().sprite = circ;
                av.GetComponent<Image>().color = m.tint;
                av.GetComponent<Image>().raycastTarget = false;
                var ini = MakeText(av.transform, "I",
                    string.IsNullOrEmpty(m.author) ? "?" : m.author.Substring(0, 1).ToUpper(),
                    30, FontStyles.Bold, Color.white);
                Stretch(ini.rectTransform); ini.alignment = TextAlignmentOptions.Center;
            }

            // Name + time (incoming only)
            float bubbleTop = -6;
            if (!m.fromMe)
            {
                var nt = MakeText(row.transform, "NT", $"{m.author}   <color=#9A93B0>{m.time}</color>",
                    21, FontStyles.Bold, GOLD);
                var ntRT = nt.rectTransform;
                ntRT.anchorMin = new Vector2(0, 1); ntRT.anchorMax = new Vector2(1, 1);
                ntRT.pivot = new Vector2(0, 1);
                ntRT.anchoredPosition = new Vector2(82, -6);
                ntRT.sizeDelta = new Vector2(-92, 28);
                nt.alignment = TextAlignmentOptions.MidlineLeft;
                bubbleTop = -36;
            }

            // Bubble
            var bubble = NewGO("Bubble", row.transform, typeof(Image));
            var brt = bubble.GetComponent<RectTransform>();
            float maxW = 620f;
            brt.anchorMin = new Vector2(m.fromMe ? 1 : 0, 1);
            brt.anchorMax = new Vector2(m.fromMe ? 1 : 0, 1);
            brt.pivot = new Vector2(m.fromMe ? 1 : 0, 1);
            brt.anchoredPosition = new Vector2(m.fromMe ? -8 : 82, bubbleTop);
            brt.sizeDelta = new Vector2(maxW, le.preferredHeight + bubbleTop - 8);
            var bImg = bubble.GetComponent<Image>();
            var bubbleSp = LoadSprite(BUBBLE_BG);
            if (bubbleSp != null) { bImg.sprite = bubbleSp; bImg.type = Image.Type.Sliced; }
            bImg.color = m.fromMe ? BUBBLE_YOU : BUBBLE_THEM;
            bImg.raycastTarget = false;

            var body = MakeText(bubble.transform, "Body", m.text, 26, FontStyles.Normal,
                m.fromMe ? INK : CREAM);
            var bodyRT = body.rectTransform;
            Stretch(bodyRT);
            bodyRT.offsetMin = new Vector2(28, 20); bodyRT.offsetMax = new Vector2(-28, -16);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
        }

        // ─────────────────────────────────────────────────────────────────
        // INPUT BAR  (shared; contextual back button for DM threads)
        // ─────────────────────────────────────────────────────────────────

        private static void BuildInputBar(Transform card)
        {
            var bar = NewGO("InputBar", card, typeof(Image));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 22);
            rt.sizeDelta = new Vector2(-36, 116);
            bar.GetComponent<Image>().color = TITLEBAR;
            _inputBar = bar;

            // Back button (shown only inside a DM thread)
            var back = NewGO("Back", bar.transform, typeof(Image), typeof(Button));
            var bkRT = back.GetComponent<RectTransform>();
            bkRT.anchorMin = new Vector2(0, 0.5f); bkRT.anchorMax = new Vector2(0, 0.5f);
            bkRT.pivot = new Vector2(0, 0.5f);
            bkRT.anchoredPosition = new Vector2(16, 0);
            bkRT.sizeDelta = new Vector2(84, 84);
            var bkCirc = LoadSprite(CIRCLE_BG);
            if (bkCirc != null) back.GetComponent<Image>().sprite = bkCirc;
            back.GetComponent<Image>().color = new Color(0.40f, 0.34f, 0.55f, 1f);
            var bkL = MakeText(back.transform, "L", "<", 44, FontStyles.Bold, Color.white);
            Stretch(bkL.rectTransform); bkL.alignment = TextAlignmentOptions.Center;
            back.GetComponent<Button>().onClick.AddListener(() => SwitchTab(0));
            back.name = "Back";

            // Input field — Layer Lab InputField sprite, tinted
            var field = NewGO("Field", bar.transform, typeof(Image), typeof(TMP_InputField));
            var fRT = field.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0, 0.5f); fRT.anchorMax = new Vector2(1, 0.5f);
            fRT.pivot = new Vector2(0, 0.5f);
            fRT.anchoredPosition = new Vector2(116, 0);
            fRT.sizeDelta = new Vector2(-242, 84);
            var fImg = field.GetComponent<Image>();
            var inSp = LoadSprite(INPUT_BG);
            if (inSp != null) { fImg.sprite = inSp; fImg.type = Image.Type.Sliced; }
            fImg.color = new Color(0.10f, 0.08f, 0.16f, 1f);

            var ta = NewGO("Text Area", field.transform, typeof(RectMask2D));
            var taRT = ta.GetComponent<RectTransform>();
            Stretch(taRT); taRT.offsetMin = new Vector2(24, 6); taRT.offsetMax = new Vector2(-24, -6);

            var ph = NewGO("Placeholder", ta.transform);
            Stretch(ph.GetComponent<RectTransform>());
            var phTm = ph.AddComponent<TextMeshProUGUI>();
            phTm.text = "Enter Text...";
            phTm.fontSize = 28; phTm.fontStyle = FontStyles.Italic;
            phTm.color = new Color(0.55f, 0.52f, 0.66f);
            phTm.font = TMP_Settings.defaultFontAsset;
            phTm.alignment = TextAlignmentOptions.MidlineLeft;
            phTm.raycastTarget = false;

            var txt = NewGO("Text", ta.transform);
            Stretch(txt.GetComponent<RectTransform>());
            var txtTm = txt.AddComponent<TextMeshProUGUI>();
            txtTm.text = ""; txtTm.fontSize = 28; txtTm.color = Color.white;
            txtTm.font = TMP_Settings.defaultFontAsset;
            txtTm.alignment = TextAlignmentOptions.MidlineLeft;
            txtTm.raycastTarget = false;

            _input = field.GetComponent<TMP_InputField>();
            _input.textViewport = taRT;
            _input.textComponent = txtTm;
            _input.placeholder = phTm;
            _input.characterLimit = 240;
            _input.lineType = TMP_InputField.LineType.SingleLine;
            _input.fontAsset = TMP_Settings.defaultFontAsset;
            _input.pointSize = 28;

            // Send button — blue circle, paper-plane glyph
            var send = NewGO("Send", bar.transform, typeof(Image), typeof(Button));
            var sRT = send.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(1, 0.5f); sRT.anchorMax = new Vector2(1, 0.5f);
            sRT.pivot = new Vector2(1, 0.5f);
            sRT.anchoredPosition = new Vector2(-16, 0);
            sRT.sizeDelta = new Vector2(100, 84);
            var sCirc = LoadSprite(CIRCLE_BG);
            if (sCirc != null) send.GetComponent<Image>().sprite = sCirc;
            send.GetComponent<Image>().color = SEND_BLUE;
            var sL = MakeText(send.transform, "L", "Send", 26, FontStyles.Bold, Color.white);
            Stretch(sL.rectTransform); sL.alignment = TextAlignmentOptions.Center;
            send.GetComponent<Button>().onClick.AddListener(() => { _sendHandler?.Invoke(); });
        }

        private static void ShowInputBar(bool visible, bool backToInbox)
        {
            if (_inputBar == null) return;
            _inputBar.SetActive(visible);
            if (!visible) return;
            var back = _inputBar.transform.Find("Back");
            if (back != null) back.gameObject.SetActive(backToInbox);
            // When the back button is hidden, slide the field left to fill the gap.
            var field = _inputBar.transform.Find("Field") as RectTransform;
            if (field != null)
                field.anchoredPosition = new Vector2(backToInbox ? 116 : 24, 0);
            if (_input != null) { _input.text = ""; _input.ActivateInputField(); }
        }

        // ─────────────────────────────────────────────────────────────────
        // SEND — routed through the safety stack (same as ChatSender)
        // ─────────────────────────────────────────────────────────────────

        private static bool TrySend(string raw, out Msg sent)
        {
            sent = null;
            if (string.IsNullOrEmpty(raw)) return false;

            // Rate limit / mute check
            if (!Sparq.Safety.RateLimiter.CanSend(out string rateReason))
            {
                Toast(rateReason, new Color(0.85f, 0.45f, 0.30f));
                return false;
            }

            // Content moderation
            var verdict = Sparq.Safety.ContentModerator.Inspect(raw, "chat");
            if (!verdict.Allowed)
            {
                Toast(verdict.UserFacingMessage, new Color(0.85f, 0.30f, 0.30f));
                if (_input != null) _input.text = verdict.SanitizedText;   // let them revise; PII hidden
                // Self-harm ideation still routes to crisis resources.
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                    && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
                { try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {} }
                return false;
            }

            string clean = verdict.SanitizedText;
            if (verdict.Severity == Sparq.Safety.ContentModerator.Severity.Warn &&
                !string.IsNullOrEmpty(verdict.UserFacingMessage))
                Toast(verdict.UserFacingMessage, new Color(1f, 0.78f, 0.30f));

            Sparq.Safety.RateLimiter.RecordSend();
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            if (_input != null) { _input.text = ""; _input.ActivateInputField(); }

            // Crisis check even on an allowed message (ideation that isn't a block)
            if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
            { try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {} }

            sent = new Msg { author = "You", text = clean, fromMe = true, time = NowHM(), tint = GOLD };
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        // SCROLL LIST + HELPERS
        // ─────────────────────────────────────────────────────────────────

        // Returns (scrollRect, content) — content has a VerticalLayoutGroup.
        private static (ScrollRect, Transform) BuildScrollList(Transform parent, float topInset)
        {
            var scrollGO = NewGO("Scroll", parent, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = Vector2.zero; srRT.anchorMax = Vector2.one;
            srRT.offsetMin = new Vector2(0, 0); srRT.offsetMax = new Vector2(0, -topInset);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.18f);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 28f;

            var viewport = NewGO("Viewport", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            var vpRT = viewport.GetComponent<RectTransform>();
            Stretch(vpRT); vpRT.offsetMin = new Vector2(8, 8); vpRT.offsetMax = new Vector2(-8, -8);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", viewport.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.padding = new RectOffset(6, 6, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRT; sr.content = ctRT;
            return (sr, content.transform);
        }

        private static void ClearContent()
        {
            if (_contentFrame == null) return;
            for (int i = _contentFrame.childCount - 1; i >= 0; i--)
                Object.Destroy(_contentFrame.GetChild(i).gameObject);
        }

        private static void Toast(string text, Color color)
        {
            if (string.IsNullOrEmpty(text) || _root == null) return;
            try
            {
                XPFloater.Spawn(_root.transform,
                    new Vector3(Screen.width / 2f, Screen.height * 0.78f, 0), text, color);
            }
            catch {}
        }

        private static string NowHM() => System.DateTime.Now.ToString("h:mm tt");

        // ── Mock data (a real backend would feed these) ──
        private static void EnsureData()
        {
            if (_world != null) return;
            Color c1 = new Color(0.85f,0.40f,0.50f), c2 = new Color(0.55f,0.85f,0.45f),
                  c3 = new Color(0.55f,0.75f,1f),   c4 = new Color(1f,0.65f,0.30f),
                  c5 = new Color(0.65f,0.55f,0.85f);

            _world = new List<Msg>
            {
                new Msg{ author="fantasyhero", text="anyone up for a co-op boss run?", time="8:30 AM", tint=c1 },
                new Msg{ author="SkyWarden",   text="just hit level 12 lets goooo", time="8:33 AM", tint=c3 },
                new Msg{ author="You", text="gg, grats!", fromMe=true, time="8:34 AM", tint=GOLD },
                new Msg{ author="MossKnight",  text="trading a rare chest, dm me", time="8:36 AM", tint=c2 },
            };
            _guild = new List<Msg>
            {
                new Msg{ author="Dragon",   text="congrats on 15lv!", time="7:12 AM", tint=c1 },
                new Msg{ author="SuperKing", text="how active is everyone this week?", time="7:40 AM", tint=c4 },
                new Msg{ author="You", text="pretty active, did the daily already", fromMe=true, time="7:41 AM", tint=GOLD },
                new Msg{ author="Quill",    text="raid tonight at 8, be there", time="7:55 AM", tint=c5 },
            };
            _dms = new List<Convo>
            {
                new Convo{ name="Aria", tint=c1, lastMsg="that boss was brutal lol", time="2m", unread=2,
                    thread=new List<Msg>{
                        new Msg{ author="Aria", text="hey! you online?", time="9:50 AM", tint=c1 },
                        new Msg{ author="You", text="yep whats up", fromMe=true, time="9:51 AM", tint=GOLD },
                        new Msg{ author="Aria", text="that boss was brutal lol", time="9:52 AM", tint=c1 },
                    }},
                new Convo{ name="Bram", tint=c2, lastMsg="thanks for the gear!", time="1h", unread=0,
                    thread=new List<Msg>{
                        new Msg{ author="You", text="sent you that spare helmet", fromMe=true, time="8:10 AM", tint=GOLD },
                        new Msg{ author="Bram", text="thanks for the gear!", time="8:14 AM", tint=c2 },
                    }},
                new Convo{ name="Dax", tint=c4, lastMsg="streak day 6!", time="3h", unread=1,
                    thread=new List<Msg>{
                        new Msg{ author="Dax", text="streak day 6!", time="6:30 AM", tint=c4 },
                    }},
            };
        }

        // ── Tiny UI helpers ──
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
            var existing = Object.FindFirstObjectByType<EventSystem>();
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
