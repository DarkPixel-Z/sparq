using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqAttachUnaController
    {
        [MenuItem("Sparq/19. Attach Una tutorial controller")]
        public static void Attach()
        {
            var una = GameObject.Find("Una");
            if (una == null)
            {
                EditorUtility.DisplayDialog("Sparq Una",
                    "Una GameObject not found in scene.", "OK");
                return;
            }

            if (una.GetComponent<Sparq.UI.UnaController>() == null)
                una.AddComponent<Sparq.UI.UnaController>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Una",
                "✅ UnaController attached.\n\n" +
                "• Una shows while onboardingComplete == false\n" +
                "• On first quest completion, she fades up and vanishes\n" +
                "• Future launches: Una stays hidden\n\n" +
                "(Restart tutorial later via UnaController.RestartOnboarding())", "OK");
        }
    }
}
