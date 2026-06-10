using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqAddPetsButton
    {
        private const string BTN_PINK = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/Button01_s_BtnText_Pink.prefab";

        [MenuItem("Sparq/48. Add PETS button + raise nav bar")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK");
                return;
            }

            // Raise the bar so it doesn't cover the tree
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot     = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(14f, -120f);  // higher than -180
            brt.sizeDelta = new Vector2(135, 360);            // taller for 4 buttons

            // Add PETS button if not present
            var existingPets = bar.transform.Find("PetsBtn");
            if (existingPets == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BTN_PINK);
                if (prefab == null)
                {
                    EditorUtility.DisplayDialog("Sparq",
                        "Pink button prefab missing. Re-import GUI Pro Super Casual.", "OK");
                    return;
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bar.transform);
                go.name = "PetsBtn";
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(120, 70);
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
                le.preferredWidth = 120;
                le.preferredHeight = 70;
                foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
                {
                    tmp.text = "PETS";
                    tmp.fontSize = 24;
                    tmp.fontStyle = FontStyles.Bold;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ PETS button added + nav bar raised.\n\n" +
                "• 4 buttons now: MAP, SHOP, BAG, PETS\n" +
                "• Bar position raised so it doesn't cover the tree\n" +
                "• PETS opens pet swap panel (Bear ↔ Batty in future)\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
