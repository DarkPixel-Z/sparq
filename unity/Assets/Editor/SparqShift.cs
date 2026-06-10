using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqShift
    {
        [MenuItem("Sparq/118. Karu further left + Wisp further right")]
        public static void Apply()
        {
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.position = new Vector3(-4.0f, -1.6f, 0f);  // further left (was -3.0)
            }

            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(0.0f, -2.0f, 0f);  // further right (was -1.5)
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Shifted:\n\n" +
                "• Karu: x=-3.0 → -4.0 (further left)\n" +
                "• Wisp: x=-1.5 → 0.0 (further right, near center)\n\n" +
                "Hit ▶ Play. Run again to shift more if needed.", "OK");
        }
    }
}
