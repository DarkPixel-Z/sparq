using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqNudge2
    {
        [MenuItem("Sparq/119. Hero slightly right + Wisp down-left")]
        public static void Apply()
        {
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                var p = karu.transform.position;
                karu.transform.position = new Vector3(-3.4f, p.y, p.z); // -4.0 → -3.4 (slight right)
            }

            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(-0.7f, -2.4f, 0f); // 0.0 → -0.7 (left), -2.0 → -2.4 (down)
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Nudged:\n\n" +
                "• Karu: x=-4.0 → -3.4 (slight right)\n" +
                "• Wisp: 0.0,-2.0 → -0.7,-2.4 (left + down)\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
