using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Finds the duplicate XP bar by hunting for the literal "99/99" or "99 / 99"
    /// text label, walking up to the closest ancestor that owns a Slider, and
    /// nuking that whole subtree. Survives whatever weird names it has.
    /// </summary>
    public static class SparqNuke9999
    {
        [MenuItem("Sparq/24. Nuke duplicate '99/99' XP bar")]
        public static void Nuke()
        {
            int killed = 0;

            // Repeat until no more matches (in case more than one)
            for (int safety = 0; safety < 5; safety++)
            {
                TMP_Text hit = null;
                foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                {
                    if (tmp == null) continue;
                    var t = tmp.text ?? "";
                    if (t.Contains("99/99") || t.Contains("99 / 99"))
                    {
                        hit = tmp;
                        break;
                    }
                }
                if (hit == null) break;

                // Walk up to find the bar root: closest ancestor with a Slider OR a top-level direct-canvas-child
                Transform root = hit.transform;
                Transform barRoot = null;
                Transform p = root;
                while (p != null)
                {
                    if (p.GetComponent<Slider>() != null)
                    {
                        barRoot = p;
                        break;
                    }
                    p = p.parent;
                }
                // If no slider in ancestry, just kill the text + its immediate parent
                if (barRoot == null) barRoot = hit.transform.parent ?? hit.transform;

                Debug.Log($"[Sparq Nuke] Killing '{barRoot.name}' (path: {GetPath(barRoot)}) — contained '99/99'");
                Object.DestroyImmediate(barRoot.gameObject);
                killed++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Nuke",
                killed > 0
                  ? $"✅ Removed {killed} object(s) containing '99/99' label.\n\nHit ▶ Play."
                  : "No '99/99' label found.\n\nIf you still see two bars, click each in the Hierarchy and tell me their exact names.",
                "OK");
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "";
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }
    }
}
