using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 147: Reposition + resize the Daily Trial card so it sits cleanly
    /// in the open central area, with all text visible.
    /// </summary>
    public static class SparqTrialPolish147
    {
        [MenuItem("Sparq/147. Polish Daily Trial card (bigger + center it)")]
        public static void Apply()
        {
            var card = GameObject.Find("DailyTrialCard");
            if (card == null) { EditorUtility.DisplayDialog("Sparq", "DailyTrialCard not found. Run #145 first.", "OK"); return; }

            // Make the card bigger and center it in the open middle
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(60, 50); // slight right + up so it doesn't crowd hero
            rt.sizeDelta = new Vector2(500, 140);      // taller so reward text isn't clipped

            // Re-layout children with proper proportions
            // 1. Ribbon top
            var ribbon = card.transform.Find("Ribbon");
            if (ribbon != null)
            {
                var rrt = ribbon.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0.5f, 1);
                rrt.anchoredPosition = new Vector2(0, -10);
                rrt.sizeDelta = new Vector2(-50, 26);
                foreach (var tm in ribbon.GetComponentsInChildren<TMP_Text>(true))
                    tm.fontSize = 13;
            }

            // 2. Glyph circle
            var glyphBg = card.transform.Find("GlyphBg");
            if (glyphBg != null)
            {
                var grt = glyphBg.GetComponent<RectTransform>();
                grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
                grt.pivot = new Vector2(0, 0.5f);
                grt.anchoredPosition = new Vector2(18, -10);
                grt.sizeDelta = new Vector2(64, 64);
                foreach (var tm in glyphBg.GetComponentsInChildren<TMP_Text>(true))
                    tm.fontSize = 36;
            }

            // 3. Title
            var title = card.transform.Find("Title");
            if (title != null)
            {
                var trt = title.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
                trt.pivot = new Vector2(0, 1);
                trt.anchoredPosition = new Vector2(94, -42);
                trt.sizeDelta = new Vector2(-240, 26);
                foreach (var tm in title.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 20;
                    tm.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }

            // 4. Subtitle
            var sub = card.transform.Find("Sub");
            if (sub != null)
            {
                var srt = sub.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
                srt.pivot = new Vector2(0, 1);
                srt.anchoredPosition = new Vector2(94, -68);
                srt.sizeDelta = new Vector2(-240, 22);
                foreach (var tm in sub.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 14;
                    tm.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }

            // 5. Reward (now fully visible — bigger box, more space)
            var reward = card.transform.Find("Reward");
            if (reward != null)
            {
                var rrt = reward.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0, 1);
                rrt.anchoredPosition = new Vector2(94, -94);
                rrt.sizeDelta = new Vector2(-240, 22);
                foreach (var tm in reward.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 16;
                    tm.alignment = TextAlignmentOptions.MidlineLeft;
                    tm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    tm.overflowMode = TextOverflowModes.Overflow;
                }
            }

            // 6. BEGIN button
            var begin = card.transform.Find("BeginBtn");
            if (begin != null)
            {
                var brt = begin.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
                brt.pivot = new Vector2(1, 0.5f);
                brt.anchoredPosition = new Vector2(-16, -10);
                brt.sizeDelta = new Vector2(130, 64);
                foreach (var tm in begin.GetComponentsInChildren<TMP_Text>(true))
                    tm.fontSize = 20;
            }

            // Render order: above environment but below currency / stats / nav
            card.transform.SetAsLastSibling();
            // Then nudge things that should still be on top
            string[] alwaysTop = { "PlayerHUD", "CurrencyHeader", "HomeNavButtons", "BottomNav", "HelpIcon", "ForgePanel" };
            foreach (var n in alwaysTop)
            {
                var go = GameObject.Find(n);
                if (go != null) go.transform.SetAsLastSibling();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Trial card polished:\n\n" +
                "• Card 500×140 (was 440×110)\n" +
                "• Centered in open mid-area, slight right of hero\n" +
                "• Reward text no longer clipped\n" +
                "• Title 20pt, subtitle 14pt, reward 16pt, BEGIN 20pt\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
