using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqRivalSystem
    {
        [MenuItem("Sparq/35. Install Rival Manager + reset HP")]
        public static void Install()
        {
            // Drop a [RivalManager] into the scene
            var old = GameObject.Find("[RivalManager]");
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject("[RivalManager]");
            go.AddComponent<Sparq.Systems.RivalManager>();

            // Reset current rival to index 0 (Slym) and seed fresh HP
            var data = Sparq.Core.SaveService.Data;
            if (data == null) Sparq.Core.SaveService.Load();
            data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.currentRivalIndex = 0;
                var r = Sparq.Systems.RivalRoster.ROSTER[0];
                data.fitchXP = data.totalXP + r.baseHpXP;
                Sparq.Core.SaveService.Save();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Rival System",
                "✅ RivalManager installed + HP reset.\n\n" +
                "• Current rival: Slym (Goo Trickster)\n" +
                "• HP: 72 (taps to defeat)\n" +
                "• On defeat: +40 XP, +150 coins, advance to Volt (180 HP)\n\n" +
                "5 rivals in roster — cycle continues indefinitely.\n\n" +
                "Hit ▶ Play and tap Karu to damage the rival.", "OK");
        }
    }
}
