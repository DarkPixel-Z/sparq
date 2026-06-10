using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace Sparq.Editor
{
    public static class SparqTrollNestButtons
    {
        private const string TN_DIR = "Assets/[UIFabrica]TrollNest_Free_v01/02.PNG/";

        // Each top button = one TrollNest button sprite (icon already baked in)
        private static readonly (string btn, string spriteName, string label)[] TOP_BTNS = new[]
        {
            ("MapBtn",   "trollnest_castle-normal.png",  "MAP"),     // castle = map
            ("ShopBtn",  "trollnest_coin-normal.png",    "SHOP"),    // coin
            ("BagBtn",   "trollnest_cup-normal.png",     "BAG"),     // cup/trophy
            ("PetsBtn",  "trollnest_potion-normal.png",  "PETS"),    // potion
            ("WorldBtn", "trollnest_options-normal.png", "WORLD"),   // options/gear
        };

        // Bottom nav uses the long button variant for HOME/JOURNAL/REMIND/FEED/PROFILE
        private static readonly (string tab, string label)[] BOTTOM_TABS = new[]
        {
            ("Home",    "HOME"),
            ("Quests",  "QUESTS"),
            ("Journal", "JOURNAL"),
            ("Remind",  "REMIND"),
            ("Feed",    "FEED"),
            ("Profile", "PROFILE"),
        };

        [MenuItem("Sparq/127. TrollNest buttons (top + bottom + help)")]
        public static void Apply()
        {
            EnsureSprites();

            ApplyTopButtons();
            ApplyBottomNav();
            ApplyHelpIcon();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ TrollNest buttons applied:\n\n" +
                "• Top: castle, coin, cup, potion, options (icons baked in)\n" +
                "• Bottom: long buttons with text labels\n" +
                "• Help icon: round main button with Una mage on top\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void ApplyTopButtons()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 6;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            foreach (var (btnName, spriteName, label) in TOP_BTNS)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TN_DIR + spriteName);
                if (sprite == null) continue;

                var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(bar.transform, false);
                var le = go.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.preferredHeight = 64;

                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;

                // Label below the button (since icons are baked in)
                var lbl = new GameObject("Label", typeof(RectTransform));
                lbl.transform.SetParent(go.transform, false);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0f);
                lrt.anchoredPosition = new Vector2(0, -2);
                lrt.sizeDelta = new Vector2(0, 14);
                var tm = lbl.AddComponent<TextMeshProUGUI>();
                tm.text = label;
                tm.fontSize = 10;
                tm.fontStyle = FontStyles.Bold;
                tm.color = new Color(1f, 0.95f, 0.6f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.raycastTarget = false;
            }
        }

        private static void ApplyBottomNav()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            // Wipe except accent
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
            {
                var child = bar.transform.GetChild(i);
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;
                Object.DestroyImmediate(child.gameObject);
            }

            var ctrl = bar.GetComponent<Sparq.UI.BottomNavBar>();
            if (ctrl == null) ctrl = bar.gameObject.AddComponent<Sparq.UI.BottomNavBar>();

            var longSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TN_DIR + "trollnest_long-btn-normal.png");
            if (longSprite == null) return;

            foreach (var (tab, label) in BOTTOM_TABS)
            {
                var wrapper = new GameObject($"Tab_{tab}", typeof(RectTransform));
                wrapper.transform.SetParent(bar.transform, false);
                var le = wrapper.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.preferredHeight = 64;

                var btnGO = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(wrapper.transform, false);
                var rt = btnGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
                var img = btnGO.GetComponent<Image>();
                img.sprite = longSprite;
                img.type = Image.Type.Sliced;

                // Label centered
                var lbl = new GameObject("Label", typeof(RectTransform));
                lbl.transform.SetParent(btnGO.transform, false);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                var ltm = lbl.AddComponent<TextMeshProUGUI>();
                ltm.text = label;
                ltm.fontSize = 13;
                ltm.fontStyle = FontStyles.Bold;
                ltm.color = new Color(1f, 0.95f, 0.6f);
                ltm.alignment = TextAlignmentOptions.Center;
                ltm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                ltm.raycastTarget = false;

                var btn = btnGO.GetComponent<Button>();
                Sparq.UI.BottomNavBar.Tab t = tab switch
                {
                    "Home" => Sparq.UI.BottomNavBar.Tab.Home,
                    "Quests" => Sparq.UI.BottomNavBar.Tab.Quests,
                    "Journal" => Sparq.UI.BottomNavBar.Tab.Journal,
                    "Remind" => Sparq.UI.BottomNavBar.Tab.Remind,
                    "Feed" => Sparq.UI.BottomNavBar.Tab.Feed,
                    "Profile" => Sparq.UI.BottomNavBar.Tab.Profile,
                    _ => Sparq.UI.BottomNavBar.Tab.Home,
                };
                var capCtrl = ctrl;
                btn.onClick.AddListener(() => capCtrl.OnTabClicked(t));
            }
        }

        private static void ApplyHelpIcon()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("HelpIcon");
            if (old != null) Object.DestroyImmediate(old);

            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TN_DIR + "trollnest_main-btn-normal.png");

            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);
            var hrt = help.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(8f, 88f);
            hrt.sizeDelta = new Vector2(80, 80);
            var img = help.GetComponent<Image>();
            if (bgSprite != null) img.sprite = bgSprite;
            img.preserveAspect = true;

            // Una sprite on top
            string unaPath = "Assets/Art/Sparq/una-mage.png";
            var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(unaPath);
            if (unaSprite != null)
            {
                var una = new GameObject("Una", typeof(RectTransform), typeof(Image));
                una.transform.SetParent(help.transform, false);
                var urt = una.GetComponent<RectTransform>();
                urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one;
                urt.offsetMin = new Vector2(8, 8); urt.offsetMax = new Vector2(-8, -8);
                var uimg = una.GetComponent<Image>();
                uimg.sprite = unaSprite;
                uimg.preserveAspect = true;
                uimg.raycastTarget = false;
            }

            // Yellow ? badge
            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(help.transform, false);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(2f, 2f);
            brt.sizeDelta = new Vector2(24, 24);
            badge.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f);

            var qGO = new GameObject("Q", typeof(RectTransform));
            qGO.transform.SetParent(badge.transform, false);
            var qrt = qGO.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var qtm = qGO.AddComponent<TextMeshProUGUI>();
            qtm.text = "?";
            qtm.fontSize = 16;
            qtm.fontStyle = FontStyles.Bold;
            qtm.alignment = TextAlignmentOptions.Center;
            qtm.color = new Color(0.05f, 0.02f, 0.10f);
            qtm.raycastTarget = false;

            var btn = help.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });
        }

        private static void EnsureSprites()
        {
            if (!Directory.Exists(TN_DIR)) return;
            bool changed = false;
            foreach (var f in Directory.GetFiles(TN_DIR, "*.png"))
            {
                string ap = f.Replace('\\','/');
                int idx = ap.IndexOf("Assets/");
                if (idx >= 0) ap = ap.Substring(idx);
                var imp = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                    changed = true;
                }
            }
            if (changed) AssetDatabase.Refresh();
        }
    }
}
