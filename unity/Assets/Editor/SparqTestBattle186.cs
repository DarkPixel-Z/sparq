using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 186: Test the battle scene by force-launching it. Useful if
    /// today's daily trial isn't a combat one.
    /// </summary>
    public static class SparqTestBattle186
    {
        [MenuItem("Sparq/186. Test Battle — Forest Goblin")]
        public static void Goblin()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Hit ▶ Play first, then run this menu while playing.", "OK");
                return;
            }
            Sparq.Systems.BattleScene.Start("Forest Patrol");
        }

        [MenuItem("Sparq/186a. Test Battle — Shadow Wolf (boss)")]
        public static void Wolf()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Hit ▶ Play first, then run this menu while playing.", "OK");
                return;
            }
            Sparq.Systems.BattleScene.Start("Hunt the Shadow Wolf");
        }

        [MenuItem("Sparq/186b. Test Battle — Mind Phantom")]
        public static void Phantom()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Hit ▶ Play first, then run this menu while playing.", "OK");
                return;
            }
            Sparq.Systems.BattleScene.Start("Mind Phantom");
        }

        [MenuItem("Sparq/186c. Test Battle — Stone Brute")]
        public static void Brute()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Hit ▶ Play first, then run this menu while playing.", "OK");
                return;
            }
            Sparq.Systems.BattleScene.Start("Stone Brute");
        }
    }
}
