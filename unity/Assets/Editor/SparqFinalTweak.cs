using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqFinalTweak
    {
        [MenuItem("Sparq/100. FINAL tweaks (Karu+Mochi bigger, HUD top-right, soft yellow quests)")]
        public static void Apply()
        {
            // 1. Karu — BIGGER + position adjust
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.localScale = Vector3.one * 0.65f;     // was 0.45
                karu.transform.position = new Vector3(-2.2f, -0.7f, 0f);
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 50;
            }

            // 2. Mochi — WAY bigger (was 0.38, now 0.55)
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 0.55f;
                mochi.transform.position = new Vector3(0.0f, -1.0f, 0f);
                var msr = mochi.GetComponent<SpriteRenderer>();
                if (msr != null)
                {
                    msr.sortingOrder = 49;
                    var c = msr.color; c.a = 1f; msr.color = c;
                }
            }

            // 3. Karu HUD → top-right
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(1f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(1f, 1f);
                hrt.anchoredPosition = new Vector2(-14f, -8f);  // top-right corner
                hrt.sizeDelta = new Vector2(320, 96);
            }

            // 4. Quest box — UP slightly + softer yellow
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                // Move up — was anchoredPosition (-14, 90), now higher
                qrt.anchoredPosition = new Vector2(-14f, 130f);
                qrt.sizeDelta = new Vector2(340, 270);

                // Softer yellow background (was 0.98, 0.85, 0.30 = bright)
                var bg = ql.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = new Color(0.95f, 0.85f, 0.55f, 0.92f); // softer warm yellow
                }

                // Title slightly toned down
                foreach (var tmp in ql.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (tmp.text != null && tmp.text.Contains("Today"))
                    {
                        tmp.color = new Color(0.50f, 0.10f, 0.45f);  // deep magenta still pops
                        tmp.fontSize = 22;
                        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Final tweaks applied:\n\n" +
                "• Karu scale 0.45 → 0.65 (bigger hero)\n" +
                "• Mochi scale 0.38 → 0.55 (way bigger sidekick)\n" +
                "• Karu HUD → top-right corner (320×96)\n" +
                "• Quest box: moved up + softer warm yellow\n" +
                "• Quest title: deep magenta italic bold\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
