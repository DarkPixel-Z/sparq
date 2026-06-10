using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 161: Make SocialPanel dim fully opaque + tighten prefab scale.
    /// </summary>
    public static class SparqFixSocialDim161
    {
        [MenuItem("Sparq/161. SocialPanel: opaque dim + tighter scale")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Solid black dim
            var dim = social.transform.Find("Dim");
            if (dim != null)
            {
                var img = dim.GetComponent<Image>();
                if (img != null) img.color = new Color(0.04f, 0.03f, 0.08f, 1f); // near-black, fully opaque
                var rt = dim.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                }
            }

            // 2. Make prefab tabs smaller AND position them centered inside content
            var content = social.transform.Find("Content");
            if (content != null)
            {
                int n = 0;
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (!c.name.EndsWith("_Tab")) continue;
                    var rt = c.GetComponent<RectTransform>();
                    if (rt == null) continue;
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot     = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = new Vector3(0.55f, 0.55f, 1f);
                    n++;
                }
            }

            // 3. Bring SocialPanel canvas to absolute top
            var canvas = social.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 12000;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Fixed:\n• Dim → fully opaque dark\n• Prefab tabs → 0.55×, centered\n• SocialPanel sortingOrder → 12000\n\nHit ▶ Play.", "OK");
        }
    }
}
