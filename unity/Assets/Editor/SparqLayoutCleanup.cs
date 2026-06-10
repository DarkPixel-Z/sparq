using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqLayoutCleanup
    {
        private const string FH_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Buttons/";

        [MenuItem("Sparq/93. Fix layout (text fit + buttons higher + bottom nav back)")]
        public static void Apply()
        {
            FixTopButtons();
            FixBottomNav();
            EnsureCanvasesRenderOnTop();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Layout fixed.\n\n" +
                "• Top buttons widened to 120px + word wrap off + 12pt\n" +
                "• Top buttons + Karu HUD moved higher (above clouds)\n" +
                "• Bottom nav rebuilt with Fantasy Hero painted tabs\n" +
                "• UI Canvas sortingOrder bumped above world sprites\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void FixTopButtons()
        {
            // 1. Move bar UP
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 1f);
                brt.anchorMax = new Vector2(0.5f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, -8f);  // very top
                brt.sizeDelta = new Vector2(660, 50);

                // 2. Force every button to fit text
                foreach (Transform t in bar.transform)
                {
                    var rt = t.GetComponent<RectTransform>();
                    if (rt != null) rt.sizeDelta = new Vector2(120, 44);
                    var le = t.GetComponent<LayoutElement>();
                    if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 120;
                    le.preferredHeight = 44;
                    le.flexibleWidth = 0;

                    foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (tmp == null) continue;
                        tmp.fontSize = 12;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                        tmp.overflowMode = TextOverflowModes.Overflow;
                        tmp.alignment = TextAlignmentOptions.Center;
                    }
                }
            }

            // 3. Move Karu HUD UP (was -60, now -8 to align with buttons)
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchoredPosition = new Vector2(-14f, -8f);
                hrt.sizeDelta = new Vector2(220, 60);
            }

            // 4. Move quest list down to clear HUD
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchoredPosition = new Vector2(-14f, -80f);
                qrt.sizeDelta = new Vector2(320, 250);
            }
        }

        private static void FixBottomNav()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Wipe + rebuild
            var old = GameObject.Find("BottomNav");
            if (old != null) Object.DestroyImmediate(old);

            var bar = new GameObject("BottomNav", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(canvas.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, 80);
            bar.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.95f);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 8, 8);
            hlg.spacing = 4;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Yellow accent strip on top of bar
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(bar.transform, false);
            var arRT = accent.GetComponent<RectTransform>();
            arRT.anchorMin = new Vector2(0, 1); arRT.anchorMax = new Vector2(1, 1);
            arRT.pivot = new Vector2(0.5f, 1f);
            arRT.anchoredPosition = Vector2.zero;
            arRT.sizeDelta = new Vector2(0, 3);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.85f);
            accent.GetComponent<Image>().raycastTarget = false;
            var alay = accent.AddComponent<LayoutElement>();
            alay.ignoreLayout = true;

            var ctrl = bar.AddComponent<Sparq.UI.BottomNavBar>();

            var tabs = new (string tab, string label, string color)[]
            {
                ("Home",    "HOME",    "Green"),
                ("Journal", "JOURNAL", "Orange"),
                ("Remind",  "REMIND",  "Blue"),
                ("Feed",    "FEED",    "Pink"),
                ("Profile", "PROFILE", "Plum"),
            };
            foreach (var (tab, label, color) in tabs)
            {
                BuildTab(bar.transform, tab, label, color, ctrl);
            }
        }

        private static void BuildTab(Transform parent, string tabName, string label, string color, Sparq.UI.BottomNavBar ctrl)
        {
            string prefabPath = $"{FH_DIR}Button_01_l_{color}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var wrapper = new GameObject($"Tab_{tabName}", typeof(RectTransform));
            wrapper.transform.SetParent(parent, false);
            var le = wrapper.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 56;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            go.name = "Btn";
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
            }
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.text = label;
                tmp.fontSize = 11;
                tmp.fontStyle = FontStyles.Bold;
                tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
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
        }

        private static void EnsureCanvasesRenderOnTop()
        {
            // Boost UI Canvas sortingOrder so jungle layers don't cover it
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null) continue;
                if (c.name == "UI Canvas")
                {
                    c.sortingOrder = 100; // far above world sprites (max world sort ~50)
                }
            }
        }
    }
}
