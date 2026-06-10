using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqLayoutAdjust
    {
        [MenuItem("Sparq/94. Quests bottom-right + bigger Karu stats")]
        public static void Apply()
        {
            // 1. Quest list → bottom-right, just above bottom nav (~80px tall)
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchorMin = new Vector2(1f, 0f);
                qrt.anchorMax = new Vector2(1f, 0f);
                qrt.pivot     = new Vector2(1f, 0f);
                qrt.anchoredPosition = new Vector2(-14f, 90f);  // 90 above bottom (nav is 80)
                qrt.sizeDelta = new Vector2(340, 280);
            }

            // 2. Karu HUD → bigger (~50% larger)
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.sizeDelta = new Vector2(320, 96);  // was 220x60

                // Bump avatar to fill the bigger box
                var avatarBg = hud.transform.Find("AvatarBg");
                if (avatarBg != null)
                {
                    var abrt = avatarBg.GetComponent<RectTransform>();
                    abrt.anchoredPosition = new Vector2(8, 0);
                    abrt.sizeDelta = new Vector2(80, 80);
                }
                // Name + Level text bigger
                var nameGO = hud.transform.Find("Name");
                if (nameGO != null)
                {
                    var tm = nameGO.GetComponent<TMP_Text>();
                    if (tm != null) { tm.fontSize = 28; tm.fontStyle = FontStyles.Bold; }
                    var nrt = nameGO.GetComponent<RectTransform>();
                    nrt.anchoredPosition = new Vector2(98, 18);
                    nrt.sizeDelta = new Vector2(200, 32);
                }
                var levelGO = hud.transform.Find("Level");
                if (levelGO != null)
                {
                    var lrt = levelGO.GetComponent<RectTransform>();
                    lrt.anchoredPosition = new Vector2(98, -14);
                    lrt.sizeDelta = new Vector2(76, 28);
                    foreach (var tm in levelGO.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 16;
                        tm.fontStyle = FontStyles.Bold;
                    }
                }
                // XP bar bigger
                var xpGO = hud.transform.Find("XPBg");
                if (xpGO != null)
                {
                    var xrt = xpGO.GetComponent<RectTransform>();
                    xrt.anchoredPosition = new Vector2(180, -14);
                    xrt.sizeDelta = new Vector2(120, 16);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Layout adjusted:\n\n" +
                "• Quests → bottom-right, above bottom nav (340×280)\n" +
                "• Karu HUD → 320×96 (was 220×60), 50% larger\n" +
                "• Avatar 80×80, name 28pt, level badge 76px wide\n" +
                "• XP bar 120×16\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
