using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqCharacterPicker
    {
        [MenuItem("Sparq/83. Show Character Select (5 starters)")]
        public static void Show()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Character Select shows on Play.\n\n" +
                    "To trigger it on first launch, use Sparq → 83a (clear activePet).\n" +
                    "Then hit ▶ Play.", "OK");
                return;
            }
            Sparq.UI.CharacterSelect.Show();
        }

        [MenuItem("Sparq/83a. Reset character (force pick on next Play)")]
        public static void Reset()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) Sparq.Core.SaveService.Load();
            data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.activePet = "";
                data.petName = "";
                Sparq.Core.SaveService.Save();
                EditorUtility.DisplayDialog("Sparq",
                    "✅ Character cleared.\n\n" +
                    "On next ▶ Play, the Character Select screen will appear with 5 fantasy starters:\n" +
                    "• Karu the Bear\n" +
                    "• Mochi the Cat\n" +
                    "• Hoot the Owl\n" +
                    "• Azure the Wisp\n" +
                    "• Batty the Bat", "OK");
            }
        }
    }
}
