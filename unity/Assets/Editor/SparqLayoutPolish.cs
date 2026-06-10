using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqLayoutPolish
    {
        [MenuItem("Sparq/106. Polish (logo fills button + Mochi HUD same size + bigger Mochi)")]
        public static void Apply()
        {
            // 1. Logo fills the frame (no padding)
            var title = GameObject.Find("GameTitle");
            if (title != null)
            {
                var logo = title.transform.Find("Logo");
                if (logo != null)
                {
                    var lrt = logo.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero;       // no padding
                    lrt.offsetMax = Vector2.zero;
                }
            }

            // 2. Mochi HUD → SAME width as Karu HUD, directly below
            var karuHUD = GameObject.Find("PlayerHUD");
            var mochiHUD = GameObject.Find("MochiHUD");
            if (karuHUD != null && mochiHUD != null)
            {
                var krt = karuHUD.GetComponent<RectTransform>();
                var mrt = mochiHUD.GetComponent<RectTransform>();

                // Karu top-right
                krt.anchorMin = new Vector2(1f, 1f);
                krt.anchorMax = new Vector2(1f, 1f);
                krt.pivot     = new Vector2(1f, 1f);
                krt.anchoredPosition = new Vector2(-14f, -8f);
                krt.sizeDelta = new Vector2(420, 110);

                // Mochi same width, directly below
                mrt.anchorMin = new Vector2(1f, 1f);
                mrt.anchorMax = new Vector2(1f, 1f);
                mrt.pivot     = new Vector2(1f, 1f);
                mrt.anchoredPosition = new Vector2(-14f, -125f);  // just below Karu HUD
                mrt.sizeDelta = new Vector2(420, 60);             // same width

                // Make Mochi HUD bigger inside
                var avBg = mochiHUD.transform.Find("AvatarBg");
                if (avBg != null)
                {
                    var arrt = avBg.GetComponent<RectTransform>();
                    arrt.anchoredPosition = new Vector2(8, 0);
                    arrt.sizeDelta = new Vector2(50, 50);
                }
                var nameGO = mochiHUD.transform.Find("Name");
                if (nameGO != null)
                {
                    var nrt = nameGO.GetComponent<RectTransform>();
                    nrt.anchoredPosition = new Vector2(72, 8);
                    nrt.sizeDelta = new Vector2(140, 28);
                    foreach (var tm in nameGO.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 22;
                    }
                }
                var lvlGO = mochiHUD.transform.Find("Level");
                if (lvlGO != null)
                {
                    var lrt = lvlGO.GetComponent<RectTransform>();
                    lrt.anchoredPosition = new Vector2(72, -16);
                    lrt.sizeDelta = new Vector2(60, 22);
                    foreach (var tm in lvlGO.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 14;
                    }
                }
                var subGO = mochiHUD.transform.Find("Subtitle");
                if (subGO != null)
                {
                    var srt = subGO.GetComponent<RectTransform>();
                    srt.anchoredPosition = new Vector2(140, -16);
                    srt.sizeDelta = new Vector2(260, 20);
                    foreach (var tm in subGO.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 12;
                    }
                }
            }

            // 3. Top buttons → just above quest box, right side, straight down stack
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(1f, 1f);
                brt.anchoredPosition = new Vector2(-14f, -200f);  // below Mochi HUD
                brt.sizeDelta = new Vector2(420, 40);
            }

            // 4. Quest box → just below buttons
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchorMin = new Vector2(1f, 1f);
                qrt.anchorMax = new Vector2(1f, 1f);
                qrt.pivot = new Vector2(1f, 1f);
                qrt.anchoredPosition = new Vector2(-14f, -250f);
                qrt.sizeDelta = new Vector2(420, 270);
            }

            // 5. Una help icon → smaller, just above bottom buttons (which are 80px tall)
            var help = GameObject.Find("HelpIcon");
            if (help != null)
            {
                var hrt = help.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0f, 0f);
                hrt.anchorMax = new Vector2(0f, 0f);
                hrt.pivot = new Vector2(0f, 0f);
                hrt.anchoredPosition = new Vector2(8f, 88f);   // just above bottom nav (80)
                hrt.sizeDelta = new Vector2(56, 56);           // smaller (was 70)

                var badge = help.transform.Find("Badge");
                if (badge != null)
                {
                    var brt = badge.GetComponent<RectTransform>();
                    brt.sizeDelta = new Vector2(20, 20);
                    foreach (var tmp in badge.GetComponentsInChildren<TMP_Text>(true))
                        tmp.fontSize = 14;
                }
            }

            // 6. Mochi character — actually visible (was 0.32, make it 0.85)
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 0.85f;
                mochi.transform.position = new Vector3(-0.6f, -1.1f, 0f);
                var sr = mochi.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sortingOrder = 49; var c = sr.color; c.a = 1f; sr.color = c; }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Polish applied:\n\n" +
                "• Logo image fills the button frame (no padding)\n" +
                "• Mochi HUD: 420×60, directly below Karu HUD (same width)\n" +
                "• Top buttons: stacked below Mochi HUD on right\n" +
                "• Quest box: stacked below top buttons\n" +
                "• Una help icon: 56×56, just above bottom nav\n" +
                "• Mochi character scale: 0.85 (was tiny)\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
