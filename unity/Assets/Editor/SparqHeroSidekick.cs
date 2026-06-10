using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqHeroSidekick
    {
        [MenuItem("Sparq/98. Hero LEFT + Mochi BIGGER (matched proportions)")]
        public static void Apply()
        {
            // Karu → LEFT side of screen, larger
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.position = new Vector3(-2.0f, -1.0f, 0f);  // pulled left
                karu.transform.localScale = Vector3.one * 0.45f;
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 50;
            }

            // Mochi → next to Karu, ~85% of Karu's size, in front of him so visible
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(-0.4f, -1.4f, 0f);  // closer + slightly right of Karu
                mochi.transform.localScale = Vector3.one * 0.38f;          // bigger now (was 0.32)
                var sr = mochi.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 49; // just behind Karu
                    var c = sr.color; c.a = 1f; sr.color = c;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Hero + sidekick repositioned:\n\n" +
                "• Karu → LEFT (x=-2.0, y=-1.0), scale 0.45\n" +
                "• Mochi → next to Karu (x=-0.4, y=-1.4), scale 0.38 (~85% of Karu)\n" +
                "• Mochi alpha 100% (was slightly transparent)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/98a. Mochi EVEN BIGGER (same as Karu)")]
        public static void Equal()
        {
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 0.45f;
                var sr = mochi.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 49;
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq", "✅ Mochi at full Karu size (0.45). Hit ▶ Play.", "OK");
        }
    }
}
