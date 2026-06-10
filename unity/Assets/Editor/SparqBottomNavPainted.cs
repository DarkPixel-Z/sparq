using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Rebuilds the bottom nav using painted Super Casual button prefabs
    /// (same style as MAP/SHOP/BAG/PETS/WORLD side buttons).
    /// </summary>
    public static class SparqBottomNavPainted
    {
        private const string PREFAB_DIR = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/";

        private static readonly (string tab, string label, string prefab)[] TABS = new[]
        {
            ("Home",    "HOME",    "Button01_s_BtnText_Green.prefab"),
            ("Journal", "JOURNAL", "Button01_s_BtnText_Yellow.prefab"),
            ("Remind",  "REMIND",  "Button01_s_BtnText_Sky.prefab"),
            ("Feed",    "FEED",    "Button01_s_BtnText_Pink.prefab"),
            ("Profile", "PROFILE", "Button01_s_BtnText_Purple.prefab"),
        };

        [MenuItem("Sparq/64. PAINTED bottom nav (same style as side buttons)")]
        public static void Build()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Remove old bottom nav
            var old = GameObject.Find("BottomNav");
            if (old != null) Object.DestroyImmediate(old);

            // Build new bar
            var bar = new GameObject("BottomNav", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(canvas.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, 110);

            bar.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.92f);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 14, 14);
            hlg.spacing = 8;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Top accent line
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(bar.transform, false);
            var arRT = accent.GetComponent<RectTransform>();
            arRT.anchorMin = new Vector2(0, 1); arRT.anchorMax = new Vector2(1, 1);
            arRT.pivot = new Vector2(0.5f, 1f);
            arRT.anchoredPosition = Vector2.zero;
            arRT.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.85f);
            accent.GetComponent<Image>().raycastTarget = false;
            var accLE = accent.AddComponent<LayoutElement>();
            accLE.ignoreLayout = true;

            // Add controller component
            var ctrl = bar.AddComponent<Sparq.UI.BottomNavBar>();

            // Build painted tab buttons
            foreach (var (tab, label, prefabFile) in TABS)
            {
                BuildPaintedTab(bar.transform, tab, label, PREFAB_DIR + prefabFile, ctrl);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Bottom Nav",
                "✅ Painted bottom nav built!\n\n" +
                "• HOME (green) | JOURNAL (yellow) | REMIND (sky) | FEED (pink) | PROFILE (purple)\n" +
                "• Same painted style as side MAP/SHOP/BAG buttons\n" +
                "• Yellow accent strip on top\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildPaintedTab(Transform parent, string tabName, string label, string prefabPath, Sparq.UI.BottomNavBar ctrl)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            // Wrapper with name "Tab_X" so the controller can find it
            var wrapper = new GameObject($"Tab_{tabName}", typeof(RectTransform));
            wrapper.transform.SetParent(parent, false);
            var le = wrapper.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 80;

            // Instantiate the painted button as child
            var btnGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            btnGO.name = "Btn";
            var brt = btnGO.GetComponent<RectTransform>();
            if (brt != null)
            {
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(4, 4); brt.offsetMax = new Vector2(-4, -4);
                brt.localScale = Vector3.one;
            }

            // Set the label
            foreach (var tmp in btnGO.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.text = label;
                tmp.fontSize = 18;
                tmp.fontStyle = FontStyles.Bold;
            }

            // Wire onClick
            var btn = btnGO.GetComponent<Button>();
            if (btn == null) btn = btnGO.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                Sparq.UI.BottomNavBar.Tab tabType = tabName switch
                {
                    "Home"    => Sparq.UI.BottomNavBar.Tab.Home,
                    "Journal" => Sparq.UI.BottomNavBar.Tab.Journal,
                    "Remind"  => Sparq.UI.BottomNavBar.Tab.Remind,
                    "Feed"    => Sparq.UI.BottomNavBar.Tab.Feed,
                    "Profile" => Sparq.UI.BottomNavBar.Tab.Profile,
                    _         => Sparq.UI.BottomNavBar.Tab.Home,
                };
                var cap = ctrl;
                btn.onClick.AddListener(() => cap.OnTabClicked(tabType));
            }
        }
    }
}
