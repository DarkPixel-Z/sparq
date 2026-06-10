using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Modal viewer for a single journal entry. Shows full text, plays voice
    /// (if attached), and offers Edit/Delete actions. Adventure-tome styling.
    /// </summary>
    public static class JournalEntryViewer
    {
        // Adventure parchment palette
        private static readonly Color PARCHMENT     = new Color(0.96f, 0.88f, 0.70f, 1f);
        private static readonly Color PARCHMENT_DK  = new Color(0.86f, 0.74f, 0.50f, 1f);
        private static readonly Color BROWN         = new Color(0.42f, 0.22f, 0.08f, 1f);
        private static readonly Color BROWN_DK      = new Color(0.20f, 0.10f, 0.04f, 1f);
        private static readonly Color GOLD          = new Color(0.90f, 0.72f, 0.18f, 1f);
        private static readonly Color INK           = new Color(0.18f, 0.10f, 0.04f, 1f);

        private static GameObject _root;
        private static AudioSource _audio;

        public static void Show(Sparq.Systems.JournalService.Entry entry)
        {
            if (entry == null) return;
            if (_root != null) Hide();

            _root = new GameObject("JournalEntryViewer",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(AudioSource));
            _audio = _root.GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14850;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim
            var dim = MakeImage(_root.transform, "Dim", new Color(0, 0, 0, 0.92f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // Outer brown leather frame
            var frame = MakeRounded(_root.transform, "Frame", BROWN_DK, 28);
            var frt = frame.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 0.5f); frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(900, 1300);

            // Inner parchment page
            var page = MakeRounded(frame.transform, "Page", PARCHMENT, 22);
            var pRT = page.GetComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
            pRT.offsetMin = new Vector2(20, 20); pRT.offsetMax = new Vector2(-20, -20);

            // Top header band (brown wood) with date
            var hdr = MakeRounded(page.transform, "Hdr", BROWN, 16);
            var hRT = hdr.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -10);
            hRT.sizeDelta = new Vector2(-20, 88);

            var dt = System.DateTimeOffset.FromUnixTimeSeconds(entry.unix).LocalDateTime;
            string headerText = dt.ToString("MMMM d, yyyy") + "   ·   " + dt.ToString("h:mm tt");
            var hTm = MakeText(hdr.transform, "T", headerText,
                26, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hTm.alignment = TextAlignmentOptions.Center;
            hTm.outlineWidth = 0.30f;
            hTm.outlineColor = BROWN_DK;

            // Lock badge if locked
            if (entry.locked)
            {
                var badge = MakeRounded(hdr.transform, "Lock", GOLD, 16);
                var brt = badge.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
                brt.pivot = new Vector2(1, 0.5f);
                brt.anchoredPosition = new Vector2(-12, 0);
                brt.sizeDelta = new Vector2(56, 56);
                var bImg = badge.GetComponent<Image>();
                bImg.sprite = LoadCircleSprite();
                bImg.type = Image.Type.Simple;
                var lockTm = MakeText(badge.transform, "L", "✦",
                    32, FontStyles.Bold, BROWN_DK,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                lockTm.alignment = TextAlignmentOptions.Center;
            }

            // Decorative divider
            var divider = MakeImage(page.transform, "Div", new Color(BROWN.r, BROWN.g, BROWN.b, 0.35f));
            var dvRT = divider.GetComponent<RectTransform>();
            dvRT.anchorMin = new Vector2(0.1f, 1); dvRT.anchorMax = new Vector2(0.9f, 1);
            dvRT.pivot = new Vector2(0.5f, 1);
            dvRT.anchoredPosition = new Vector2(0, -110);
            dvRT.sizeDelta = new Vector2(0, 2);

            // Body text — parchment-style, scrollable
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(page.transform, false);
            var scrt = scrollGO.GetComponent<RectTransform>();
            scrt.anchorMin = new Vector2(0, 0); scrt.anchorMax = new Vector2(1, 1);
            scrt.offsetMin = new Vector2(40, 220); scrt.offsetMax = new Vector2(-40, -130);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            var viewport = new GameObject("Vp", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0); // transparent
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            sr.viewport = vrt;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cct = content.GetComponent<RectTransform>();
            cct.anchorMin = new Vector2(0, 1); cct.anchorMax = new Vector2(1, 1);
            cct.pivot = new Vector2(0.5f, 1);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childForceExpandWidth = true; vlg.childControlWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = cct;

            // The actual text
            var bodyGO = new GameObject("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(content.transform, false);
            bodyGO.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var bodyTm = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyTm.text = string.IsNullOrEmpty(entry.text) ? "<i>(voice only — tap PLAY VOICE below)</i>" : entry.text;
            bodyTm.richText = true;
            bodyTm.fontSize = 28;
            bodyTm.color = INK;
            bodyTm.alignment = TextAlignmentOptions.TopLeft;
            bodyTm.font = TMP_Settings.defaultFontAsset;
            bodyTm.textWrappingMode = TextWrappingModes.Normal;

            // ── Bottom action row: PLAY VOICE / DELETE / CLOSE ──
            float btnY = 80;
            bool hasVoice = !string.IsNullOrEmpty(entry.voicePath);
            if (hasVoice)
            {
                var play = MakeBtn(page.transform, "Play", "▶  PLAY VOICE",
                    new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, btnY), new Vector2(280, 90),
                    BROWN, new Color(1f, 0.92f, 0.55f), 22);
                ApplyRound(play);
                var pLbl = play.transform.Find("Lbl")?.GetComponent<TMP_Text>();
                if (pLbl != null) { pLbl.outlineWidth = 0.25f; pLbl.outlineColor = BROWN_DK; }
                play.onClick.AddListener(() => PlayVoice(entry.voicePath));
            }

            // Close (right side)
            var close = MakeBtn(page.transform, "Close", "✓  CLOSE",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, btnY), new Vector2(220, 90),
                new Color(0.30f, 0.65f, 0.35f), Color.white, 24);
            ApplyRound(close);
            var cLbl = close.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (cLbl != null) { cLbl.outlineWidth = 0.22f; cLbl.outlineColor = new Color(0.10f, 0.20f, 0.10f); }
            close.onClick.AddListener(Hide);

            // Delete (center bottom, smaller)
            var del = MakeBtn(page.transform, "Del", "DELETE",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, btnY), new Vector2(180, 70),
                new Color(0.55f, 0.20f, 0.20f), Color.white, 18);
            ApplyRound(del);
            var dLbl = del.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (dLbl != null) { dLbl.outlineWidth = 0.22f; dLbl.outlineColor = new Color(0.10f, 0.05f, 0.05f); }
            del.onClick.AddListener(() =>
            {
                Sparq.Systems.JournalService.Delete(entry.id);
                Hide();
            });
        }

        public static void Hide()
        {
            if (_audio != null && _audio.isPlaying) _audio.Stop();
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _audio = null;
        }

        private static void PlayVoice(string path)
        {
            if (_audio == null) return;
            var clip = Sparq.Systems.JournalService.LoadVoice(path);
            if (clip == null) { Debug.LogWarning("[JournalViewer] Voice clip not found: " + path); return; }
            _audio.Stop();
            _audio.clip = clip;
            _audio.Play();
        }

        // ─── helpers ───
        private static void ApplyRound(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) { img.sprite = LoadRoundedSprite(20); img.type = Image.Type.Sliced; }
        }

        private static System.Collections.Generic.Dictionary<int, Sprite> _roundedCache = new System.Collections.Generic.Dictionary<int, Sprite>();
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
        private static Sprite _circleSp;
        private static Sprite LoadCircleSprite()
        {
            if (_circleSp != null) return _circleSp;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
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
