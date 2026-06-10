using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqMochiAdjust
    {
        [MenuItem("Sparq/112. Mochi bigger + left + down")]
        public static void Apply()
        {
            var mochi = GameObject.Find("Mochi");
            if (mochi == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Mochi not in scene.", "OK");
                return;
            }

            mochi.transform.localScale = Vector3.one * 1.1f;            // big — was 0.85
            mochi.transform.position = new Vector3(-3.5f, -1.8f, 0f);    // far left + lower
            var sr = mochi.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 49;
                var c = sr.color; c.a = 1f; sr.color = c;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Mochi adjusted:\n\n" +
                "• Scale 0.85 → 1.1 (much bigger)\n" +
                "• Position: x=-3.5 (far left), y=-1.8 (lower)\n\n" +
                "Hit ▶ Play. If still wrong, try Sparq → 112a / 112b for variants.", "OK");
        }

        [MenuItem("Sparq/112a. Mochi HUGE")]
        public static void Huge()
        {
            var mochi = GameObject.Find("Mochi");
            if (mochi == null) return;
            mochi.transform.localScale = Vector3.one * 1.5f;
            mochi.transform.position = new Vector3(-3.8f, -2.0f, 0f);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Sparq/112b. Mochi same size as Karu")]
        public static void EqualKaru()
        {
            var karu = GameObject.Find("Karu");
            var mochi = GameObject.Find("Mochi");
            if (karu == null || mochi == null) return;
            mochi.transform.localScale = karu.transform.localScale;
            mochi.transform.position = new Vector3(karu.transform.position.x - 2.5f, karu.transform.position.y - 0.4f, 0);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
