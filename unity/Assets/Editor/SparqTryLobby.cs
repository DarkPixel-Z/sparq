using UnityEditor;
using UnityEngine;

namespace Sparq.EditorTools
{
    /// <summary>
    /// Quick preview menu — instantiates the Layer Lab GUI Pro-FantasyHero
    /// Lobby.prefab in the running scene so we can see the new home page
    /// before fully replacing the old one.
    /// </summary>
    public static class SparqTryLobby
    {
        [MenuItem("Sparq/Try Layer Lab Lobby (preview)", priority = 5)]
        public static void TryLobby()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Try Layer Lab Lobby",
                    "The lobby spawns into the active Canvas at runtime.\n\nEnter Play Mode and try again.",
                    "OK");
                return;
            }
            Sparq.UI.HomeLobbyPanel.Show();
        }

        [MenuItem("Sparq/Hide Layer Lab Lobby", priority = 6)]
        public static void HideLobby()
        {
            if (!Application.isPlaying) return;
            Sparq.UI.HomeLobbyPanel.Hide();
        }
    }
}
