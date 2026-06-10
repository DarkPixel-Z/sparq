using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqMicroTweak
    {
        [MenuItem("Sparq/102. Quests lower + Mochi big + HUD covers clouds")]
        public static void Apply()
        {
            // 1. Quest box → slightly LOWER
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchoredPosition = new Vector2(-14f, 90f);  // was 130, drop to 90
            }

            // 2. Mochi — way bigger (was 0.55, now 0.95 — almost Karu's size)
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 0.95f;
                mochi.transform.position = new Vector3(0.4f, -1.2f, 0f);
                var sr = mochi.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 49;
                    var c = sr.color; c.a = 1f; sr.color = c;
                }
            }

            // 3. Karu HUD — BIGGER (covers cloud area top-right)
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(1f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(1f, 1f);
                hrt.anchoredPosition = new Vector2(-14f, -8f);
                hrt.sizeDelta = new Vector2(420, 130); // was 320×96, now 420×130

                // Make sure background is opaque enough to hide the clouds behind it
                var bg = hud.GetComponent<Image>();
                if (bg != null)
                {
                    var c = bg.color;
                    c.a = 0.95f;
                    bg.color = c;
                }

                // Bump avatar to fill more of the bigger box
                var avatarBg = hud.transform.Find("AvatarBg");
                if (avatarBg != null)
                {
                    var arrt = avatarBg.GetComponent<RectTransform>();
                    arrt.anchoredPosition = new Vector2(10, 0);
                    arrt.sizeDelta = new Vector2(110, 110);
                }
                // Name + Level + XP bar — scaled up to match bigger box
                var nameGO = hud.transform.Find("Name");
                if (nameGO != null)
                {
                    var nrt = nameGO.GetComponent<RectTransform>();
                    nrt.anchoredPosition = new Vector2(130, 28);
                    nrt.sizeDelta = new Vector2(260, 36);
                    foreach (var tm in nameGO.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 32;
                        tm.fontStyle = FontStyles.Bold;
                    }
                }
                var levelGO = hud.transform.Find("Level");
                if (levelGO != null)
                {
                    var lrt = levelGO.GetComponent<RectTransform>();
                    lrt.anchoredPosition = new Vector2(130, -10);
                    lrt.sizeDelta = new Vector2(80, 30);
                    foreach (var tm in levelGO.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 18;
                        tm.fontStyle = FontStyles.Bold;
                    }
                }
                var xpGO = hud.transform.Find("XPBg");
                if (xpGO != null)
                {
                    var xrt = xpGO.GetComponent<RectTransform>();
                    xrt.anchoredPosition = new Vector2(220, -10);
                    xrt.sizeDelta = new Vector2(170, 20);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Tweaks applied:\n\n" +
                "• Quest box: y 130 → 90 (lower)\n" +
                "• Mochi: 0.55 → 0.95 (~Karu size)\n" +
                "• HUD: 320×96 → 420×130 (covers clouds)\n" +
                "• HUD bg alpha 95% (hides clouds behind it)\n" +
                "• Avatar 110×110, name 32pt, XP bar 170×20\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
