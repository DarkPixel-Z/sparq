using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

namespace Sparq.Editor
{
    /// <summary>
    /// Removes duplicate XP bar instances from the Home scene.
    /// Keeps the one that has an XPBarDisplay controller wired (or the first found).
    /// Also strips any leftover "99/99" text label.
    /// </summary>
    public static class SparqDedupeBars
    {
        [MenuItem("Sparq/22. Remove duplicate XP bars")]
        public static void Dedupe()
        {
            // Find every XP-bar-looking object in the canvas:
            //   FantasyXPBar (renamed by SparqUIUpgrade), Slider_Border_* (raw prefab clones), XPBarContainer (legacy procedural)
            var candidates = new List<GameObject>();
            var seen = new HashSet<int>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null) continue;
                bool match = t.name == "FantasyXPBar"
                          || t.name.StartsWith("Slider_Border_Tapered")
                          || t.name == "XPBarContainer"
                          || t.name.StartsWith("FantasyXPBar"); // includes any (1), (Clone) suffixes
                if (match && !seen.Contains(t.gameObject.GetInstanceID()))
                {
                    // Skip if this is nested inside another candidate (we only want top-level)
                    bool nested = false;
                    Transform p = t.parent;
                    while (p != null)
                    {
                        if (p.name == "FantasyXPBar"
                            || p.name.StartsWith("Slider_Border_Tapered")
                            || p.name == "XPBarContainer"
                            || p.name.StartsWith("FantasyXPBar"))
                        {
                            nested = true; break;
                        }
                        p = p.parent;
                    }
                    if (!nested)
                    {
                        candidates.Add(t.gameObject);
                        seen.Add(t.gameObject.GetInstanceID());
                    }
                }
            }

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("Sparq Dedupe",
                    "No XP bar objects found. Nothing to do.", "OK");
                return;
            }

            // Pick the keeper: prefer one with XPBarDisplay attached
            GameObject keeper = null;
            foreach (var c in candidates)
            {
                if (c.GetComponent<Sparq.UI.XPBarDisplay>() != null
                    || c.GetComponentInChildren<Sparq.UI.XPBarDisplay>() != null)
                {
                    keeper = c;
                    break;
                }
            }
            if (keeper == null) keeper = candidates[0];

            int removed = 0;
            foreach (var c in candidates)
            {
                if (c == keeper) continue;
                Object.DestroyImmediate(c);
                removed++;
            }

            // Wipe any leftover "99/99" labels (could be inside the keeper or freestanding)
            int labelsZapped = 0;
            foreach (var tmp in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            {
                if (tmp == null) continue;
                var t = tmp.text;
                if (string.IsNullOrEmpty(t)) continue;
                if (t.Contains("99 / 99") || t.Contains("99/99"))
                {
                    Object.DestroyImmediate(tmp.gameObject);
                    labelsZapped++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq Dedupe] Removed {removed} duplicate bar(s) and {labelsZapped} stale 99/99 label(s). Kept: {keeper.name}");
            EditorUtility.DisplayDialog("Sparq Dedupe",
                $"✅ Done.\n\n" +
                $"• Found {candidates.Count} XP bar(s)\n" +
                $"• Removed {removed} duplicate(s)\n" +
                $"• Wiped {labelsZapped} leftover '99/99' label(s)\n" +
                $"• Kept: {keeper.name}", "OK");
        }
    }
}
