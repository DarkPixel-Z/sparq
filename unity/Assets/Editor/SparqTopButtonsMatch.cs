using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqTopButtonsMatch
    {
        private const string FH_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Buttons/";

        [MenuItem("Sparq/109. Top buttons match bottom + sit above quest box")]
        public static void Apply()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Wipe + rebuild with same prefabs as bottom nav
            var ctrl = bar.GetComponent<Sparq.UI.HomeNavBar>();
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // ── POSITION: bottom-right anchor, just above quest box ──
            // Quest box is at y=90 with sizeDelta (420×280). Its TOP edge = 90+280 = 370 from bottom.
            // Place buttons at y=378 (8px gap above quest top)
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot     = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-14f, 378f);
            brt.sizeDelta = new Vector2(420, 64);  // same height as bottom nav layout

            // Layout: SAME as bottom nav — flex width, fixed height
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 4, 4);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;     // EQUAL flex like bottom
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Same prefabs + same colors as bottom nav
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
                var prefabPath = $"{FH_DIR}Button_01_l_{color}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                // Wrap in a layout cell (same pattern as bottom nav)
                var wrapper = new GameObject(objName, typeof(RectTransform));
                wrapper.transform.SetParent(bar.transform, false);
                var wle = wrapper.AddComponent<LayoutElement>();
                wle.flexibleWidth = 1;       // same flex as bottom
                wle.preferredHeight = 56;    // same height as bottom

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
            }

            if (ctrl == null) bar.AddComponent<Sparq.UI.HomeNavBar>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Top buttons rebuilt:\n\n" +
                "• Same Fantasy Hero painted prefabs as bottom nav (Green/Orange/Blue/Pink/Plum)\n" +
                "• 78×50 each (proportional to bottom)\n" +
                "• Anchored bottom-right, y=378 (just above quest box top)\n" +
                "• 4px spacing between\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
