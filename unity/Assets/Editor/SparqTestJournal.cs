using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>Menu 198: Test the Journal popup.</summary>
    public static class SparqTestJournal
    {
        [MenuItem("Sparq/198. Open Journal Panel")]
        public static void OpenJournal()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.JournalPanel.Show();
        }
    }
}
