using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqHeroSize
    {
        [MenuItem("Sparq/88. Make hero bigger (current: tiny)")]
        public static void Bigger()
        {
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.localScale = Vector3.one * 0.05f; // ~3x bigger than 0.018
                karu.transform.position = new Vector3(0f, -0.4f, 0f);
            }

            // Squad members — smaller than hero but still visible
            var squad = GameObject.Find("[HeroSquad]");
            if (squad != null)
            {
                foreach (Transform t in squad.transform)
                {
                    t.localScale = Vector3.one * 0.030f; // ~2.5x bigger than 0.012
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Heroes scaled up.\n\n" +
                "• Karu: 0.018 → 0.05 (~3x bigger)\n" +
                "• Squad: 0.012 → 0.030 (~2.5x bigger)\n\n" +
                "If still too small, run Sparq → 88a (huge).\n" +
                "Hit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/88a. Hero EVEN BIGGER (huge)")]
        public static void Huge() => Resize(0.08f, 0.05f, -0.2f);

        [MenuItem("Sparq/88b. Hero MASSIVE (hero shot)")]
        public static void Massive() => Resize(0.12f, 0.07f, 0.0f);

        [MenuItem("Sparq/88c. Hero GIANT (fills middle)")]
        public static void Giant() => Resize(0.18f, 0.09f, 0.3f);

        private static void Resize(float karuScale, float squadScale, float karuY)
        {
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.localScale = Vector3.one * karuScale;
                karu.transform.position = new Vector3(0f, karuY, 0f);
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 15;
            }
            // Mochi scales with Karu (similar size, slightly smaller)
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * (karuScale * 0.7f);
                mochi.transform.position = new Vector3(2f, karuY - 0.4f, 0f);
                var msr = mochi.GetComponent<SpriteRenderer>();
                if (msr != null) msr.sortingOrder = 14;
            }
            var squad = GameObject.Find("[HeroSquad]");
            if (squad != null)
            {
                foreach (Transform t in squad.transform)
                {
                    t.localScale = Vector3.one * squadScale;
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 13;
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                $"✅ Hero={karuScale}, Mochi={(karuScale*0.7f):F3}, Squad={squadScale}.\n\nHit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/88d. Hero MEGA (cinematic centerpiece)")]
        public static void Mega() => Resize(0.25f, 0.10f, 0.5f);

        [MenuItem("Sparq/88e. Hero ULTRA (hero takes the screen)")]
        public static void Ultra() => Resize(0.35f, 0.12f, 0.8f);
    }
}
