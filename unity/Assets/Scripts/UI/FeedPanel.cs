using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Social Feed — scrollable timeline of friends' / rivals' recent activity.
    /// Mock data for now (would be wired to a real backend later).
    /// Matches QuestsPanel/JournalPanel/ProfilePanel/RemindPanel visual language.
    /// </summary>
    public static class FeedPanel
    {
        private static readonly Color GOLD       = new Color(1f, 0.82f, 0.30f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.13f, 0.28f);
        private static readonly Color CARD_BG    = new Color(0.22f, 0.20f, 0.40f, 1f);
        private static readonly Color TITLE_BG   = new Color(0.42f, 0.22f, 0.68f, 1f);
        private static readonly Color BANNER_BG  = new Color(0.30f, 0.18f, 0.46f, 1f);
        private static readonly Color ROW_BG     = new Color(0.36f, 0.32f, 0.60f, 1f);

        private static GameObject _root;
        private static readonly Dictionary<int, Sprite> _roundedCache = new Dictionary<int, Sprite>();
        private static Sprite _circleSp;

        // Mock activity feed — would come from a real backend
        private struct FeedItem
        {
            public string actor;
            public Color  actorTint;
            public string letter;
            public string activity;
            public string when;
            public Color  pillTint;
            public string pillLabel;
        }

        private static readonly FeedItem[] FEED = new[]
        {
            new FeedItem { actor="Fitch",   actorTint=new Color(0.95f, 0.45f, 0.50f), letter="F",
                           activity="just completed <b>5 quests</b> today!",
                           when="2 m ago", pillTint=new Color(1f, 0.55f, 0.30f), pillLabel="+125 XP" },
            new FeedItem { actor="Maris",   actorTint=new Color(0.55f, 0.85f, 0.45f), letter="M",
                           activity="reached <b>Level 7</b> 🎉",
                           when="14 m ago", pillTint=GOLD, pillLabel="LV UP" },
            new FeedItem { actor="Kael",    actorTint=new Color(0.55f, 0.62f, 0.95f), letter="K",
                           activity="hit a <b>10-day streak</b>!",
                           when="1 h ago", pillTint=new Color(0.95f, 0.55f, 0.35f), pillLabel="STREAK" },
            new FeedItem { actor="Una",     actorTint=new Color(0.85f, 0.55f, 0.95f), letter="U",
                           activity="earned <b>Quest Master</b> achievement.",
                           when="3 h ago", pillTint=GOLD, pillLabel="MEDAL" },
            new FeedItem { actor="Fitch",   actorTint=new Color(0.95f, 0.45f, 0.50f), letter="F",
                           activity="defeated <b>Forest Goblin</b> in 2 hits.",
                           when="5 h ago", pillTint=new Color(0.95f, 0.45f, 0.45f), pillLabel="VICTORY" },
            new FeedItem { actor="Pip",     actorTint=new Color(0.55f, 0.85f, 0.85f), letter="P",
                           activity="logged <b>Calm</b> in their journal.",
                           when="yesterday", pillTint=new Color(0.55f, 0.85f, 1f), pillLabel="JOURNAL" },
            new FeedItem { actor="Maris",   actorTint=new Color(0.55f, 0.85f, 0.45f), letter="M",
                           activity="found a <b>Legendary</b> trinket.",
                           when="yesterday", pillTint=new Color(0.95f, 0.65f, 0.15f), pillLabel="LOOT" },
            new FeedItem { actor="Kael",    actorTint=new Color(0.55f, 0.62f, 0.95f), letter="K",
                           activity="finished <b>Chapter 1</b>.",
                           when="2 d ago", pillTint=new Color(0.55f, 0.85f, 0.45f), pillLabel="STAGE" },
        };

        public static void Show()
        {
            if (_root != null) { Hide(); return; }

            _root = new GameObject("FeedPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14600;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim
            var dim = MakeImage(_root.transform, "Dim", new Color(0, 0, 0, 0.85f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // Stroke
            var stroke = MakeRounded(_root.transform, "Stroke", TITLE_BG, 30);
            var srt = stroke.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(36, 136); srt.offsetMax = new Vector2(-36, -76);

            // Card
            var card = MakeRounded(_root.transform, "Card", CARD_BG, 28);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 1);
            crt.offsetMin = new Vector2(40, 140); crt.offsetMax = new Vector2(-40, -80);

            BuildFunkyBackdrop(card.transform);

            // Title shadow + bar
            var titleShadow = MakeRounded(card.transform, "TitleShadow", new Color(0, 0, 0, 0.35f), 24);
            var tshrt = titleShadow.GetComponent<RectTransform>();
            tshrt.anchorMin = new Vector2(0, 1); tshrt.anchorMax = new Vector2(1, 1);
            tshrt.pivot = new Vector2(0.5f, 1f);
            tshrt.anchoredPosition = new Vector2(0, -26);
            tshrt.sizeDelta = new Vector2(-40, 110);

            var titleBar = MakeRounded(card.transform, "TitleBar", TITLE_BG, 24);
            var tbrt = titleBar.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = new Vector2(1, 1);
            tbrt.pivot = new Vector2(0.5f, 1f);
            tbrt.anchoredPosition = new Vector2(0, -20);
            tbrt.sizeDelta = new Vector2(-40, 110);

            var title = MakeText(titleBar.transform, "Title", "FEED",
                52, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            title.alignment = TextAlignmentOptions.Center;
            title.outlineWidth = 0.28f;
            title.outlineColor = new Color(0.45f, 0.05f, 0.22f, 1f);

            // Back
            var backBtn = MakeBtn(card.transform, "BackBtn", "←  BACK",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-115, -55), new Vector2(190, 80),
                GOLD, DEEP_NAVY, 28);
            backBtn.onClick.AddListener(Hide);
            var bImg = backBtn.GetComponent<Image>();
            bImg.sprite = LoadRoundedSprite(28); bImg.type = Image.Type.Sliced;
            var bLbl = backBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bLbl != null) { bLbl.fontStyle = FontStyles.Bold; bLbl.outlineWidth = 0.22f; bLbl.outlineColor = new Color(1f, 0.95f, 0.7f); }

            // ── Rivalry banner ──
            int playerXP = 0, fitchXP = 0;
            try
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null)
                {
                    playerXP = data.totalXP;
                    var f = data.GetType().GetField("fitchXP");
                    if (f != null) fitchXP = (int)f.GetValue(data);
                }
            } catch {}
            int lead = playerXP - fitchXP;

            var banner = MakeRounded(card.transform, "Rivalry", BANNER_BG, 18);
            var brrt = banner.GetComponent<RectTransform>();
            brrt.anchorMin = new Vector2(0, 1); brrt.anchorMax = new Vector2(1, 1);
            brrt.pivot = new Vector2(0.5f, 1f);
            brrt.anchoredPosition = new Vector2(0, -150);
            brrt.sizeDelta = new Vector2(-50, 110);

            string leadStr = lead >= 0 ? $"+{lead} ahead" : $"{lead} behind";
            Color leadColor = lead >= 0 ? new Color(0.55f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.50f);
            var rivalryTm = MakeText(banner.transform, "RivalTxt", $"⚔  Karu vs Fitch  ·  <color=#{ColorUtility.ToHtmlStringRGB(leadColor)}>{leadStr}</color>",
                30, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            rivalryTm.alignment = TextAlignmentOptions.Center;
            rivalryTm.outlineWidth = 0.20f;
            rivalryTm.outlineColor = new Color(0.05f, 0.02f, 0.08f);

            // Section header
            var hdr = MakeText(card.transform, "Hdr", "·  ACTIVITY  ·",
                22, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -290), new Vector2(0, 32));
            hdr.alignment = TextAlignmentOptions.Center;
            hdr.characterSpacing = 12f;
            hdr.outlineWidth = 0.18f; hdr.outlineColor = new Color(0.10f, 0.05f, 0);

            // Scroll list
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(card.transform, false);
            var scrt = scrollGO.GetComponent<RectTransform>();
            scrt.anchorMin = new Vector2(0, 0); scrt.anchorMax = new Vector2(1, 1);
            scrt.offsetMin = new Vector2(30, 140); scrt.offsetMax = new Vector2(-30, -340);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 35f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            var vpImg = viewport.GetComponent<Image>();
            vpImg.sprite = LoadRoundedSprite(20); vpImg.type = Image.Type.Sliced;
            vpImg.color = new Color(0, 0, 0, 0.25f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;
            sr.viewport = vrt;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cct = content.GetComponent<RectTransform>();
            cct.anchorMin = new Vector2(0, 1); cct.anchorMax = new Vector2(1, 1);
            cct.pivot = new Vector2(0.5f, 1f);
            cct.anchoredPosition = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 14;
            vlg.childForceExpandWidth = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = cct;

            foreach (var item in FEED) BuildItem(content.transform, item);
        }

        public static void Hide()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
        }

        private static void BuildItem(Transform parent, FeedItem it)
        {
            var row = MakeRounded(parent, $"F_{it.actor}_{it.when}", ROW_BG, 16);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 140; le.minHeight = 140;

            // Avatar disc on left — colored circle with single letter
            var av = MakeRounded(row.transform, "Av", it.actorTint, 36);
            var avRT = av.GetComponent<RectTransform>();
            avRT.anchorMin = new Vector2(0, 0.5f); avRT.anchorMax = new Vector2(0, 0.5f);
            avRT.pivot = new Vector2(0, 0.5f);
            avRT.anchoredPosition = new Vector2(20, 0);
            avRT.sizeDelta = new Vector2(86, 86);
            var avImg = av.GetComponent<Image>();
            avImg.sprite = LoadCircleSprite();
            avImg.type = Image.Type.Simple;

            // Cream halo ring around the avatar
            var halo = MakeRounded(av.transform, "Halo", new Color(1f, 0.97f, 0.85f, 0.55f), 36);
            var hRT = halo.GetComponent<RectTransform>();
            hRT.anchorMin = Vector2.zero; hRT.anchorMax = Vector2.one;
            hRT.offsetMin = new Vector2(-3, -3); hRT.offsetMax = new Vector2(3, 3);
            var hImg = halo.GetComponent<Image>();
            hImg.sprite = LoadCircleSprite(); hImg.type = Image.Type.Simple;
            hImg.raycastTarget = false;
            halo.transform.SetAsFirstSibling();

            var letter = MakeText(av.transform, "L", it.letter,
                42, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            letter.alignment = TextAlignmentOptions.Center;
            letter.outlineWidth = 0.30f;
            letter.outlineColor = new Color(0, 0, 0, 0.85f);

            // Actor name (top of right column)
            var nameTm = MakeText(row.transform, "Name", it.actor,
                28, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            nameTm.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = nameTm.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(125, 0); nRT.offsetMax = new Vector2(-180, -10);
            nameTm.outlineWidth = 0.20f;
            nameTm.outlineColor = new Color(0.05f, 0.02f, 0.08f);

            // Activity (rich text, bottom of right column)
            var actTm = MakeText(row.transform, "Act", it.activity,
                22, FontStyles.Normal, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            actTm.alignment = TextAlignmentOptions.MidlineLeft;
            var aRT = actTm.rectTransform;
            aRT.anchorMin = new Vector2(0, 0); aRT.anchorMax = new Vector2(1, 0.55f);
            aRT.pivot = new Vector2(0, 0.5f);
            aRT.offsetMin = new Vector2(125, 12); aRT.offsetMax = new Vector2(-180, 0);
            actTm.richText = true;
            actTm.outlineWidth = 0.18f;
            actTm.outlineColor = new Color(0, 0, 0, 0.7f);

            // Pill chip (top right)
            var pill = MakeRounded(row.transform, "Pill", it.pillTint, 14);
            var pRT = pill.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(1, 0.55f); pRT.anchorMax = new Vector2(1, 1);
            pRT.pivot = new Vector2(1, 0.5f);
            pRT.anchoredPosition = new Vector2(-18, -12);
            pRT.sizeDelta = new Vector2(150, 50);
            var pillTm = MakeText(pill.transform, "PT", it.pillLabel,
                20, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            pillTm.alignment = TextAlignmentOptions.Center;
            pillTm.outlineWidth = 0.22f;
            pillTm.outlineColor = new Color(1f, 0.95f, 0.7f);

            // When (bottom right)
            var whenTm = MakeText(row.transform, "When", it.when,
                18, FontStyles.Italic, new Color(1, 1, 1, 0.65f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            whenTm.alignment = TextAlignmentOptions.MidlineRight;
            var wRT = whenTm.rectTransform;
            wRT.anchorMin = new Vector2(1, 0); wRT.anchorMax = new Vector2(1, 0.55f);
            wRT.pivot = new Vector2(1, 0.5f);
            wRT.anchoredPosition = new Vector2(-18, 12);
            wRT.sizeDelta = new Vector2(150, 36);
        }

        // ─────────── Funky pastel backdrop (same as RemindPanel) ───────────
        private static void BuildFunkyBackdrop(Transform card)
        {
            var mask = new GameObject("FunkyMask",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            mask.transform.SetParent(card, false);
            var mrt = mask.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            var mImg = mask.GetComponent<Image>();
            mImg.sprite = LoadRoundedSprite(28); mImg.type = Image.Type.Sliced;
            mImg.color = Color.white;
            mask.GetComponent<Mask>().showMaskGraphic = false;

            (float ax, float ay, float size, Color col)[] blobs = {
                (0.10f, 0.95f, 320, new Color(0.42f, 0.22f, 0.68f, 0.22f)),
                (0.95f, 0.78f, 280, new Color(1.00f, 0.82f, 0.30f, 0.20f)),
                (0.60f, 0.55f, 380, new Color(0.45f, 0.85f, 0.65f, 0.18f)),
                (0.05f, 0.40f, 260, new Color(0.55f, 0.62f, 0.95f, 0.22f)),
                (0.85f, 0.18f, 320, new Color(0.92f, 0.55f, 0.85f, 0.22f)),
                (0.25f, 0.10f, 240, new Color(0.55f, 0.85f, 1.00f, 0.20f)),
            };
            foreach (var b in blobs)
            {
                var go = new GameObject("Blob", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(mask.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(b.ax, b.ay); rt.anchorMax = new Vector2(b.ax, b.ay);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(b.size, b.size);
                var img = go.GetComponent<Image>();
                img.sprite = LoadCircleSprite(); img.color = b.col; img.raycastTarget = false;
            }

            for (int i = 0; i < 40; i++)
            {
                var dot = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(mask.transform, false);
                var rt = dot.GetComponent<RectTransform>();
                float ax = ((i * 73) % 100) / 100f;
                float ay = ((i * 47 + 13) % 100) / 100f;
                rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float s = 4 + (i % 5) * 2;
                rt.sizeDelta = new Vector2(s, s);
                var img = dot.GetComponent<Image>();
                img.sprite = LoadCircleSprite();
                img.color = new Color(1f, 0.97f, 0.65f, 0.22f + (i % 4) * 0.05f);
                img.raycastTarget = false;
            }

            mask.transform.SetAsFirstSibling();
        }

        // ─────────── helpers ───────────
        private static Sprite LoadCircleSprite()
        {
            if (_circleSp != null) return _circleSp;
            const int s = 96;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.48f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            _circleSp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _circleSp;
        }
        private static Sprite LoadRoundedSprite(int radius)
        {
            if (_roundedCache.TryGetValue(radius, out var sp) && sp != null) return sp;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside;
                int dx = 0, dy = 0;
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
        private static GameObject MakeRounded(Transform parent, string name, Color color, int radius)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(radius); img.type = Image.Type.Sliced; img.color = color;
            return go;
        }
        private static GameObject MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
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
            rt.anchoredPosition = anch; rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
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
            rt.anchoredPosition = anch; rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;
            var t = new GameObject("Lbl", typeof(RectTransform));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tm = t.AddComponent<TextMeshProUGUI>();
            tm.text = label; tm.fontSize = fontSize; tm.fontStyle = FontStyles.Bold;
            tm.color = fg; tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset; tm.raycastTarget = false;
            return go.GetComponent<Button>();
        }
    }
}
