using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 152: Raise the trial bubble and perch the Pyro-Griffin
    /// on the rock platform (lower-right). Speech tail repositions
    /// to point DOWN-RIGHT toward the phoenix.
    /// </summary>
    public static class SparqPhoenixOnRock152
    {
        [MenuItem("Sparq/152. Raise bubble + perch phoenix on rock")]
        public static void Apply()
        {
            var card = GameObject.Find("DailyTrialCard");
            var phx  = GameObject.Find("PhoenixMascot");
            if (card == null) { EditorUtility.DisplayDialog("Sparq", "DailyTrialCard not found.", "OK"); return; }

            // 1. Raise bubble
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot     = new Vector2(0.5f, 0.5f);
            cardRT.anchoredPosition = new Vector2(200, -40); // moved down more
            cardRT.sizeDelta = new Vector2(500, 140); // restored size

            // 2. Perch phoenix on the rock (lower-right)
            if (phx != null)
            {
                var pRT = phx.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0.5f, 0.5f);
                pRT.anchorMax = new Vector2(0.5f, 0.5f);
                pRT.pivot     = new Vector2(0.5f, 1f); // pivot bottom so it "stands" on the rock
                pRT.anchoredPosition = new Vector2(400, -150); // right + slightly down
                pRT.sizeDelta = new Vector2(140, 140);
            }

            // 3. Move speech tail to bottom-right of bubble pointing down-right
            var tail = card.transform.Find("SpeechTail");
            if (tail != null)
            {
                var trt = tail.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(1, 0);
                trt.anchorMax = new Vector2(1, 0);
                trt.pivot     = new Vector2(0, 1);
                trt.anchoredPosition = new Vector2(-40, 4);
                trt.sizeDelta = new Vector2(28, 28);
                trt.localRotation = Quaternion.Euler(0, 0, 200); // point down-right
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Bubble raised, phoenix perched on rock.\n\n" +
                "• Bubble: y -100 → +80\n" +
                "• Phoenix: lower-right (+220, -120) with bottom pivot\n" +
                "• Speech tail: bottom-right pointing down\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
