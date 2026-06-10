using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 144: Raise Karu/Wisp level badges so they're not covered
    /// by the XP bars. Also nudges the XP bars down a hair for clearance.
    /// </summary>
    public static class SparqLiftLvls144
    {
        [MenuItem("Sparq/144. Raise Lv badges (clear of XP bars)")]
        public static void Apply()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            FixRow(hud.transform.Find("KaruRow"));
            FixRow(hud.transform.Find("MochiRow"));

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Lv badges raised, XP bars nudged down.\n\nHit ▶ Play.", "OK");
        }

        private static void FixRow(Transform row)
        {
            if (row == null) return;

            // Lv badge: lift it well clear of the bar
            var lvl = row.Find("Level");
            if (lvl != null)
            {
                var lrt = lvl.GetComponent<RectTransform>();
                if (lrt != null)
                {
                    // Anchor to TOP-LEFT of row (consistent regardless of row size)
                    lrt.anchorMin = new Vector2(0, 1);
                    lrt.anchorMax = new Vector2(0, 1);
                    lrt.pivot     = new Vector2(0, 1);
                    lrt.anchoredPosition = new Vector2(58, -22);  // sits below the name
                    lrt.sizeDelta = new Vector2(44, 18);
                }
            }

            // Name: anchor top-left so it doesn't shift when row resizes
            var name = row.Find("Name");
            if (name != null)
            {
                var nrt = name.GetComponent<RectTransform>();
                if (nrt != null)
                {
                    nrt.anchorMin = new Vector2(0, 1);
                    nrt.anchorMax = new Vector2(0, 1);
                    nrt.pivot     = new Vector2(0, 1);
                    nrt.anchoredPosition = new Vector2(58, -2);
                    nrt.sizeDelta = new Vector2(190, 22);
                }
            }

            // XP bar: pin to bottom of row with small inset, away from Lv badge
            var bar = row.Find("XPBar");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                if (brt != null)
                {
                    brt.anchorMin = new Vector2(0, 0);
                    brt.anchorMax = new Vector2(1, 0);
                    brt.pivot     = new Vector2(0.5f, 0);
                    brt.anchoredPosition = new Vector2(28, 4);
                    brt.sizeDelta = new Vector2(-70, 11);
                }
            }
        }
    }
}
