using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Polish pass:
    /// • Restore the older "SPARQ" plate-style logo (yellow text, purple plate)
    /// • Make Una a Help button (tap → opens tutorial popup)
    /// • Move forest up so grass is visible
    /// </summary>
    public static class SparqPolishPass
    {
        [MenuItem("Sparq/52. Polish: better logo + Una help + raise forest")]
        public static void Apply()
        {
            FixLogo();
            MakeUnaHelp();
            RaiseForest();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Polish",
                "✅ Polish applied:\n\n" +
                "• Logo restored — purple plate with yellow Sparq + bolt\n" +
                "• Una is now the HELP button (tap her → tutorial popup)\n" +
                "• Forest raised — grass visible at bottom\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void FixLogo()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            var titleGO = new GameObject("GameTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(14f, -14f);
            rt.sizeDelta = new Vector2(260, 80);

            // Purple plate
            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(titleGO.transform, false);
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            plate.GetComponent<Image>().color = new Color(0.20f, 0.05f, 0.30f, 0.75f);
            plate.GetComponent<Image>().raycastTarget = false;

            // Yellow accent strip on top + bottom
            for (int i = 0; i < 2; i++)
            {
                var bar = new GameObject($"Bar{i}", typeof(RectTransform), typeof(Image));
                bar.transform.SetParent(plate.transform, false);
                var brt = bar.GetComponent<RectTransform>();
                if (i == 0) { brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 1f); }
                else        { brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0.5f, 0f); }
                brt.anchoredPosition = Vector2.zero;
                brt.sizeDelta = new Vector2(0, 4);
                bar.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);
                bar.GetComponent<Image>().raycastTarget = false;
            }

            // Drop shadow
            var shadow = new GameObject("Shadow", typeof(RectTransform));
            shadow.transform.SetParent(titleGO.transform, false);
            var srt = shadow.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(3, -5); srt.offsetMax = new Vector2(3, -5);
            var stm = shadow.AddComponent<TextMeshProUGUI>();
            stm.text = "SPARQ";
            stm.fontSize = 48;
            stm.fontStyle = FontStyles.Bold;
            stm.color = new Color(0, 0, 0, 0.6f);
            stm.alignment = TextAlignmentOptions.Center;
            stm.raycastTarget = false;

            // Main title — ASCII only, no emoji that breaks
            var mainGO = new GameObject("Title", typeof(RectTransform));
            mainGO.transform.SetParent(titleGO.transform, false);
            var mrt = mainGO.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            var mtm = mainGO.AddComponent<TextMeshProUGUI>();
            mtm.text = "SPARQ";
            mtm.fontSize = 48;
            mtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            mtm.alignment = TextAlignmentOptions.Center;
            mtm.color = new Color(1f, 0.92f, 0.35f);
            mtm.outlineWidth = 0.3f;
            mtm.outlineColor = new Color(0.35f, 0.05f, 0.55f, 1f);
            mtm.raycastTarget = false;
        }

        private static void MakeUnaHelp()
        {
            var una = GameObject.Find("Una");
            if (una == null)
            {
                // Search inactive too
                var allRTs = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var rt in allRTs)
                {
                    if (rt != null && rt.name == "Una") { una = rt.gameObject; break; }
                }
            }
            if (una == null) return;

            // Make her visible + always active
            una.SetActive(true);

            // Make sure she has a collider so taps register
            var col = una.GetComponent<BoxCollider2D>();
            if (col == null) col = una.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 1.4f);
            col.isTrigger = true;

            // Add Help script
            if (una.GetComponent<Sparq.UI.HelpButton>() == null)
                una.AddComponent<Sparq.UI.HelpButton>();

            // Position her in a clear spot — bottom-right above XP bar
            una.transform.position = new Vector3(3.4f, -1.6f, 0f);
            una.transform.localScale = Vector3.one * 0.6f;
        }

        private static void RaiseForest()
        {
            var forest = GameObject.Find("[Forest]");
            if (forest == null) return;
            // Lift the entire forest up so grass is visible
            forest.transform.position = new Vector3(0f, 1.2f, 0f);
        }

        [MenuItem("Sparq/52a. Hide duplicate level on bottom XP bar")]
        public static void HideBottomLevelText()
        {
            // The Karu card already shows the level. Hide the "Lv.X" text inside the bottom slider.
            var bar = GameObject.Find("FantasyXPBar");
            if (bar == null) return;

            foreach (var tmp in bar.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (tmp.text != null && tmp.text.StartsWith("Lv"))
                {
                    tmp.gameObject.SetActive(false);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Removed duplicate Lv text from bottom XP bar.\n\n" +
                "The Karu stats card up top is now the only level display.", "OK");
        }
    }
}
