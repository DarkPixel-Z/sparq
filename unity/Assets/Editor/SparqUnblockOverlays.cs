using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 997: Disable input-blocking on visual-only overlay canvases
    /// (VignetteCanvas, RaysCanvas, SkyCanvas) so clicks pass through to the UI.
    /// </summary>
    public static class SparqUnblockOverlays
    {
        private static readonly string[] VFX_CANVASES = {
            "VignetteCanvas", "RaysCanvas", "SkyCanvas",
        };

        [MenuItem("Sparq/997. Unblock VFX overlays (vignette/rays)")]
        public static void Apply()
        {
            int touched = 0;
            foreach (var name in VFX_CANVASES)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                // Remove the GraphicRaycaster — these canvases are visual-only
                var gr = go.GetComponent<GraphicRaycaster>();
                if (gr != null)
                {
                    Object.DestroyImmediate(gr);
                    touched++;
                }
                // Also turn off raycastTarget on every Image inside
                foreach (var img in go.GetComponentsInChildren<Graphic>(true))
                {
                    if (img.raycastTarget) { img.raycastTarget = false; touched++; }
                }
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
            EditorUtility.DisplayDialog("Sparq",
                touched > 0
                    ? $"✅ Unblocked {touched} VFX raycast(s).\nClicks will now pass through.\nHit ▶ Play."
                    : "No VFX canvases found needing fixes.",
                "OK");
        }
    }
}
