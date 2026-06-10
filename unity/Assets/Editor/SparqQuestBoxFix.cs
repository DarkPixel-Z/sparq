using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqQuestBoxFix
    {
        [MenuItem("Sparq/56. Fix quest box (smaller, side panel)")]
        public static void Apply()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null)
            {
                EditorUtility.DisplayDialog("Sparq", "QuestListUI not found.", "OK");
                return;
            }

            var qrt = ql.GetComponent<RectTransform>();

            // Move to top-LEFT, below the SPARQ logo, narrower + shorter
            qrt.anchorMin = new Vector2(0f, 1f);
            qrt.anchorMax = new Vector2(0f, 1f);
            qrt.pivot     = new Vector2(0f, 1f);
            qrt.anchoredPosition = new Vector2(14f, -110f);  // below logo
            qrt.sizeDelta = new Vector2(380, 280);

            // Background: more transparent so it doesn't block the scene
            var bg = ql.GetComponent<Image>();
            if (bg == null) bg = ql.gameObject.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.05f, 0.20f, 0.78f);

            // Make sure each quest row's font sizes are reasonable
            foreach (var tmp in ql.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (tmp.text == "Today's Quests")
                {
                    tmp.fontSize = 18;
                    tmp.color = new Color(1f, 0.85f, 0.4f);
                    tmp.fontStyle = FontStyles.Bold;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Quest box repositioned.\n\n" +
                "• Top-LEFT under the SPARQ logo\n" +
                "• Smaller: 380×280\n" +
                "• 78% opacity — forest peeks through\n" +
                "• Title styled gold/bold\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
