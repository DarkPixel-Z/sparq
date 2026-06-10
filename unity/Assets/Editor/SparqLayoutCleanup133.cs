using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 133: Layout cleanup pass.
    /// • Removes duplicate SparqEnv objects
    /// • Lifts currency bar fully into view + sets render order
    /// • Pushes top button row up off the quest box
    /// • Resizes quest box so all 3 rows fit cleanly
    /// • Tightens stats card spacing
    /// </summary>
    public static class SparqLayoutCleanup133
    {
        [MenuItem("Sparq/133. Layout cleanup pass")]
        public static void Apply()
        {
            int dupesRemoved = WipeDuplicateEnv();
            FixCurrencyHeader();
            LiftTopButtons();
            ResizeQuestBox();
            ClampQuestRows();
            BringChromeToFront();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Layout cleaned up:\n\n" +
                $"• {dupesRemoved} duplicate SparqEnv removed\n" +
                "• Currency bar pulled fully into view\n" +
                "• Top buttons lifted away from quest box\n" +
                "• Quest box resized to fit all 3 rows\n" +
                "• UI chrome moved to render last\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // 1. Remove duplicate SparqEnv (keep last/most-recent active one)
        private static int WipeDuplicateEnv()
        {
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject keep = null;
            foreach (var go in all)
            {
                if (go == null || go.name != "SparqEnv") continue;
                if (go.activeSelf) keep = go; // prefer the active one
            }
            if (keep == null)
            {
                foreach (var go in all)
                    if (go != null && go.name == "SparqEnv") { keep = go; break; }
            }
            int removed = 0;
            foreach (var go in all)
            {
                if (go == null || go.name != "SparqEnv" || go == keep) continue;
                Object.DestroyImmediate(go);
                removed++;
            }
            return removed;
        }

        // 2. Currency header visible + on top
        private static void FixCurrencyHeader()
        {
            var header = GameObject.Find("CurrencyHeader");
            if (header == null) return;
            var rt = header.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-12, -16);
                rt.sizeDelta = new Vector2(280, 36);
            }
            header.transform.SetAsLastSibling();

            // Make sure stats card sits below the header
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                if (hrt != null)
                {
                    var p = hrt.anchoredPosition;
                    if (p.y > -54f) hrt.anchoredPosition = new Vector2(p.x, -54f);
                }
            }
        }

        // 3. Top button bar — push up so there's clear gap above quest box
        private static void LiftTopButtons()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;
            var rt = bar.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12, -220);
            rt.sizeDelta = new Vector2(420, 90);
        }

        // 4. Quest box — make it tall enough for 3 rows + header
        private static void ResizeQuestBox()
        {
            // Try common names
            string[] candidates = { "QuestList", "QuestBox", "TodayQuests", "TodaysQuests", "QuestPanel" };
            GameObject box = null;
            foreach (var n in candidates) { var g = GameObject.Find(n); if (g != null) { box = g; break; } }
            if (box == null) return;

            // If parent has the visual frame, walk up one
            Transform target = box.transform;
            if (target.parent != null && target.parent.GetComponent<Image>() != null
                && target.parent.name.ToLower().Contains("quest"))
                target = target.parent;

            var rt = target.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12, -320);
            rt.sizeDelta = new Vector2(420, 200);
        }

        // 4b. Hide quest rows beyond the first 3 (prevents overflow into bottom nav)
        private static void ClampQuestRows()
        {
            string[] candidates = { "QuestList", "QuestBox", "TodayQuests", "TodaysQuests", "QuestPanel" };
            GameObject list = null;
            foreach (var n in candidates) { var g = GameObject.Find(n); if (g != null) { list = g; break; } }
            if (list == null) return;

            // Add a content size limiter via mask + max-height, simpler: deactivate rows past index 2
            int idx = 0;
            for (int i = 0; i < list.transform.childCount; i++)
            {
                var c = list.transform.GetChild(i);
                // Skip header / non-row siblings (look for "Row"/"Quest" in name)
                bool looksLikeRow = c.name.ToLower().Contains("row") || c.name.ToLower().Contains("quest");
                if (!looksLikeRow) continue;
                c.gameObject.SetActive(idx < 3);
                idx++;
            }

            // Also constrain VLG so runtime additions don't expand
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) { vlg.childForceExpandHeight = false; vlg.childControlHeight = false; }
            var fitter = list.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        // 5. Make sure UI chrome renders on top of vignette / env
        private static void BringChromeToFront()
        {
            string[] toFront = {
                "PlayerHUD", "CurrencyHeader", "HomeNavButtons", "BottomNav",
                "QuestList", "QuestBox", "GameTitle", "HelpIcon"
            };
            foreach (var n in toFront)
            {
                var go = GameObject.Find(n);
                if (go != null) go.transform.SetAsLastSibling();
            }
            // Vignette stays behind UI but in front of world
            var v = GameObject.Find("Vignette");
            if (v != null) v.transform.SetSiblingIndex(0);
        }
    }
}
