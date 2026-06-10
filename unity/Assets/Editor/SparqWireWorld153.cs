using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 153: Wires the WORLD top button to open WorldPanel
    /// (Friends / Feed / Top Heroes tabs).
    /// </summary>
    public static class SparqWireWorld153
    {
        [MenuItem("Sparq/153. Wire WORLD button → community panel")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK"); return; }

            Transform world = null;
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var c = bar.transform.GetChild(i);
                string n = c.name.ToLower();
                if (n.Contains("world")) { world = c; break; }
            }
            if (world == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "WorldBtn not found. Run #143 first to add the WORLD button.", "OK");
                return;
            }

            var btn = world.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => Sparq.UI.WorldPanel.Show());

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ WORLD button wired to community panel.\n\n" +
                "Tap WORLD → fullscreen panel with 3 tabs:\n" +
                "  • Friends — 5 mock heroes w/ avatars + status + Wave button\n" +
                "  • Feed — 5 recent activity posts\n" +
                "  • Top Heroes — leaderboard (Karu = #3, highlighted)\n\n" +
                "Tap dim outside or X to close.\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
