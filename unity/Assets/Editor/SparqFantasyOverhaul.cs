using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// • Removes hero squad (keeps just Karu + Mochi sidekick)
    /// • Mochi placed slightly behind + offset Karu
    /// • Swaps button prefabs from Super Casual → Fantasy Hero (mature RPG)
    /// </summary>
    public static class SparqFantasyOverhaul
    {
        private const string FH_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Buttons/";

        private static readonly (string name, string color)[] SIDE_BUTTONS = new[]
        {
            ("MapBtn",   "Green"),
            ("ShopBtn",  "Orange"),
            ("BagBtn",   "Blue"),
            ("PetsBtn",  "Pink"),
            ("WorldBtn", "Plum"),
        };

        private static readonly (string name, string color)[] BOTTOM_TABS = new[]
        {
            ("Home",    "Green"),
            ("Journal", "Orange"),
            ("Remind",  "Blue"),
            ("Feed",    "Pink"),
            ("Profile", "Plum"),
        };

        [MenuItem("Sparq/91. Fantasy overhaul (squad → solo + Mochi sidekick + RPG buttons)")]
        public static void Apply()
        {
            // 1. Remove hero squad
            var squad = GameObject.Find("[HeroSquad]");
            if (squad != null) Object.DestroyImmediate(squad);

            // 2. Reposition Mochi as a single sidekick — behind + offset
            var karu = GameObject.Find("Karu");
            var mochi = GameObject.Find("Mochi");
            if (karu != null && mochi != null)
            {
                // Set back slightly (smaller scale and behind in sort order, slight offset)
                mochi.transform.position = new Vector3(2.0f, -1.6f, 0f);
                mochi.transform.localScale = Vector3.one * 0.32f;  // smaller than Karu (0.45)
                var msr = mochi.GetComponent<SpriteRenderer>();
                if (msr != null)
                {
                    msr.sortingOrder = 30;  // behind Karu (which is at 50)
                    var c = msr.color; c.a = 0.95f; msr.color = c; // very slight transparency hint
                }
            }

            // 3. Rebuild side buttons with Fantasy Hero prefabs
            RebuildSideButtons();

            // 4. Rebuild bottom nav with Fantasy Hero prefabs
            RebuildBottomNav();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Fantasy overhaul applied:\n\n" +
                "• Hero squad REMOVED — just Karu + Mochi sidekick\n" +
                "• Mochi positioned behind + offset (sort 30, scale 0.32)\n" +
                "• Side nav: Fantasy Hero painted RPG buttons\n" +
                "• Bottom nav: Fantasy Hero painted RPG tabs\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void RebuildSideButtons()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Wipe children, save controller
            var ctrl = bar.GetComponent<Sparq.UI.HomeNavBar>();
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // Build replacement painted buttons
            foreach (var (name, color) in SIDE_BUTTONS)
            {
                BuildSideBtn(bar.transform, name, color);
            }

            if (ctrl == null) bar.AddComponent<Sparq.UI.HomeNavBar>();
        }

        private static void BuildSideBtn(Transform parent, string objName, string color)
        {
            string prefabPath = $"{FH_DIR}Button_01_l_{color}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = objName;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(96, 44);
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 96;
            le.preferredHeight = 44;

            string label = objName.Replace("Btn", "").ToUpper();
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.text = label;
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
            }
        }

        private static void RebuildBottomNav()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            var ctrl = bar.GetComponent<Sparq.UI.BottomNavBar>();
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
            {
                var child = bar.transform.GetChild(i);
                // Don't kill the accent strip (LayoutElement.ignoreLayout)
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;
                Object.DestroyImmediate(child.gameObject);
            }
            if (ctrl == null) ctrl = bar.gameObject.AddComponent<Sparq.UI.BottomNavBar>();

            foreach (var (tab, color) in BOTTOM_TABS)
            {
                BuildBottomTab(bar.transform, tab, color, ctrl);
            }
        }

        private static void BuildBottomTab(Transform parent, string tabName, string color, Sparq.UI.BottomNavBar ctrl)
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
                tmp.text = tabName.ToUpper();
                tmp.fontSize = 12;
                tmp.fontStyle = FontStyles.Bold;
                tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
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
    }
}
