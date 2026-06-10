using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqHUDSmaller
    {
        [MenuItem("Sparq/122. HUD even smaller + yellow divider above Wisp")]
        public static void Apply()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Slightly smaller: 280×100
            var rt = hud.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 100);

            // Each row: 46px
            ShrinkRow(hud.transform.Find("KaruRow"), 0f);
            ShrinkRow(hud.transform.Find("MochiRow"), -50f);

            // Replace divider with YELLOW line
            var divider = hud.transform.Find("Divider");
            if (divider == null)
            {
                divider = new GameObject("Divider", typeof(RectTransform), typeof(Image)).transform;
                divider.SetParent(hud.transform, false);
            }
            var drt = divider.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 1);
            drt.anchorMax = new Vector2(1, 1);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0, -50f);
            drt.sizeDelta = new Vector2(-12, 3);  // 3px tall yellow line, slight inset

            var dimg = divider.GetComponent<Image>();
            if (dimg == null) dimg = divider.gameObject.AddComponent<Image>();
            dimg.color = new Color(1f, 0.85f, 0.35f, 0.95f); // gold
            dimg.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ HUD even smaller + gold divider:\n\n" +
                "• Total: 320×120 (was 380×150)\n" +
                "• Row: 56px (was 70)\n" +
                "• Avatar: 44×44\n" +
                "• Name: 16pt\n" +
                "• Yellow accent line between Karu and Wisp\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void ShrinkRow(Transform row, float yOffset)
        {
            if (row == null) return;
            var rt = row.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0, yOffset - 6);
                rt.sizeDelta = new Vector2(-16, 46);
            }

            var avBg = row.Find("AvatarBg");
            if (avBg != null)
            {
                var arrt = avBg.GetComponent<RectTransform>();
                arrt.anchoredPosition = new Vector2(6, 0);
                arrt.sizeDelta = new Vector2(36, 36);
            }

            foreach (Transform t in row)
            {
                if (t.name == "Name")
                {
                    var nrt = t.GetComponent<RectTransform>();
                    if (nrt != null) { nrt.anchoredPosition = new Vector2(58, 10); nrt.sizeDelta = new Vector2(180, 22); }
                    foreach (var tm in t.GetComponentsInChildren<TMP_Text>(true)) tm.fontSize = 16;
                }
                if (t.name == "Level")
                {
                    var lrt = t.GetComponent<RectTransform>();
                    if (lrt != null) { lrt.anchoredPosition = new Vector2(58, -10); lrt.sizeDelta = new Vector2(44, 18); }
                    foreach (var tm in t.GetComponentsInChildren<TMP_Text>(true)) tm.fontSize = 11;
                }
                if (t.name == "Subtitle")
                {
                    var srt = t.GetComponent<RectTransform>();
                    if (srt != null) { srt.anchoredPosition = new Vector2(108, -10); srt.sizeDelta = new Vector2(160, 16); }
                    foreach (var tm in t.GetComponentsInChildren<TMP_Text>(true)) tm.fontSize = 10;
                }
                if (t.name == "XPSlider")
                {
                    var xrt = t.GetComponent<RectTransform>();
                    if (xrt != null) { xrt.anchoredPosition = new Vector2(108, -10); xrt.sizeDelta = new Vector2(160, 12); }
                }
            }
        }
    }
}
