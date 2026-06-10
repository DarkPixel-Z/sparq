using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Removes leftover old UI (duplicate XP bar with "99/99" label)
    /// and repositions Una to a visible spot.
    /// </summary>
    public static class SparqCleanup
    {
        [MenuItem("Sparq/20. Cleanup (remove duplicate XP bar + fix Una)")]
        public static void Cleanup()
        {
            int removed = 0;

            // Remove the OLD home-made XP bar pieces from SparqSetup (the "99/99" bar)
            foreach (var goName in new[] { "XPBarContainer", "XPBarBG", "XPBarFill", "LevelText", "XPText" })
            {
                var go = GameObject.Find(goName);
                if (go != null)
                {
                    // Only remove if this is the OLD procedural bar, not the FantasyHero slider
                    // The FantasyHero slider lives under a prefab-instance root named like "Slider_Border_Tapered..."
                    bool isUnderFantasyHero = false;
                    Transform t = go.transform;
                    while (t != null)
                    {
                        if (t.name.Contains("Slider_Border") || t.name.Contains("Fantasy"))
                        { isUnderFantasyHero = true; break; }
                        t = t.parent;
                    }
                    if (!isUnderFantasyHero)
                    {
                        Object.DestroyImmediate(go);
                        removed++;
                    }
                }
            }

            // Also hunt for any text child literally showing "99 / 99" or "99/99"
            foreach (var tmp in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            {
                if (tmp == null) continue;
                var txt = tmp.text;
                if (!string.IsNullOrEmpty(txt) && (txt.Contains("99 / 99") || txt.Contains("99/99")))
                {
                    // Don't kill if inside a FantasyHero slider root
                    bool isFantasy = false;
                    Transform t = tmp.transform;
                    while (t != null)
                    {
                        if (t.name.Contains("Slider_Border") || t.name.Contains("Fantasy")) { isFantasy = true; break; }
                        t = t.parent;
                    }
                    if (!isFantasy)
                    {
                        Object.DestroyImmediate(tmp.gameObject);
                        removed++;
                    }
                }
            }

            // Fix Una — make her visible on the left side of Karu
            var karu = GameObject.Find("Karu");
            var una = GameObject.Find("Una");
            if (una != null && karu != null)
            {
                var kp = karu.transform.position;
                una.transform.position = new Vector3(kp.x - 1.8f, kp.y - 0.4f, 0f);
                una.transform.localScale = Vector3.one * 0.7f; // slightly smaller than Karu
                var sr = una.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var c = sr.color; c.a = 1f; sr.color = c;
                    sr.sortingOrder = 5;
                }
                // If she's been onboarded and hidden, don't force visible — controller handles
                var ctrl = una.GetComponent<Sparq.UI.UnaController>();
                if (ctrl == null || !Sparq.Core.SaveService.Data.onboardingComplete)
                    una.SetActive(true);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq Cleanup] Removed {removed} leftover object(s). Una repositioned.");
            EditorUtility.DisplayDialog("Sparq Cleanup",
                $"✅ Cleanup complete.\n\n" +
                $"• Removed {removed} leftover 99/99 bar piece(s)\n" +
                $"• Una repositioned next to Karu (left-below)\n\n" +
                "Hit ▶ Play — should be clean now.", "OK");
        }
    }
}
