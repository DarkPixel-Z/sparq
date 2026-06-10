using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 138: Pro currency bar + stats card upgrade
    /// using actual Layer Lab GUI Pro-FantasyHero assets:
    ///   • Bubble pills with real coin / gem / energy sprites
    ///   • Slider-style XP bars on stats card with border + fill sprites
    /// </summary>
    public static class SparqProCurrency138
    {
        private const string FH_ICON  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/";
        private const string FH_LABEL = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Label/";
        private const string FH_SLIDER= "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Slider/";

        private static readonly Color CREAM     = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/138. Pro currency + stats bar (real fantasy sprites)")]
        public static void Apply()
        {
            EnsureSprite(FH_LABEL  + "Label_Bubble_01_Bg.png");
            EnsureSprite(FH_ICON   + "ItemIcon_Coin_Gold.png");
            EnsureSprite(FH_ICON   + "ItemIcon_Gem_Diamond_Blue.png");
            EnsureSprite(FH_ICON   + "ItemIcon_Energy_Purple.png");
            EnsureSprite(FH_SLIDER + "Slider_Border_Rectangle_01_Bg.png");
            EnsureSprite(FH_SLIDER + "Slider_Border_Rectangle_01_Border.png");
            EnsureSprite(FH_SLIDER + "Slider_Border_Rectangle_01_Fill_Yellow.png");
            EnsureSprite(FH_SLIDER + "Slider_Border_Rectangle_01_Fill_Blue.png");

            BuildCurrency();
            UpgradeStatsCard();
            DockTopButtonsUnderStats();
            BringChromeToFront();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Currency + stats upgraded:\n\n" +
                "• Bubble pills with real Coin / Gem / Energy sprites\n" +
                "• XP bars now use proper Slider sprites (border + fill)\n" +
                "• Karu = yellow fill, Wisp = blue fill\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Currency bar ─────────────────────
        private static void BuildCurrency()
        {
            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null)
            {
                var c = Object.FindAnyObjectByType<Canvas>();
                if (c != null) canvas = c.gameObject;
            }
            if (canvas == null) return;

            var old = GameObject.Find("CurrencyHeader");
            if (old != null) Object.DestroyImmediate(old);

            var header = new GameObject("CurrencyHeader", typeof(RectTransform));
            header.transform.SetParent(canvas.transform, false);
            header.transform.SetAsLastSibling();

            var rt = header.GetComponent<RectTransform>();
            // Anchor top-LEFT, sit just to the right of the logo
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(296, -28); // logo ends ~288, gap, then pills
            rt.sizeDelta = new Vector2(380, 52);

            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var bubbleBg = AssetDatabase.LoadAssetAtPath<Sprite>(FH_LABEL + "Label_Bubble_01_Bg.png");
            BuildPill(header.transform, "Gold",
                bubbleBg,
                AssetDatabase.LoadAssetAtPath<Sprite>(FH_ICON + "ItemIcon_Coin_Gold.png"),
                "1,250");
            BuildPill(header.transform, "Gems",
                bubbleBg,
                AssetDatabase.LoadAssetAtPath<Sprite>(FH_ICON + "ItemIcon_Gem_Diamond_Blue.png"),
                "42");
            BuildPill(header.transform, "Energy",
                bubbleBg,
                AssetDatabase.LoadAssetAtPath<Sprite>(FH_ICON + "ItemIcon_Energy_Purple.png"),
                "18/20");
        }

        private static void BuildPill(Transform parent, string name, Sprite bg, Sprite icon, string val)
        {
            var pill = new GameObject(name + "Pill", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            pill.transform.SetParent(parent, false);
            var le = pill.GetComponent<LayoutElement>();
            le.preferredWidth = 116; le.preferredHeight = 48;

            var img = pill.GetComponent<Image>();
            if (bg != null) { img.sprite = bg; img.type = Image.Type.Sliced; img.color = Color.white; }
            else img.color = new Color(0.15f, 0.10f, 0.20f, 0.92f);

            // Icon (overhangs left edge of bubble)
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(pill.transform, false);
            var irt = iconGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot     = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(8, 0);
            irt.sizeDelta = new Vector2(40, 40);
            var iimg = iconGO.GetComponent<Image>();
            if (icon != null) { iimg.sprite = icon; iimg.preserveAspect = true; }
            else iimg.color = new Color(1f, 0.82f, 0.32f);
            iimg.raycastTarget = false;

            // Value text right-aligned
            var v = new GameObject("Val", typeof(RectTransform));
            v.transform.SetParent(pill.transform, false);
            var vrt = v.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(34, 4); vrt.offsetMax = new Vector2(-8, -4);
            var vtm = v.AddComponent<TextMeshProUGUI>();
            vtm.text = val;
            vtm.fontSize = 18;
            vtm.fontStyle = FontStyles.Bold;
            vtm.color = DEEP_NAVY;                          // darker font as requested
            vtm.alignment = TextAlignmentOptions.MidlineRight;
            vtm.enableAutoSizing = true;
            vtm.fontSizeMin = 12;
            vtm.fontSizeMax = 18;
            vtm.outlineWidth = 0.20f;
            vtm.outlineColor = new Color(1f, 0.95f, 0.75f, 0.95f); // cream halo for readability
            vtm.raycastTarget = false;
        }

        // ───────────────────── Stats card upgrade ─────────────────────
        private static void UpgradeStatsCard()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            var hrt = hud.GetComponent<RectTransform>();
            if (hrt != null)
            {
                hrt.anchorMin = new Vector2(1f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot     = new Vector2(1f, 1f);
                // Move to very top-right, give it more room
                // Flush against top-right corner
                hrt.anchoredPosition = new Vector2(-4, -4);
                hrt.sizeDelta = new Vector2(310, 130);
            }

            UpgradeRow(hud.transform.Find("KaruRow"),  isWisp:false);
            UpgradeRow(hud.transform.Find("MochiRow"), isWisp:true);

            // Remove the yellow divider line between rows
            var divider = hud.transform.Find("Divider");
            if (divider != null) Object.DestroyImmediate(divider.gameObject);
        }

        private static void UpgradeRow(Transform row, bool isWisp)
        {
            if (row == null) return;
            var rt = row.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Explicit position so KaruRow and MochiRow don't overlap
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, isWisp ? -66 : -4);
                rt.sizeDelta = new Vector2(-14, 60);
            }

            var avBg = row.Find("AvatarBg");
            if (avBg != null)
            {
                var arrt = avBg.GetComponent<RectTransform>();
                arrt.anchoredPosition = new Vector2(8, 0);
                arrt.sizeDelta = new Vector2(44, 44);
            }
            var name = row.Find("Name");
            if (name != null)
            {
                var nrt = name.GetComponent<RectTransform>();
                nrt.anchoredPosition = new Vector2(58, 14);
                nrt.sizeDelta = new Vector2(190, 22);
                foreach (var tm in name.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 15;
                    tm.fontStyle = FontStyles.Bold;
                    tm.color = CREAM;
                }
            }
            var lvl = row.Find("Level");
            if (lvl != null)
            {
                var lrt = lvl.GetComponent<RectTransform>();
                lrt.anchoredPosition = new Vector2(58, 0); // raised so XP bar doesn't cover it
                lrt.sizeDelta = new Vector2(44, 16);
                foreach (var tm in lvl.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 10;
                    tm.fontStyle = FontStyles.Bold;
                }
            }

            // Replace XP bar with proper slider sprites
            var oldBar = row.Find("XPBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);

            var bar = new GameObject("XPBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(row, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(28, 5);
            brt.sizeDelta = new Vector2(-70, 13); // bar slightly thicker

            var border = AssetDatabase.LoadAssetAtPath<Sprite>(FH_SLIDER + "Slider_Border_Rectangle_01_Bg.png");
            var bImg = bar.GetComponent<Image>();
            if (border != null) { bImg.sprite = border; bImg.type = Image.Type.Sliced; }
            else bImg.color = new Color(0, 0, 0, 0.6f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            float pct = isWisp ? 0.30f : 0.65f;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(pct, 1);
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);

            string fillSprite = isWisp
                ? FH_SLIDER + "Slider_Border_Rectangle_01_Fill_Blue.png"
                : FH_SLIDER + "Slider_Border_Rectangle_01_Fill_Yellow.png";
            var fImg = fill.GetComponent<Image>();   // already added in constructor
            var fSp = AssetDatabase.LoadAssetAtPath<Sprite>(fillSprite);
            if (fSp != null) { fImg.sprite = fSp; fImg.type = Image.Type.Sliced; }
            else fImg.color = isWisp ? new Color(0.55f, 0.7f, 1f) : new Color(1f, 0.85f, 0.35f);
        }

        // ───────────────────── Top buttons docked under stats card ─────────────────────
        private static void DockTopButtonsUnderStats()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;
            var rt = bar.GetComponent<RectTransform>();
            if (rt == null) return;

            // Stats card: y=-4, height 130 → ends at y=-134. Buttons sit at y=-140 with small gap.
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-60, -200);
            rt.sizeDelta = new Vector2(300, 88);
        }

        // ───────────────────── helpers ─────────────────────
        private static void BringChromeToFront()
        {
            string[] toFront = { "GameTitle", "PlayerHUD", "CurrencyHeader",
                                 "HomeNavButtons", "BottomNav", "HelpIcon" };
            foreach (var n in toFront)
            {
                var go = GameObject.Find(n);
                if (go != null) go.transform.SetAsLastSibling();
            }
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }
}
