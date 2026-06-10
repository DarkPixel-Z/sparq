using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqStageMapSetup
    {
        private const string STAGE_PATH =
            "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/Play_Stage_Select_1.prefab";

        [MenuItem("Sparq/36. Wire Stage Map (20 rivals)")]
        public static void Wire()
        {
            var pmGO = GameObject.Find("[PopupManager]");
            if (pmGO == null)
            {
                EditorUtility.DisplayDialog("Sparq Map",
                    "[PopupManager] not in scene. Run Sparq → 18 first.", "OK");
                return;
            }
            var pm = pmGO.GetComponent<Sparq.UI.PopupManager>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(STAGE_PATH);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Sparq Map",
                    "Play_Stage_Select_1.prefab not found.", "OK");
                return;
            }

            var so = new SerializedObject(pm);
            so.FindProperty("stageMapPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();

            // Add a MAP button next to SHOP/BAG on left edge
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq Map",
                    "HomeNavButtons not found. Run Sparq → 32 first.", "OK");
                return;
            }

            // Remove old Map button if re-running
            var oldMap = bar.transform.Find("MapBtn");
            if (oldMap != null) Object.DestroyImmediate(oldMap.gameObject);

            BuildMapButton(bar.transform);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Stage Map",
                "✅ Stage Map wired!\n\n" +
                "• Green 'MAP' button on left edge (next to SHOP + BAG)\n" +
                "• Opens full-screen stage select with all 20 rivals\n" +
                "• Prev/Next to browse, locked stages greyed out\n" +
                "• Tap any stage button to challenge that rival\n" +
                "• Respects level gating (Lv 18 Ember won't fight at Lv 1)\n\n" +
                "Hit ▶ Play → tap MAP.", "OK");
        }

        private static void BuildMapButton(Transform parent)
        {
            var go = new GameObject("MapBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling(); // put MAP at top

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 64;
            le.preferredHeight = 64;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.35f, 0.85f, 0.45f, 0.95f);  // green, matches XP bar

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => Sparq.UI.PopupManager.Instance?.OpenMap());

            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(go.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = "MAP";
            tm.fontSize = 16;
            tm.color = new Color(0.05f, 0.15f, 0.05f);
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
        }
    }
}
