using UnityEngine;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>Menu 149: Bold + colored Trial card title.</summary>
    public static class SparqTrialTitleStyle149
    {
        // Deep crimson — feels like a quest-poster title against cream
        private static readonly Color CRIMSON = new Color(0.62f, 0.13f, 0.18f);

        [MenuItem("Sparq/149. Trial title — bold crimson")]
        public static void Apply()
        {
            var card = GameObject.Find("DailyTrialCard");
            if (card == null) { EditorUtility.DisplayDialog("Sparq", "DailyTrialCard not found.", "OK"); return; }

            var title = card.transform.Find("Title");
            if (title == null) return;

            foreach (var tm in title.GetComponentsInChildren<TMP_Text>(true))
            {
                tm.color = CRIMSON;
                tm.fontStyle = FontStyles.Bold;
                tm.outlineWidth = 0.18f;
                tm.outlineColor = new Color(1f, 0.95f, 0.82f, 0.9f); // cream halo
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Trial title → bold crimson with cream halo.\nHit ▶ Play.", "OK");
        }
    }
}
