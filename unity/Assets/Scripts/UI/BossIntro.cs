using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Cinematic boss intro panel — shown before chapter-boss fights.
    /// Uses Layer Lab GUI Pro-FantasyRPG PlayBoss.prefab if available,
    /// otherwise falls back to a procedural panel.
    /// </summary>
    public static class BossIntro
    {
        public static void Show(string bossName, System.Action onComplete)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) { onComplete?.Invoke(); return; }

            var root = new GameObject("BossIntro",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14800;
            var cs = root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Black flash
            var flash = MakeImg(root.transform, "Flash", new Color(0, 0, 0, 1f));
            var frt = flash.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            // Try the prefab first
            #if UNITY_EDITOR
            var pfx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_DemoScene_Panels/PlayBoss.prefab");
            #else
            GameObject pfx = null;
            #endif

            if (pfx != null)
            {
                // Object.Instantiate works in both editor + builds (no PrefabUtility needed)
                var inst = Object.Instantiate(pfx, root.transform);
                if (inst != null)
                {
                    var instRT = inst.GetComponent<RectTransform>();
                    if (instRT != null)
                    {
                        instRT.anchorMin = new Vector2(0.5f, 0.5f);
                        instRT.anchorMax = new Vector2(0.5f, 0.5f);
                        instRT.pivot = new Vector2(0.5f, 0.5f);
                        instRT.anchoredPosition = Vector2.zero;
                        instRT.localScale = Vector3.one;
                    }
                    // Try to set the boss name in any TMP_Text labelled "Name"/"Title"
                    foreach (var tm in inst.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (tm == null) continue;
                        string n = tm.gameObject.name.ToLower();
                        if (n.Contains("name") || n.Contains("title") || n.Contains("boss"))
                            tm.text = bossName.ToUpper();
                    }
                }
            }
            else
            {
                BuildFallback(root.transform, bossName);
            }

            // Auto-dismiss after 2.4s
            var runner = root.AddComponent<RunnerStub>();
            runner.StartCoroutine(Dismiss(root, onComplete));
        }

        private static IEnumerator Dismiss(GameObject root, System.Action cb)
        {
            yield return new WaitForSeconds(2.4f);
            Object.Destroy(root);
            cb?.Invoke();
        }

        private static void BuildFallback(Transform parent, string bossName)
        {
            // Procedural fallback if prefab missing
            var bg = MakeImg(parent, "Bg", new Color(0.10f, 0.04f, 0.10f, 0.95f));
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            var label = new GameObject("Title", typeof(RectTransform));
            label.transform.SetParent(parent, false);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(1, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(0, 200);
            var tm = label.AddComponent<TextMeshProUGUI>();
            tm.text = $"⚔  BOSS  ⚔\n<size=72>{bossName.ToUpper()}</size>";
            tm.fontSize = 56;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(1f, 0.78f, 0.22f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.outlineWidth = 0.30f;
            tm.outlineColor = new Color(0.4f, 0, 0, 1f);
        }

        private static GameObject MakeImg(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private class RunnerStub : MonoBehaviour {}
    }
}
