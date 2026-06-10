using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>Menu 148: Shift Daily Trial card right + down.</summary>
    public static class SparqShiftTrial148
    {
        [MenuItem("Sparq/148. Shift Trial card right + down")]
        public static void Apply()
        {
            var card = GameObject.Find("DailyTrialCard");
            if (card == null) { EditorUtility.DisplayDialog("Sparq", "DailyTrialCard not found.", "OK"); return; }

            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(260, -100); // tiny right + tiny up

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Trial card moved right + down.\nx 60→160, y 50→-40\n\nHit ▶ Play.", "OK");
        }
    }
}
