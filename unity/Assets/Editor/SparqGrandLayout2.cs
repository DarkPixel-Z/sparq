using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqGrandLayout2
    {
        private const string FH_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Buttons/";

        [MenuItem("Sparq/105. Logo as fantasy button + Mochi stats + smaller Una bottom-left")]
        public static void Apply()
        {
            // 1. LOGO inside Fantasy Hero painted button (matches bottom nav)
            BuildLogoAsFantasyButton();

            // 2. Top buttons → octagonal Fantasy Hero style, on right next to quests
            ReshapeTopButtons();

            // 3. Quests → top-right
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchorMin = new Vector2(1f, 1f);
                qrt.anchorMax = new Vector2(1f, 1f);
                qrt.pivot = new Vector2(1f, 1f);
                qrt.anchoredPosition = new Vector2(-14f, -170f);
                qrt.sizeDelta = new Vector2(360, 280);
            }

            // 4. Add Mochi stats card BELOW Karu HUD
            AddMochiStatsCard();

            // 5. Una help icon → smaller + bottom-left + lower
            ResizeUnaHelp();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Grand layout 2 applied:\n\n" +
                "• Logo in Fantasy Hero button shape (matches bottom nav style)\n" +
                "• Top buttons same painted style as bottom\n" +
                "• Quests back to top-right\n" +
                "• Mochi stats card below Karu HUD\n" +
                "• Una help icon: 70×70, bottom-left, lower\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildLogoAsFantasyButton()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            // Use a Fantasy Hero prefab as the frame (Plum looks great with the purple logo)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FH_DIR + "Button_01_l_Plum.prefab");
            if (prefab == null) return;

            var titleGO = new GameObject("GameTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -8f);
            rt.sizeDelta = new Vector2(280, 110);

            // Instantiate the button prefab as the visual frame
            var frameInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, titleGO.transform);
            frameInstance.name = "Frame";
            var frt = frameInstance.GetComponent<RectTransform>();
            if (frt != null)
            {
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            }
            // Hide the prefab's default text label
            foreach (var tmp in frameInstance.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.enabled = false;
            }

            // Logo image on top of the frame
            string logoPath = "Assets/Art/Sparq/sparq-logo.png";
            var imp = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
            if (sprite != null)
            {
                var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
                logo.transform.SetParent(titleGO.transform, false);
                var lrt = logo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(8, 8); lrt.offsetMax = new Vector2(-8, -8);
                var img = logo.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }

        private static void ReshapeTopButtons()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            var ctrl = bar.GetComponent<Sparq.UI.HomeNavBar>();
            // Wipe + rebuild with same prefabs as bottom nav
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-14f, -120f);
            brt.sizeDelta = new Vector2(360, 44);

            var pairs = new[]
            {
                ("MapBtn",   "MAP",   "Green"),
                ("ShopBtn",  "SHOP",  "Orange"),
                ("BagBtn",   "BAG",   "Blue"),
                ("PetsBtn",  "PETS",  "Pink"),
                ("WorldBtn", "WORLD", "Plum"),
            };
            foreach (var (objName, label, color) in pairs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FH_DIR + $"Button_01_l_{color}.prefab");
                if (prefab == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bar.transform);
                go.name = objName;

                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(64, 38);
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
                le.preferredWidth = 64;
                le.preferredHeight = 38;
                le.flexibleWidth = 0;

                foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
                {
                    tmp.text = label;
                    tmp.fontSize = 10;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                }
            }

            if (ctrl == null) bar.AddComponent<Sparq.UI.HomeNavBar>();
        }

        private static void AddMochiStatsCard()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("MochiHUD");
            if (old != null) Object.DestroyImmediate(old);

            var hud = new GameObject("MochiHUD", typeof(RectTransform), typeof(Image));
            hud.transform.SetParent(canvas.transform, false);
            var rt = hud.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -75f);  // just below Karu HUD
            rt.sizeDelta = new Vector2(420, 36);
            hud.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.90f);

            // Mochi avatar
            var avBg = new GameObject("AvatarBg", typeof(RectTransform), typeof(Image));
            avBg.transform.SetParent(hud.transform, false);
            var arrt = avBg.GetComponent<RectTransform>();
            arrt.anchorMin = new Vector2(0, 0.5f); arrt.anchorMax = new Vector2(0, 0.5f);
            arrt.pivot = new Vector2(0, 0.5f);
            arrt.anchoredPosition = new Vector2(4, 0);
            arrt.sizeDelta = new Vector2(28, 28);
            avBg.GetComponent<Image>().color = new Color(0.30f, 0.20f, 0.40f, 0.9f);

            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(avBg.transform, false);
            var avrt = avatar.GetComponent<RectTransform>();
            avrt.anchorMin = Vector2.zero; avrt.anchorMax = Vector2.one;
            avrt.offsetMin = new Vector2(2, 2); avrt.offsetMax = new Vector2(-2, -2);
            var aImg = avatar.GetComponent<Image>();
            aImg.preserveAspect = true;
            // Try una-mage as Mochi for now (or mochi.svg if present)
            var mochiSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/una-mage.png");
            if (mochiSprite == null) mochiSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/mochi.svg");
            if (mochiSprite != null) aImg.sprite = mochiSprite;

            // Name
            var name = new GameObject("Name", typeof(RectTransform));
            name.transform.SetParent(hud.transform, false);
            var nrt = name.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.pivot = new Vector2(0, 0.5f);
            nrt.anchoredPosition = new Vector2(40, 0);
            nrt.sizeDelta = new Vector2(120, 28);
            var ntm = name.AddComponent<TextMeshProUGUI>();
            ntm.text = "Mochi";
            ntm.fontSize = 16;
            ntm.fontStyle = FontStyles.Bold;
            ntm.color = Color.white;
            ntm.alignment = TextAlignmentOptions.Left;

            // Lv badge
            var level = new GameObject("Level", typeof(RectTransform), typeof(Image));
            level.transform.SetParent(hud.transform, false);
            var lrt = level.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(170, 0);
            lrt.sizeDelta = new Vector2(48, 22);
            level.GetComponent<Image>().color = new Color(0.85f, 0.55f, 1f);
            var ltGO = new GameObject("LvText", typeof(RectTransform));
            ltGO.transform.SetParent(level.transform, false);
            var ltrt = ltGO.GetComponent<RectTransform>();
            ltrt.anchorMin = Vector2.zero; ltrt.anchorMax = Vector2.one;
            ltrt.offsetMin = Vector2.zero; ltrt.offsetMax = Vector2.zero;
            var lttm = ltGO.AddComponent<TextMeshProUGUI>();
            lttm.text = "Lv.1";
            lttm.fontSize = 12;
            lttm.fontStyle = FontStyles.Bold;
            lttm.alignment = TextAlignmentOptions.Center;
            lttm.color = new Color(0.05f, 0.02f, 0.10f);

            // Subtitle "Pet"
            var sub = new GameObject("Subtitle", typeof(RectTransform));
            sub.transform.SetParent(hud.transform, false);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0.5f); srt.anchorMax = new Vector2(0, 0.5f);
            srt.pivot = new Vector2(0, 0.5f);
            srt.anchoredPosition = new Vector2(225, 0);
            srt.sizeDelta = new Vector2(180, 20);
            var stm = sub.AddComponent<TextMeshProUGUI>();
            stm.text = "Loyal Companion";
            stm.fontSize = 11;
            stm.fontStyle = FontStyles.Italic;
            stm.color = new Color(0.85f, 0.85f, 1f, 0.85f);
            stm.alignment = TextAlignmentOptions.Left;
        }

        private static void ResizeUnaHelp()
        {
            var help = GameObject.Find("HelpIcon");
            if (help == null) return;
            var hrt = help.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(8f, 90f);  // lower than 96
            hrt.sizeDelta = new Vector2(70, 70);          // smaller (was 110)

            // Shrink the badge proportionally
            var badge = help.transform.Find("Badge");
            if (badge != null)
            {
                var brt = badge.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(22, 22);
                foreach (var tmp in badge.GetComponentsInChildren<TMP_Text>(true))
                    tmp.fontSize = 16;
            }
        }
    }
}
