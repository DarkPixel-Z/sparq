using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>Menu 199: Test the Profile popup.</summary>
    public static class SparqTestProfile
    {
        [MenuItem("Sparq/199. Open Profile Panel")]
        public static void OpenProfile()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.ProfilePanel.Show();
        }
    }
}
