using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>Menu 202: Test the Remind popup.</summary>
    public static class SparqTestRemind
    {
        [MenuItem("Sparq/202. Open Remind Panel")]
        public static void OpenRemind()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.RemindPanel.Show();
        }
    }
}
