using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 146: Shift the top button bar (HomeNavButtons) left a tiny bit.
    /// </summary>
    public static class SparqShiftTopBar146
    {
        [MenuItem("Sparq/146. Shift top button bar left a bit")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK"); return; }

            var rt = bar.GetComponent<RectTransform>();
            if (rt == null) return;

            // Anchor flush right + pull left with bigger offset
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-58, rt.anchoredPosition.y);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Top button bar shifted left.\n\nx offset → -90 from right edge.\n\nHit ▶ Play.", "OK");
        }
    }
}
