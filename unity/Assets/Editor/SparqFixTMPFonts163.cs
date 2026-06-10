using UnityEngine;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 163: Auto-assign TMP default font to any TextMeshProUGUI in
    /// SocialPanel (or anywhere) that has a missing font/material.
    /// </summary>
    public static class SparqFixTMPFonts163
    {
        [MenuItem("Sparq/163. Fix missing TMP fonts (UnassignedReferenceException)")]
        public static void Apply()
        {
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "TMP_Settings.defaultFontAsset is null. Open Window → TextMeshPro → Manage Project Files first.", "OK");
                return;
            }

            int fixedCount = 0;
            foreach (var tmp in Object.FindObjectsByType<TMP_Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp == null) continue;
                bool needsFix = false;
                if (tmp.font == null) needsFix = true;
                if (!needsFix && tmp.fontSharedMaterial == null) needsFix = true;
                if (!needsFix) continue;

                tmp.font = defaultFont;
                if (tmp.font != null && tmp.font.material != null)
                    tmp.fontSharedMaterial = tmp.font.material;

                EditorUtility.SetDirty(tmp);
                fixedCount++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Auto-assigned TMP default font to {fixedCount} text element(s).\n\nHit ▶ Play.", "OK");
        }
    }
}
