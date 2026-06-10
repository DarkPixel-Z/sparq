using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqHUDCompact
    {
        [MenuItem("Sparq/121. HUD compact (smaller ratio)")]
        public static void Apply()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Total HUD: 380×150 (was 420×200) — tighter
            var rt = hud.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 150);

            // Each row: 70px (was 90)
            ShrinkRow(hud.transform.Find("KaruRow"), 0f);
            ShrinkRow(hud.transform.Find("MochiRow"), -75f);

            // Adjust divider
            var divider = hud.transform.Find("Divider");
            if (divider != null)
            {
                var drt = divider.GetComponent<RectTransform>();
                drt.anchoredPosition = new Vector2(0, -75f);
                drt.sizeDelta = new Vector2(-20, 1);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ HUD compacted:\n\n" +
                "• Total: 380×150 (was 420×200)\n" +
                "• Each row: 70px tall (was 90)\n" +
                "• Avatar: 56×56 (was 70×70)\n" +
                "• Name: 20pt (was 24)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void ShrinkRow(Transform row, float yOffset)
        {
            if (row == null) return;
            var rt = row.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0, yOffset - 6);
                rt.sizeDelta = new Vector2(-16, 70);
            }

            // Avatar BG → 56×56
            var avBg = row.Find("AvatarBg");
            if (avBg != null)
            {
                var arrt = avBg.GetComponent<RectTransform>();
                arrt.anchoredPosition = new Vector2(6, 0);
                arrt.sizeDelta = new Vector2(56, 56);
            }

            foreach (Transform t in row)
            {
                if (t.name == "Name")
                {
                    var nrt = t.GetComponent<RectTransform>();
                    if (nrt != null)
                    {
                        nrt.anchoredPosition = new Vector2(70, 14);
                        nrt.sizeDelta = new Vector2(200, 26);
                    }
                    foreach (var tm in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 20;
                    }
                }
                if (t.name == "Level")
                {
                    var lrt = t.GetComponent<RectTransform>();
                    if (lrt != null)
                    {
                        lrt.anchoredPosition = new Vector2(70, -12);
                        lrt.sizeDelta = new Vector2(50, 20);
                    }
                    foreach (var tm in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 12;
                    }
                }
                if (t.name == "Subtitle")
                {
                    var srt = t.GetComponent<RectTransform>();
                    if (srt != null)
                    {
                        srt.anchoredPosition = new Vector2(128, -12);
                        srt.sizeDelta = new Vector2(180, 18);
                    }
                    foreach (var tm in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 11;
                    }
                }
                if (t.name == "XPSlider")
                {
                    var xrt = t.GetComponent<RectTransform>();
                    if (xrt != null)
                    {
                        xrt.anchoredPosition = new Vector2(128, -12);
                        xrt.sizeDelta = new Vector2(180, 14);
                    }
                }
            }
        }
    }
}
