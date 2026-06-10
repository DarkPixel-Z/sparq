using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 160: Scale up the prefab tabs inside SocialPanel so they fill the
    /// screen properly instead of looking small.
    /// </summary>
    public static class SparqScalePrefabTabs160
    {
        [MenuItem("Sparq/160. Scale prefab tabs to fit screen")]
        public static void Apply()
        {
            var social = GameObject.Find("SocialPanel");
            if (social == null)
            {
                // Try inactive
                foreach (var go in Object.FindObjectsByType<GameObject>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (go != null && go.name == "SocialPanel") { social = go; break; }
                }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var content = social.transform.Find("Content");
            if (content == null) { EditorUtility.DisplayDialog("Sparq", "Content area not found.", "OK"); return; }

            int n = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                if (!c.name.EndsWith("_Tab")) continue;
                var rt = c.GetComponent<RectTransform>();
                if (rt == null) continue;
                rt.localScale = new Vector3(0.65f, 0.65f, 1f);
                rt.anchoredPosition = Vector2.zero;
                n++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Scaled {n} prefab tab(s) to 1.5×.\n\n" +
                "If still too small / too big, run again — I'll change the factor.\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
