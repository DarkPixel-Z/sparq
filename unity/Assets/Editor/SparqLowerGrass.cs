using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqLowerGrass
    {
        [MenuItem("Sparq/85. Lower grass slightly")]
        public static void Apply()
        {
            var forest = GameObject.Find("[Forest]");
            if (forest == null) return;
            forest.transform.position = new Vector3(0f, 1.2f, 0f); // was 2.0, now 1.2

            var karu = GameObject.Find("Karu");
            if (karu != null && karu.activeSelf)
            {
                karu.transform.position = new Vector3(0f, -0.2f, 0f);
            }
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(1.4f, -0.6f, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Grass dropped from y=2.0 to y=1.2.\n\n" +
                "• Karu repositioned slightly lower\n" +
                "• Mochi repositioned alongside\n\n" +
                "If still too high, run again or tweak the [Forest] Y manually.\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
