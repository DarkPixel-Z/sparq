using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>Menu 196: Test the full-screen Quests popup.</summary>
    public static class SparqTestQuests
    {
        [MenuItem("Sparq/196. Open Quests Panel")]
        public static void OpenQuests()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.QuestsPanel.Show();
        }
    }
}
