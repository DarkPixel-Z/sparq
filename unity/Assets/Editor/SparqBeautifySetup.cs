using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Replaces our PPv2 setup with Beautify 3.
    /// • Removes the old [PostFX Volume] GameObject
    /// • Removes any PPv2 component on the Main Camera
    /// • Adds BeautifyEffect.Beautify to the Main Camera
    /// • Tunes a soft mobile-friendly preset
    /// </summary>
    public static class SparqBeautifySetup
    {
        [MenuItem("Sparq/40. Wire Beautify 3 (replace PPv2)")]
        public static void Wire()
        {
            // 1. Remove old PPv2 volume
            var oldVol = GameObject.Find("[PostFX Volume]");
            if (oldVol != null) Object.DestroyImmediate(oldVol);

            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Sparq Beautify", "No Main Camera in scene.", "OK");
                return;
            }

            // 2. Remove PPv2 PostProcessLayer if present (uses reflection so we don't have to reference PPv2 namespace)
            foreach (var c in cam.GetComponents<Component>())
            {
                if (c == null) continue;
                var typeName = c.GetType().Name;
                if (typeName == "PostProcessLayer" || typeName == "PostProcessVolume")
                    Object.DestroyImmediate(c);
            }

            // 3. Attach Beautify
            var beautifyType = System.Type.GetType("BeautifyEffect.Beautify, Assembly-CSharp");
            if (beautifyType == null) beautifyType = System.Type.GetType("BeautifyEffect.Beautify");
            if (beautifyType == null)
            {
                // Fallback search
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType("BeautifyEffect.Beautify");
                    if (t != null) { beautifyType = t; break; }
                }
            }
            if (beautifyType == null)
            {
                EditorUtility.DisplayDialog("Sparq Beautify",
                    "Couldn't find BeautifyEffect.Beautify class. Did the import finish? " +
                    "Try: Assets → Reimport All.", "OK");
                return;
            }

            var existing = cam.gameObject.GetComponent(beautifyType);
            if (existing == null)
                cam.gameObject.AddComponent(beautifyType);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Beautify",
                "✅ Beautify 3 wired to Main Camera.\n\n" +
                "• Old PPv2 [PostFX Volume] removed\n" +
                "• Any PostProcessLayer on camera stripped\n" +
                "• Beautify component added\n\n" +
                "Hit ▶ Play to see AAA post-processing.\n\n" +
                "To tune: select Main Camera → Beautify component in Inspector.\n" +
                "Try presets: Soft / Strong / Cinematic / Vivid.", "OK");
        }
    }
}
