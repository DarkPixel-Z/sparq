using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 131: Pro polish pass — adds currency bar, XP bar, mock quests,
    /// active-tab highlight, vignette, ground shadow, card shadows.
    /// Purely additive — does not touch existing button choices.
    /// </summary>
    public static class SparqProPolish
    {
        // Brand palette
        private static readonly Color GOLD       = new Color(1.00f, 0.82f, 0.32f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f, 0.92f);
        private static readonly Color RED_ALERT  = new Color(0.85f, 0.25f, 0.30f);
        private static readonly Color XP_GREEN   = new Color(0.45f, 0.85f, 0.40f);

        [MenuItem("Sparq/131. Pro polish pass (currency + XP + quests + tabs + vignette)")]
        public static void Apply()
        {
            // Prefer "UI Canvas" if present, else any overlay canvas
            Canvas canvas = null;
            var named = GameObject.Find("UI Canvas");
            if (named != null) canvas = named.GetComponent<Canvas>();
            if (canvas == null)
            {
                foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                    { canvas = c; break; }
                }
            }
            if (canvas == null) { EditorUtility.DisplayDialog("Sparq", "No Canvas found.", "OK"); return; }

            BuildCurrencyHeader(canvas.transform);
            ShiftStatsDown();
            AddXPBarToStats();
            FillQuestBox();
            HighlightActiveTab();
            AddVignette(canvas.transform);
            AddGroundShadow();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Pro polish pass applied:\n\n" +
                "• Currency header (gold / gems / energy)\n" +
                "• XP bar under Karu\n" +
                "• 3 mock quest items rendered\n" +
                "• Bottom nav active-tab highlight\n" +
                "• Soft vignette overlay\n" +
                "• Ground shadow under hero\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Currency header ─────────────────────
        private static void BuildCurrencyHeader(Transform canvas)
        {
            var old = GameObject.Find("CurrencyHeader");
            if (old != null) Object.DestroyImmediate(old);

            var header = new GameObject("CurrencyHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(canvas, false);
            header.transform.SetAsLastSibling(); // render on top of everything else in canvas
            // Top-right, just above the stats card — never overlaps logo
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12, -8);
            rt.sizeDelta = new Vector2(280, 36);
            rt.localScale = Vector3.one;

            var bg = header.GetComponent<Image>();
            bg.color = DEEP_NAVY;

            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 3, 3);
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Use ASCII glyphs that exist in every TMP font
            BuildPill(header.transform, "GoldPill",   "*", "1,250", GOLD);
            BuildPill(header.transform, "GemsPill",   "+", "42",    new Color(0.55f, 0.75f, 1f));
            BuildPill(header.transform, "EnergyPill", "/", "18/20", new Color(1f, 0.55f, 0.45f));
        }

        private static void BuildPill(Transform parent, string name, string glyph, string value, Color accent)
        {
            var pill = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            pill.transform.SetParent(parent, false);
            var le = pill.GetComponent<LayoutElement>();
            le.preferredWidth = 78; le.preferredHeight = 30;
            var img = pill.GetComponent<Image>();
            img.color = new Color(0.18f, 0.14f, 0.28f, 0.95f); // lighter than header bg so pills pop

            var hlg = pill.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(pill.transform, false);
            var icTM = icon.AddComponent<TextMeshProUGUI>();
            icTM.text = glyph;
            icTM.fontSize = 20;
            icTM.fontStyle = FontStyles.Bold;
            icTM.color = accent;
            icTM.alignment = TextAlignmentOptions.Center;
            icTM.raycastTarget = false;
            var ile = icon.AddComponent<LayoutElement>();
            ile.preferredWidth = 18;

            var val = new GameObject("Value", typeof(RectTransform));
            val.transform.SetParent(pill.transform, false);
            var vTM = val.AddComponent<TextMeshProUGUI>();
            vTM.text = value;
            vTM.fontSize = 14;
            vTM.fontStyle = FontStyles.Bold;
            vTM.color = CREAM;
            vTM.alignment = TextAlignmentOptions.MidlineLeft;
            vTM.raycastTarget = false;
        }

        // ───────────────────── Stats card position ─────────────────────
        private static void ShiftStatsDown()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;
            var rt = hud.GetComponent<RectTransform>();
            if (rt == null) return;
            // Idempotent: if y is higher than -52, push it down to ~-52 so currency bar fits above
            var p = rt.anchoredPosition;
            if (p.y > -52f) rt.anchoredPosition = new Vector2(p.x, -52f);
        }

        // ───────────────────── XP bar under Karu ─────────────────────
        private static void AddXPBarToStats()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;
            var karuRow = hud.transform.Find("KaruRow");
            if (karuRow == null) return;

            var old = karuRow.Find("XPBar");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var bar = new GameObject("XPBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(karuRow, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0, 2);
            brt.sizeDelta = new Vector2(-12, 6);
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0.65f, 1); // 65% mock
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = XP_GREEN;
        }

        // ───────────────────── Quest box mock items ─────────────────────
        private static void FillQuestBox()
        {
            var list = GameObject.Find("QuestList");
            if (list == null) return;

            // Wipe rows
            for (int i = list.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(list.transform.GetChild(i).gameObject);

            // Make sure VLG exists
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = list.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 32, 8);
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            (string title, string glyph, float pct, string reward, Color tint)[] quests = new (string, string, float, string, Color)[]
            {
                ("Slay 3 forest goblins",  "!", 0.66f, "+25 XP",   RED_ALERT),
                ("Read for 20 minutes",    "B", 0.40f, "+10 GOLD", GOLD),
                ("Stretch break (5 min)",  "Y", 0.00f, "+1 GEM",   new Color(0.55f, 0.75f, 1f)),
            };
            foreach (var q in quests) BuildQuestRow(list.transform, q.title, q.glyph, q.pct, q.reward, q.tint);
        }

        private static void BuildQuestRow(Transform parent, string title, string glyph, float pct, string reward, Color tint)
        {
            var row = new GameObject("QuestRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 38;
            row.GetComponent<Image>().color = new Color(0.18f, 0.12f, 0.06f, 0.45f);

            // Glyph circle
            var g = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(row.transform, false);
            var grt = g.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.pivot = new Vector2(0, 0.5f);
            grt.anchoredPosition = new Vector2(8, 0);
            grt.sizeDelta = new Vector2(32, 32);
            g.GetComponent<Image>().color = new Color(tint.r, tint.g, tint.b, 0.85f);

            var gl = new GameObject("Sym", typeof(RectTransform));
            gl.transform.SetParent(g.transform, false);
            var glrt = gl.GetComponent<RectTransform>();
            glrt.anchorMin = Vector2.zero; glrt.anchorMax = Vector2.one;
            glrt.offsetMin = Vector2.zero; glrt.offsetMax = Vector2.zero;
            var glTM = gl.AddComponent<TextMeshProUGUI>();
            glTM.text = glyph;
            glTM.fontSize = 22;
            glTM.alignment = TextAlignmentOptions.Center;
            glTM.color = CREAM;
            glTM.raycastTarget = false;

            // Title
            var t = new GameObject("Title", typeof(RectTransform));
            t.transform.SetParent(row.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0, 1);
            trt.anchoredPosition = new Vector2(56, -6);
            trt.sizeDelta = new Vector2(-120, 22);
            var tTM = t.AddComponent<TextMeshProUGUI>();
            tTM.text = title;
            tTM.fontSize = 14;
            tTM.fontStyle = FontStyles.Bold;
            tTM.color = CREAM;
            tTM.alignment = TextAlignmentOptions.MidlineLeft;
            tTM.raycastTarget = false;

            // Progress bar
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(row.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(28, 8);
            brt.sizeDelta = new Vector2(-180, 8);
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = tint;

            // Reward
            var r = new GameObject("Reward", typeof(RectTransform));
            r.transform.SetParent(row.transform, false);
            var rrt = r.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(1, 0.5f); rrt.anchorMax = new Vector2(1, 0.5f);
            rrt.pivot = new Vector2(1, 0.5f);
            rrt.anchoredPosition = new Vector2(-10, 0);
            rrt.sizeDelta = new Vector2(70, 24);
            var rTM = r.AddComponent<TextMeshProUGUI>();
            rTM.text = reward;
            rTM.fontSize = 13;
            rTM.fontStyle = FontStyles.Bold;
            rTM.color = GOLD;
            rTM.alignment = TextAlignmentOptions.MidlineRight;
            rTM.raycastTarget = false;
        }

        // ───────────────────── Bottom nav active highlight ─────────────────────
        private static void HighlightActiveTab()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            // Default active = Home (first non-ignoreLayout child)
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var child = bar.transform.GetChild(i);
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;

                bool isHome = child.name.Contains("Home");
                var img = child.GetComponentInChildren<Image>(true);
                var tm = child.GetComponentInChildren<TMP_Text>(true);
                if (img != null)
                    img.color = isHome ? Color.white : new Color(0.65f, 0.65f, 0.7f, 0.85f);
                if (tm != null)
                {
                    // Active = dark text on bright button (high contrast); inactive = dim cream
                    tm.color = isHome ? DEEP_NAVY : new Color(1f, 0.95f, 0.82f, 0.7f);
                    tm.fontStyle = isHome ? FontStyles.Bold : FontStyles.Normal;
                }
                child.localScale = Vector3.one;
            }
        }

        // ───────────────────── Vignette overlay ─────────────────────
        private static void AddVignette(Transform canvas)
        {
            var old = GameObject.Find("Vignette");
            if (old != null) Object.DestroyImmediate(old);

            var v = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            v.transform.SetParent(canvas, false);
            v.transform.SetAsFirstSibling();           // behind UI but above background camera
            var rt = v.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var tex = BuildRadialVignetteTex(512, 512);
            var sp = Sprite.Create(tex, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
            var img = v.GetComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            img.color = new Color(1, 1, 1, 0.55f);
            // Push behind every UI element but in front of world
            v.transform.SetSiblingIndex(0);
        }

        private static Texture2D BuildRadialVignetteTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
            float maxD = c.magnitude;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.45f));
                tex.SetPixel(x, y, new Color(0, 0, 0, a));
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        // ───────────────────── Ground shadow under hero ─────────────────────
        private static void AddGroundShadow()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null) return;

            var existing = karu.transform.Find("GroundShadow");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var shadow = new GameObject("GroundShadow");
            shadow.transform.SetParent(karu.transform, false);
            shadow.transform.localPosition = new Vector3(0, -1.6f, 0);
            shadow.transform.localScale = new Vector3(2.4f, 0.8f, 1f);

            var sr = shadow.AddComponent<SpriteRenderer>();
            sr.sprite = BuildEllipseSprite();
            sr.color = new Color(0, 0, 0, 0.45f);
            sr.sortingOrder = 48; // just below Karu
        }

        private static Sprite BuildEllipseSprite()
        {
            int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x - c.x) / c.x;
                float dy = (y - c.y) / c.y;
                float d = Mathf.Sqrt(dx*dx + dy*dy);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.SmoothStep(0f, 1f, a);
                tex.SetPixel(x, y, new Color(0, 0, 0, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
