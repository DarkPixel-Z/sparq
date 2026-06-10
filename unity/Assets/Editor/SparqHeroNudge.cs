using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqHeroNudge
    {
        [MenuItem("Sparq/115. Hero up + slightly smaller")]
        public static void Apply()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null) return;

            karu.transform.localScale = Vector3.one * 0.48f;          // slightly smaller (was 0.55)
            karu.transform.position = new Vector3(-3.0f, -1.6f, 0f);   // up (was -2.4)

            // Mochi follow up too, stay left of Karu
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 1.2f;
                mochi.transform.position = new Vector3(-5.0f, -1.8f, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Karu nudged:\n\n" +
                "• Scale 0.55 → 0.48 (slightly smaller)\n" +
                "• Position y=-2.4 → -1.6 (up, just above yellow line)\n" +
                "• Mochi scale 1.4 → 1.2, follows up\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
