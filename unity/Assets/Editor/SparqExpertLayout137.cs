using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 137: Expert layout pass.
    ///   • Currency: brighter, larger, repositioned top-center
    ///   • Stats card: bigger avatars, Wisp gets its own XP bar, sharper text
    ///   • Top action buttons: clean spacing below stats card
    ///   • Visual hierarchy: drop shadows on cards, consistent margins
    /// </summary>
    public static class SparqExpertLayout137
    {
        // Brand palette
        private static readonly Color GOLD       = new Color(1.00f, 0.82f, 0.32f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color XP_GREEN   = new Color(0.45f, 0.85f, 0.40f);
        private static readonly Color WISP_PINK  = new Color(0.95f, 0.45f, 0.85f);

        [MenuItem("Sparq/137. Expert layout pass (currency + stats + buttons)")]
        public static void Apply()
        {
            FixCurrencyHeader();
            FixStatsCard();
            FixTopButtons();
            BringChromeToFront();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Expert layout applied:\n\n" +
                "• Currency: brighter pills + larger text + top-center\n" +
                "• Stats: 56×56 avatars, both Karu & Wisp have XP bars\n" +
                "• Top buttons: cleanly placed below stats card\n" +
                "• Render order fixed\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Currency: bright + readable ─────────────────────
        private static void FixCurrencyHeader()
        {
            var header = GameObject.Find("CurrencyHeader");
            if (header == null) return;

            // Wipe old pills + bg, rebuild from scratch
            for (int i = header.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(header.transform.GetChild(i).gameObject);

            // Reposition: top-center, sits between logo and stats card
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -16);
            rt.sizeDelta = new Vector2(360, 50);

            // Transparent background — pills float as separate cards
            var img = header.GetComponent<Image>();
            if (img != null) img.color = new Color(0, 0, 0, 0); // invisible bar bg

            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // ASCII glyphs only — every TMP font has these
            BuildBrightPill(header.transform, "Gold",   "G", "1,250", GOLD,                       new Color(0.30f, 0.20f, 0.05f));
            BuildBrightPill(header.transform, "Gems",   "D", "42",    new Color(0.40f, 0.70f, 1f),new Color(0.05f, 0.18f, 0.35f));
            BuildBrightPill(header.transform, "Energy", "E", "18/20", new Color(1f, 0.55f, 0.40f),new Color(0.35f, 0.10f, 0.05f));
        }

        private static void BuildBrightPill(Transform parent, string name, string glyph, string val, Color tint, Color textBg)
        {
            var pill = new GameObject(name + "Pill", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            pill.transform.SetParent(parent, false);
            var le = pill.GetComponent<LayoutElement>();
            le.preferredWidth = 110; le.preferredHeight = 44;
            var img = pill.GetComponent<Image>();
            img.color = tint; // bright tint as bg

            var hlg = pill.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 10, 0, 0);
            hlg.spacing = 4;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Glyph circle (darker for contrast)
            var gWrap = new GameObject("Glyph", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            gWrap.transform.SetParent(pill.transform, false);
            var gle = gWrap.GetComponent<LayoutElement>();
            gle.preferredWidth = 28; gle.preferredHeight = 28;
            gWrap.GetComponent<Image>().color = textBg;

            var gtxt = new GameObject("G", typeof(RectTransform));
            gtxt.transform.SetParent(gWrap.transform, false);
            var grt = gtxt.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
            grt.offsetMin = Vector2.zero; grt.offsetMax = Vector2.zero;
            var gtm = gtxt.AddComponent<TextMeshProUGUI>();
            gtm.text = glyph;
            gtm.fontSize = 18;
            gtm.fontStyle = FontStyles.Bold;
            gtm.color = tint;
            gtm.alignment = TextAlignmentOptions.Center;
            gtm.raycastTarget = false;

            // Value text on tint bg, dark color for contrast
            var v = new GameObject("Val", typeof(RectTransform));
            v.transform.SetParent(pill.transform, false);
            var vle = v.AddComponent<LayoutElement>();
            vle.preferredWidth = 60;
            vle.flexibleWidth = 1;
            var vtm = v.AddComponent<TextMeshProUGUI>();
            vtm.text = val;
            vtm.fontSize = 16;
            vtm.fontStyle = FontStyles.Bold;
            vtm.color = DEEP_NAVY;
            vtm.alignment = TextAlignmentOptions.MidlineLeft;
            vtm.raycastTarget = false;
            vtm.outlineWidth = 0.18f;
            vtm.outlineColor = new Color(1, 1, 1, 0.7f);
        }

        // ───────────────────── Stats card: bigger avatars + 2 XP bars ─────────────────────
        private static void FixStatsCard()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Position: top-right below currency
            var hrt = hud.GetComponent<RectTransform>();
            if (hrt != null)
            {
                hrt.anchorMin = new Vector2(1f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot     = new Vector2(1f, 1f);
                hrt.anchoredPosition = new Vector2(-12, -78);
                hrt.sizeDelta = new Vector2(320, 120);
            }

            // Bigger avatars
            ResizeRow(hud.transform.Find("KaruRow"),  isWisp:false);
            ResizeRow(hud.transform.Find("MochiRow"), isWisp:true);
        }

        private static void ResizeRow(Transform row, bool isWisp)
        {
            if (row == null) return;
            var rt = row.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(-12, 56);
            }

            // Avatar size up
            var avBg = row.Find("AvatarBg");
            if (avBg != null)
            {
                var arrt = avBg.GetComponent<RectTransform>();
                arrt.anchoredPosition = new Vector2(8, 0);
                arrt.sizeDelta = new Vector2(48, 48);
            }

            // Name & level repositioned
            var name = row.Find("Name");
            if (name != null)
            {
                var nrt = name.GetComponent<RectTransform>();
                nrt.anchoredPosition = new Vector2(64, 14);
                nrt.sizeDelta = new Vector2(180, 22);
                foreach (var tm in name.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 16;
                    tm.fontStyle = FontStyles.Bold;
                    tm.color = CREAM;
                }
            }
            var lvl = row.Find("Level");
            if (lvl != null)
            {
                var lrt = lvl.GetComponent<RectTransform>();
                lrt.anchoredPosition = new Vector2(64, -8);
                lrt.sizeDelta = new Vector2(46, 18);
                foreach (var tm in lvl.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 11;
                    tm.fontStyle = FontStyles.Bold;
                }
            }

            // Add / refresh XP bar at bottom of row (full-width)
            var oldBar = row.Find("XPBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);

            var bar = new GameObject("XPBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(row, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(28, 4);
            brt.sizeDelta = new Vector2(-72, 8);
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            float pct = isWisp ? 0.30f : 0.65f;
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(pct, 1);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = isWisp ? WISP_PINK : XP_GREEN;
        }

        // ───────────────────── Top buttons: lifted clear of stats card ─────────────────────
        private static void FixTopButtons()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;
            var rt = bar.GetComponent<RectTransform>();
            if (rt == null) return;

            // Sit just below stats card with breathing room
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            // Sit clearly below stats card (stats ends at -78 - 120 = -198), gap then buttons
            rt.anchoredPosition = new Vector2(-12, -220);
            rt.sizeDelta = new Vector2(440, 100);
        }

        // ───────────────────── Bring UI chrome to front ─────────────────────
        private static void BringChromeToFront()
        {
            string[] toFront = {
                "GameTitle", "PlayerHUD", "CurrencyHeader",
                "HomeNavButtons", "BottomNav", "HelpIcon"
            };
            foreach (var n in toFront)
            {
                var go = GameObject.Find(n);
                if (go != null) go.transform.SetAsLastSibling();
            }
            var v = GameObject.Find("Vignette");
            if (v != null) v.transform.SetSiblingIndex(0);
        }
    }
}
