using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqFinalLayoutPass
    {
        private const string PREFAB_DIR = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/";

        [MenuItem("Sparq/79. FINAL: logo top-left + painted bottom nav")]
        public static void Apply()
        {
            // 1. Logo at the very top-left corner
            var logo = GameObject.Find("GameTitle");
            if (logo != null)
            {
                var lrt = logo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 1f);
                lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(14f, -8f); // very top-left
            }

            // 2. Push the side button strip down so it doesn't overlap the logo
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 1f);
                brt.anchorMax = new Vector2(0.5f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, -70f); // below logo line
            }

            // 3. Rebuild bottom nav with painted buttons (same style as top)
            BuildPaintedBottomNav();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Layout",
                "✅ Final layout applied:\n\n" +
                "• Sparq logo: very top-left corner\n" +
                "• Top buttons: row below logo\n" +
                "• Bottom nav: same painted style (HOME/JOURNAL/REMIND/FEED/PROFILE)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildPaintedBottomNav()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("BottomNav");
            if (old != null) Object.DestroyImmediate(old);

            var bar = new GameObject("BottomNav", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(canvas.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, 80);
            bar.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.92f);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 6;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Yellow accent strip on top
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
                ("Journal", "JOURNAL", "Yellow"),
                ("Remind",  "REMIND",  "Sky"),
                ("Feed",    "FEED",    "Pink"),
                ("Profile", "PROFILE", "Purple"),
            };
            foreach (var (tab, label, color) in tabs)
            {
                BuildTab(bar.transform, tab, label, color, ctrl);
            }
        }

        private static void BuildTab(Transform parent, string tabName, string label, string colorName, Sparq.UI.BottomNavBar ctrl)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PREFAB_DIR}Button01_s_BtnText_{colorName}.prefab");
            if (prefab == null) return;

            var wrapper = new GameObject($"Tab_{tabName}", typeof(RectTransform));
            wrapper.transform.SetParent(parent, false);
            var le = wrapper.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 60;

            var btnGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            btnGO.name = "Btn";
            var brt = btnGO.GetComponent<RectTransform>();
            if (brt != null)
            {
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(2, 2); brt.offsetMax = new Vector2(-2, -2);
            }

            foreach (var tmp in btnGO.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.text = label;
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
            }

            var btn = btnGO.GetComponent<Button>();
            if (btn == null) btn = btnGO.GetComponentInChildren<Button>(true);
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
                var cap = ctrl;
                btn.onClick.AddListener(() => cap.OnTabClicked(t));
            }
        }
    }
}
