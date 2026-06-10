using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqWireAvatarCapture
    {
        [MenuItem("Sparq/57. Wire runtime Karu avatar capture")]
        public static void Wire()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "PlayerHUD not in scene. Run Sparq → 55 first.", "OK");
                return;
            }

            // Find the Avatar image inside the HUD
            Image avatar = null;
            foreach (var img in hud.GetComponentsInChildren<Image>(true))
            {
                if (img != null && img.gameObject.name == "Avatar")
                { avatar = img; break; }
            }
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Avatar Image not found in PlayerHUD.", "OK");
                return;
            }

            // Attach KaruAvatarCapture component
            var existing = hud.GetComponent<Sparq.UI.KaruAvatarCapture>();
            if (existing == null) existing = hud.AddComponent<Sparq.UI.KaruAvatarCapture>();
            var so = new SerializedObject(existing);
            so.FindProperty("targetAvatar").objectReferenceValue = avatar;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Avatar",
                "✅ Runtime Karu snapshot wired.\n\n" +
                "On Play, a temp camera snaps the live Karu in scene\n" +
                "and applies it as the HUD avatar — pixel-perfect match.\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
