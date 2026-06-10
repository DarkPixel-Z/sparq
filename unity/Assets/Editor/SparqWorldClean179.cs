using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 179: Make WORLD reliably open SocialPanel, and stop the Daily
    /// Bonus calendar from auto-firing every play (it was popping over WORLD).
    /// </summary>
    public static class SparqWorldClean179
    {
        [MenuItem("Sparq/179. WORLD = SocialPanel only (suppress daily-bonus pop)")]
        public static void Apply()
        {
            // 1. Mark today's daily bonus as claimed so the popup stops auto-firing
            //    (we set lastDailyBonusDate to today via PlayerPrefs / SaveService)
            try
            {
                var dataField = typeof(Sparq.Core.SaveService)
                    .GetProperty("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var data = dataField?.GetValue(null);
                if (data != null)
                {
                    var fld = data.GetType().GetField("lastDailyBonusDate");
                    if (fld != null)
                    {
                        fld.SetValue(data, System.DateTime.Today.ToString("yyyy-MM-dd"));
                        Sparq.Core.SaveService.Save();
                    }
                }
            } catch {}

            // 2. Find SocialPanel (active or inactive)
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "SocialPanel missing. Run #157 first to build it.", "OK");
                return;
            }

            // Bump SocialPanel sortingOrder so it sits ABOVE the daily-bonus popup
            var c = social.GetComponent<Canvas>();
            if (c != null) { c.overrideSorting = true; c.sortingOrder = 14000; }

            // 3. Hard-rewire WORLD button: nuke all listeners, attach fresh PanelToggle → SocialPanel
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK"); return; }

            Transform world = null;
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var t = bar.transform.GetChild(i);
                if (t.name.ToLower().Contains("world")) { world = t; break; }
            }
            if (world == null) { EditorUtility.DisplayDialog("Sparq", "WorldBtn missing.", "OK"); return; }

            foreach (var pt in world.GetComponents<PanelToggle>()) Object.DestroyImmediate(pt);
            var btn = world.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                var so = new SerializedObject(btn);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                if (calls != null) { calls.arraySize = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            var toggle = world.gameObject.AddComponent<PanelToggle>();
            var tso = new SerializedObject(toggle);
            tso.FindProperty("target").objectReferenceValue = social;
            tso.FindProperty("setActiveOnClick").boolValue = true;
            tso.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Fixed:\n" +
                "• Daily bonus marked claimed for today → no more auto-pop on play\n" +
                "• WORLD button hard-rewired → opens SocialPanel only\n" +
                "• SocialPanel sortingOrder bumped to 14000 (above daily bonus)\n\n" +
                "Hit ▶ Play and tap WORLD.", "OK");
        }
    }
}
