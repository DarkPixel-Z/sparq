using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqFantasyAllButtons
    {
        private const string UI_DIR = "Assets/Art/UI/";
        private const string ICON_DIR = "Assets/FantasyIconPack/256/";

        // Bottom nav: tab → label, color, icon
        private static readonly (string tab, string label, string icon, string color)[] BOTTOM_TABS = new[]
        {
            ("Home",    "HOME",    "Map.png",       "brown"),     // (no House icon — Map works)
            ("Journal", "JOURNAL", "Letter.png",    "beige"),
            ("Remind",  "REMIND",  "GemYellow.png", "blue"),
            ("Feed",    "FEED",    "HeartFull.png", "brown"),
            ("Profile", "PROFILE", "HelmetT1.png",  "grey"),
        };

        [MenuItem("Sparq/124. BOTTOM nav → Fantasy buttons (long)")]
        public static void Bottom()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            // Wipe children except accent strip
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
            {
                var child = bar.transform.GetChild(i);
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;  // keep accent
                Object.DestroyImmediate(child.gameObject);
            }

            // Re-attach controller if missing
            var ctrl = bar.GetComponent<Sparq.UI.BottomNavBar>();
            if (ctrl == null) ctrl = bar.gameObject.AddComponent<Sparq.UI.BottomNavBar>();

            foreach (var (tab, label, icon, color) in BOTTOM_TABS)
            {
                BuildBottomTab(bar.transform, tab, label, icon, color, ctrl);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Bottom nav uses fantasy buttonLong style.\n\n" +
                "Same Kenney UI wood/parchment/metal/stone aesthetic as top.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildBottomTab(Transform parent, string tabName, string label, string iconFile, string color, Sparq.UI.BottomNavBar ctrl)
        {
            string buttonPath = UI_DIR + $"buttonLong_{color}.png";
            EnsureSprite(buttonPath);
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(buttonPath);
            if (buttonSprite == null) return;

            string iconPath = ICON_DIR + iconFile;
            EnsureSprite(iconPath);
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            var wrapper = new GameObject($"Tab_{tabName}", typeof(RectTransform));
            wrapper.transform.SetParent(parent, false);
            var le = wrapper.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 64;

            var btnGO = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(wrapper.transform, false);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
            var img = btnGO.GetComponent<Image>();
            img.sprite = buttonSprite;
            img.type = Image.Type.Sliced;

            // Icon left, label right inside the wide button
            if (iconSprite != null)
            {
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(btnGO.transform, false);
                var irt = icon.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0, 0.5f);
                irt.anchorMax = new Vector2(0, 0.5f);
                irt.pivot = new Vector2(0, 0.5f);
                irt.anchoredPosition = new Vector2(6, 0);
                irt.sizeDelta = new Vector2(40, 40);  // 28 → 40
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = iconSprite;
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;
            }

            // Label
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(btnGO.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(50, 0); lrt.offsetMax = new Vector2(-4, 0);
            var ltm = lbl.AddComponent<TextMeshProUGUI>();
            ltm.text = label;
            ltm.fontSize = 11;
            ltm.fontStyle = FontStyles.Bold;
            ltm.color = new Color(0.25f, 0.15f, 0.05f); // dark wood text
            ltm.alignment = TextAlignmentOptions.Center;
            ltm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            ltm.raycastTarget = false;

            // Wire onClick
            var btn = btnGO.GetComponent<Button>();
            Sparq.UI.BottomNavBar.Tab t = tabName switch
            {
                "Home" => Sparq.UI.BottomNavBar.Tab.Home,
                "Journal" => Sparq.UI.BottomNavBar.Tab.Journal,
                "Remind" => Sparq.UI.BottomNavBar.Tab.Remind,
                "Feed" => Sparq.UI.BottomNavBar.Tab.Feed,
                "Profile" => Sparq.UI.BottomNavBar.Tab.Profile,
                _ => Sparq.UI.BottomNavBar.Tab.Home,
            };
            var capCtrl = ctrl;
            btn.onClick.AddListener(() => capCtrl.OnTabClicked(t));
        }

        // ── Help icon → fantasy round button with Una sprite + ? badge ──
        [MenuItem("Sparq/125. Help icon → Fantasy round button")]
        public static void HelpFantasy()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Wipe old
            var old = GameObject.Find("HelpIcon");
            if (old != null) Object.DestroyImmediate(old);

            string buttonPath = UI_DIR + "buttonRound_brown.png";
            EnsureSprite(buttonPath);
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(buttonPath);

            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);
            var hrt = help.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(8f, 88f);
            hrt.sizeDelta = new Vector2(80, 80);  // bigger button (was 64)

            var img = help.GetComponent<Image>();
            if (bgSprite != null) img.sprite = bgSprite;
            else img.color = new Color(0.55f, 0.40f, 0.20f);
            img.type = Image.Type.Sliced;

            // Una sprite on top of fantasy button
            string unaPath = "Assets/Art/Sparq/una-mage.png";
            EnsureSprite(unaPath);
            var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(unaPath);
            if (unaSprite != null)
            {
                var una = new GameObject("Una", typeof(RectTransform), typeof(Image));
                una.transform.SetParent(help.transform, false);
                var urt = una.GetComponent<RectTransform>();
                urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one;
                urt.offsetMin = new Vector2(2, 2); urt.offsetMax = new Vector2(-2, -2);  // less padding = bigger Una
                var uimg = una.GetComponent<Image>();
                uimg.sprite = unaSprite;
                uimg.preserveAspect = true;
                uimg.raycastTarget = false;
            }

            // Yellow "?" badge top-right
            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(help.transform, false);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(2f, 2f);
            brt.sizeDelta = new Vector2(22, 22);
            badge.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f);

            var qGO = new GameObject("Q", typeof(RectTransform));
            qGO.transform.SetParent(badge.transform, false);
            var qrt = qGO.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var qtm = qGO.AddComponent<TextMeshProUGUI>();
            qtm.text = "?";
            qtm.fontSize = 14;
            qtm.fontStyle = FontStyles.Bold;
            qtm.alignment = TextAlignmentOptions.Center;
            qtm.color = new Color(0.05f, 0.02f, 0.10f);
            qtm.raycastTarget = false;

            // Wire button to help popup
            var btn = help.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Help icon → fantasy round wooden button.\n\n" +
                "• Round wooden bg\n" +
                "• Una mage on top\n" +
                "• Yellow '?' badge top-right\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
        }
    }
}
