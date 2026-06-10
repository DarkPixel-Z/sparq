using UnityEditor;
using UnityEngine;

namespace Sparq.EditorTools
{
    /// <summary>
    /// Editor menu items for testing the hero picker and forcing chibi swaps
    /// (Una in particular) without round-tripping through Play Mode.
    /// </summary>
    public static class SparqHeroPicker
    {
        // ───────── Hero picker ─────────
        [MenuItem("Sparq/Open Hero Picker", priority = 100)]
        public static void OpenHeroPicker()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Open Hero Picker",
                    "The picker spawns into the active Canvas, so it only works while the game is running.\n\nEnter Play Mode and try again.",
                    "OK");
                return;
            }
            Sparq.UI.HeroSelectPanel.Show();
        }

        // Disable while not in Play Mode (greys out the menu item)
        [MenuItem("Sparq/Open Hero Picker", validate = true)]
        public static bool ValidateOpenHeroPicker() => Application.isPlaying;

        // ───────── Reset hero pick (handy for testing) ─────────
        [MenuItem("Sparq/Reset Hero Pick", priority = 101)]
        public static void ResetHeroPick()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null)
            {
                Debug.LogWarning("[SparqHeroPicker] SaveService.Data is null — enter Play Mode first.");
                return;
            }
            data.heroClass = "";
            Sparq.Core.SaveService.Save();
            Debug.Log("[SparqHeroPicker] heroClass cleared. Resolver will fall back to weapon-based detection.");
        }

        [MenuItem("Sparq/Reset Hero Pick", validate = true)]
        public static bool ValidateResetHeroPick() => Application.isPlaying;

        // ───────── Force-refresh home chibis (Una + Karu) ─────────
        // Useful after editing UNA_SPRITE / class paths in HomeChibiUpgrade
        // so you don't have to leave & re-enter Play Mode.
        [MenuItem("Sparq/Refresh Home Chibis", priority = 110)]
        public static void RefreshHomeChibis()
        {
            Sparq.UI.HomeChibiUpgrade.ForceRun();                  // re-runs Una swap
            Sparq.UI.HomeChibiUpgrade.RefreshKaruFromHeroClass();  // re-runs Karu swap
            Debug.Log("[SparqHeroPicker] Forced Home chibi refresh (Una + Karu).");
        }

        [MenuItem("Sparq/Refresh Home Chibis", validate = true)]
        public static bool ValidateRefreshHomeChibis() => Application.isPlaying;

        // ───────── Test new turn-based battle ─────────
        [MenuItem("Sparq/Test Turn-Based Battle/Forest Goblin", priority = 200)]
        public static void TestTBB_Goblin()  => Sparq.Systems.TurnBasedBattle.Start("Forest Goblin");

        [MenuItem("Sparq/Test Turn-Based Battle/Shadow Wolf", priority = 201)]
        public static void TestTBB_Wolf()    => Sparq.Systems.TurnBasedBattle.Start("Shadow Wolf");

        [MenuItem("Sparq/Test Turn-Based Battle/Mind Phantom", priority = 202)]
        public static void TestTBB_Phantom() => Sparq.Systems.TurnBasedBattle.Start("Mind Phantom");

        [MenuItem("Sparq/Test Turn-Based Battle/Stone Brute", priority = 203)]
        public static void TestTBB_Brute()   => Sparq.Systems.TurnBasedBattle.Start("Stone Brute");

        [MenuItem("Sparq/Test Turn-Based Battle/Forest Goblin", validate = true)]
        public static bool ValidateTBB() => Application.isPlaying;

        // ───────── Test SQUAD battle (3v3 to 3v5) ─────────
        [MenuItem("Sparq/Test Squad Battle/Forest", priority = 300)]
        public static void TestSquad_Forest() => Sparq.Systems.SquadBattle.Start("Forest of Trials", "forest");

        [MenuItem("Sparq/Test Squad Battle/Haunted Ruins", priority = 301)]
        public static void TestSquad_Haunted() => Sparq.Systems.SquadBattle.Start("Haunted Ruins", "haunted");

        [MenuItem("Sparq/Test Squad Battle/Forest", validate = true)]
        public static bool ValidateSquad() => Application.isPlaying;

        // ───────── Test World Explore (Phase 1 walker) ─────────
        [MenuItem("Sparq/Test World Explore/Forest", priority = 400)]
        public static void TestWorld_Forest()  => Sparq.UI.WorldExplorePanel.Show("forest");

        [MenuItem("Sparq/Test World Explore/Haunted (map 02)", priority = 401)]
        public static void TestWorld_Haunted() => Sparq.UI.WorldExplorePanel.Show("haunted");

        [MenuItem("Sparq/Test World Explore/Forest", validate = true)]
        public static bool ValidateWorld() => Application.isPlaying;
    }
}
