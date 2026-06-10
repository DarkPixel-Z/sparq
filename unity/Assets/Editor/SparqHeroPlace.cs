using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqHeroPlace
    {
        [MenuItem("Sparq/114. Hero bigger (medium-large) + left + just above buttons")]
        public static void Apply()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null) return;

            karu.transform.localScale = Vector3.one * 0.55f;          // bigger than 0.45 default, not max
            karu.transform.position = new Vector3(-3.0f, -2.4f, 0f);   // left + low (just above bottom nav)
            var sr = karu.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 50;

            // Keep Mochi positioned relative to Karu's new spot
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                // If mochi was set to position past hero, keep her there relative
                mochi.transform.position = new Vector3(-5.5f, -2.6f, 0f); // further left + lower
                mochi.transform.localScale = Vector3.one * 1.4f;
                var msr = mochi.GetComponent<SpriteRenderer>();
                if (msr != null) msr.sortingOrder = 49;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Hero placed:\n\n" +
                "• Karu scale 0.55 (medium-large)\n" +
                "• Position: x=-3.0, y=-2.4 (left + just above bottom nav)\n" +
                "• Mochi scale 1.4, position (-5.5, -2.6) — past Karu on left\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
