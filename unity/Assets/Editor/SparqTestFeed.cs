using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>Menu 203: Test the Feed popup.</summary>
    public static class SparqTestFeed
    {
        [MenuItem("Sparq/203. Open Feed Panel")]
        public static void OpenFeed()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.FeedPanel.Show();
        }
    }
}
