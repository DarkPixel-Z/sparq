using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqAddWorldButton
    {
        // Use the same painted button style — purple matches "social/world" vibe
        private const string BTN_PURPLE = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/Button01_s_BtnText_Purple.prefab";

        [MenuItem("Sparq/51. Add WORLD button (chats + guilds)")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK");
                return;
            }

            // Make the bar taller for 5 buttons
            var brt = bar.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(135, 440);

            var existing = bar.transform.Find("WorldBtn");
            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BTN_PURPLE);
                if (prefab == null)
                {
                    EditorUtility.DisplayDialog("Sparq",
                        "Purple button prefab missing.", "OK");
                    return;
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bar.transform);
                go.name = "WorldBtn";
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(120, 70);
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
                le.preferredWidth = 120;
                le.preferredHeight = 70;
                foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
                {
                    tmp.text = "WORLD";
                    tmp.fontSize = 20;
                    tmp.fontStyle = FontStyles.Bold;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ WORLD button added (purple, same style).\n\n" +
                "Tap → opens chats + guilds panel (placeholder for now).\n\n" +
                "5 nav buttons: MAP / SHOP / BAG / PETS / WORLD", "OK");
        }
    }
}
