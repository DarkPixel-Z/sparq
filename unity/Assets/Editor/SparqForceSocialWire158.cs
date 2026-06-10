using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 158: Hard-reset the WORLD button so it uses ONLY PanelToggle
    /// pointing at SocialPanel. Strips both runtime + persistent listeners.
    /// </summary>
    public static class SparqForceSocialWire158
    {
        [MenuItem("Sparq/158. Force-rewire WORLD → SocialPanel (clears stale listeners)")]
        public static void Apply()
        {
            var social = GameObject.Find("SocialPanel");
            if (social == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "SocialPanel not in scene. Run #157 first to build it.", "OK");
                return;
            }

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            Transform world = null;
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var c = bar.transform.GetChild(i);
                if (c.name.ToLower().Contains("world")) { world = c; break; }
            }
            if (world == null)
            {
                EditorUtility.DisplayDialog("Sparq", "WorldBtn not found.", "OK");
                return;
            }

            // 1. Remove ALL existing PanelToggle components (in case stale)
            foreach (var t in world.GetComponents<PanelToggle>())
                Object.DestroyImmediate(t);

            // 2. Clear persistent onClick listeners via SerializedObject
            var btn = world.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                var so = new SerializedObject(btn);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                if (calls != null)
                {
                    calls.arraySize = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 3. Attach a fresh PanelToggle
            var toggle = world.gameObject.AddComponent<PanelToggle>();
            var tso = new SerializedObject(toggle);
            tso.FindProperty("target").objectReferenceValue = social;
            tso.FindProperty("setActiveOnClick").boolValue = true;
            tso.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ WORLD button hard-rewired:\n\n" +
                "• Persistent onClick listeners cleared\n" +
                "• Old PanelToggle components removed\n" +
                "• Fresh PanelToggle → SocialPanel attached\n\n" +
                "Hit ▶ Play and tap WORLD.", "OK");
        }
    }
}
