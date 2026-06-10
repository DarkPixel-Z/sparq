using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Karu/Una were hidden behind the Screen Space Overlay sky canvas.
    /// Switch the sky + rays canvases to Screen Space Camera at far plane
    /// so world-space characters render in front of them.
    /// </summary>
    public static class SparqFixKaruVisibility
    {
        [MenuItem("Sparq/17. Fix Karu/Una visibility (push sky behind)")]
        public static void Fix()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Sparq Fix", "No Main Camera.", "OK");
                return;
            }

            int fixedCount = 0;

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.name == "SkyCanvas")
                {
                    c.renderMode    = RenderMode.ScreenSpaceCamera;
                    c.worldCamera   = cam;
                    c.planeDistance = 90f;
                    c.sortingOrder  = -100;
                    fixedCount++;
                }
                else if (c.name == "RaysCanvas")
                {
                    c.renderMode    = RenderMode.ScreenSpaceCamera;
                    c.worldCamera   = cam;
                    c.planeDistance = 50f;
                    c.sortingOrder  = -50;
                    fixedCount++;
                }
            }

            // Make sure Karu and Una are at z=0 so they sit in front of the sky (z=90)
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                var p = karu.transform.position;
                karu.transform.position = new Vector3(p.x, p.y, 0f);
            }
            var una = GameObject.Find("Una");
            if (una != null)
            {
                var p = una.transform.position;
                una.transform.position = new Vector3(p.x, p.y, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq Fix] Pushed {fixedCount} cinematic canvas(es) behind world sprites.");
            EditorUtility.DisplayDialog("Sparq Fix",
                $"✅ Fixed {fixedCount} canvas(es).\n\n" +
                "Karu and Una should now appear in front of the sky.\n" +
                "Hit ▶ Play to verify.", "OK");
        }
    }
}
