using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqQuestPlace
    {
        [MenuItem("Sparq/108. Quests bottom-right just above buttons")]
        public static void Apply()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null) return;

            var qrt = ql.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(1f, 0f);
            qrt.anchorMax = new Vector2(1f, 0f);
            qrt.pivot     = new Vector2(1f, 0f);
            qrt.anchoredPosition = new Vector2(-14f, 90f);  // bottom nav is 80, quest sits 10px above
            qrt.sizeDelta = new Vector2(420, 280);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Quest box → bottom-right, just above bottom nav.\n\n" +
                "• Anchor: bottom-right\n" +
                "• Position: -14, 90 (10px above 80px nav)\n" +
                "• Size: 420×280\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
