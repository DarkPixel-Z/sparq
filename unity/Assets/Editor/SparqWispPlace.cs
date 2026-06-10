using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqWispPlace
    {
        [MenuItem("Sparq/117. Wisp → right + down to cape bottom")]
        public static void Apply()
        {
            var mochi = GameObject.Find("Mochi");
            if (mochi == null) return;

            var karu = GameObject.Find("Karu");
            float kx = karu != null ? karu.transform.position.x : -3.0f;

            // Position: right of Karu + lower (cape bottom level)
            mochi.transform.position = new Vector3(kx + 1.5f, -2.5f, 0f);
            mochi.transform.localScale = Vector3.one * 0.7f;  // slightly smaller for cape-buddy feel

            var sr = mochi.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 51; // in front of Karu so visible peeking from cape

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Wisp placed:\n\n" +
                $"• Position: x={kx + 1.5f:F1} (right of Karu), y=-2.5 (cape bottom)\n" +
                $"• Scale: 0.7\n" +
                $"• Sort order: 51 (in front of Karu)\n\n" +
                "Hit ▶ Play. If still off, tell me 'higher / lower / closer / further'.", "OK");
        }
    }
}
