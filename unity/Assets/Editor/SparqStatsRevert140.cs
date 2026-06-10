using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 140: Revert the bad stats-frame change from #139.
    /// Restores the dark navy stats card background and re-runs #138's row layout.
    /// </summary>
    public static class SparqStatsRevert140
    {
        private static readonly Color CARD_NAVY = new Color(0.10f, 0.08f, 0.18f, 0.92f);
        private static readonly Color CREAM     = new Color(1.00f, 0.95f, 0.82f);

        [MenuItem("Sparq/140. Revert stats frame (back to dark navy card)")]
        public static void Apply()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Strip the white frame layers from #139
            string[] removeNames = { "FrameBg", "FrameBorder", "FrameInnerBorder", "FrameGradient" };
            for (int i = hud.transform.childCount - 1; i >= 0; i--)
            {
                var c = hud.transform.GetChild(i);
                foreach (var n in removeNames)
                    if (c.name == n) { Object.DestroyImmediate(c.gameObject); break; }
            }

            // Restore the dark navy bg on the HUD itself
            var img = hud.GetComponent<Image>();
            if (img == null) img = hud.AddComponent<Image>();
            img.sprite = null;          // no fancy frame, plain rounded rect color
            img.color  = CARD_NAVY;
            img.type   = Image.Type.Simple;

            // Restore avatar bgs to plain dark circles
            RestoreAvatarBg(hud.transform.Find("KaruRow/AvatarBg"));
            RestoreAvatarBg(hud.transform.Find("MochiRow/AvatarBg"));

            // Wipe duplicate XP bars (in case #139 left two)
            WipeDuplicateXPBars(hud.transform.Find("KaruRow"));
            WipeDuplicateXPBars(hud.transform.Find("MochiRow"));

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Reverted.\n\n" +
                "• White frame removed\n" +
                "• Dark navy card restored\n" +
                "• Avatar circles restored\n" +
                "• Duplicate XP bars cleaned\n\n" +
                "Now re-run Sparq → 138 to refresh row positions.\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void RestoreAvatarBg(Transform avBg)
        {
            if (avBg == null) return;
            var img = avBg.GetComponent<Image>();
            if (img == null) img = avBg.gameObject.AddComponent<Image>();
            img.sprite = null;
            img.color  = new Color(0, 0, 0, 0.55f);
            img.type   = Image.Type.Simple;
            img.preserveAspect = true;

            // Pull the inner avatar back to fill the circle
            for (int i = 0; i < avBg.childCount; i++)
            {
                var child = avBg.GetChild(i);
                var rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
                }
            }
        }

        private static void WipeDuplicateXPBars(Transform row)
        {
            if (row == null) return;
            int kept = 0;
            for (int i = row.childCount - 1; i >= 0; i--)
            {
                var c = row.GetChild(i);
                if (c.name == "XPBar")
                {
                    if (kept == 0) { kept++; continue; }
                    Object.DestroyImmediate(c.gameObject);
                }
            }
        }
    }
}
