using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqAttachNavBar
    {
        [MenuItem("Sparq/37. Fix home nav buttons (attach HomeNavBar)")]
        public static void Attach()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "HomeNavButtons not found. Run Sparq → 32 first.", "OK");
                return;
            }
            if (bar.GetComponent<Sparq.UI.HomeNavBar>() == null)
                bar.AddComponent<Sparq.UI.HomeNavBar>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ HomeNavBar attached.\n\n" +
                "MAP, SHOP, BAG buttons will re-wire at runtime on every Play.\n\n" +
                "Hit ▶ Play and tap them.", "OK");
        }
    }
}
