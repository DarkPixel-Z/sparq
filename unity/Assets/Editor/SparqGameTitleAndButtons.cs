using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Adds the SPARQ game title at top center + replaces MAP/SHOP/BAG colored squares
    /// with painted Super Casual button prefabs.
    /// </summary>
    public static class SparqGameTitleAndButtons
    {
        private const string BTN_GREEN  = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/Button01_s_BtnText_Green.prefab";
        private const string BTN_YELLOW = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/Button01_s_BtnText_Yellow.prefab";
        private const string BTN_SKY    = "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_Component_Buttons/Button01_s_BtnText_Sky.prefab";

        [MenuItem("Sparq/45. SPARQ title + painted buttons")]
        public static void Apply()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            BuildTitle(canvas.transform);
            ReplaceNavButtons(canvas.transform);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Polish",
                "✅ Title + buttons upgraded.\n\n" +
                "• 'SPARQ' logo top-center with electric purple+yellow style\n" +
                "• MAP / SHOP / BAG → painted Super Casual buttons\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildTitle(Transform canvasT)
        {
            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            // Top-left, clean, no plate — match the WebView style
            var titleGO = new GameObject("GameTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvasT, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(160f, -22f);
            rt.sizeDelta = new Vector2(280, 80);

            // Word "Sparq" — yellow, bold, slight italic for energy
            var wordGO = new GameObject("Word", typeof(RectTransform));
            wordGO.transform.SetParent(titleGO.transform, false);
            var wrt = wordGO.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0, 0); wrt.anchorMax = new Vector2(0, 1);
            wrt.pivot = new Vector2(0, 0.5f);
            wrt.anchoredPosition = new Vector2(0, 0);
            wrt.sizeDelta = new Vector2(190, 0);
            var wtm = wordGO.AddComponent<TextMeshProUGUI>();
            wtm.text = "Sparq";
            wtm.fontSize = 48;
            wtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            wtm.color = new Color(1f, 0.85f, 0.25f);  // warm yellow
            wtm.alignment = TextAlignmentOptions.Left;
            wtm.outlineWidth = 0.18f;
            wtm.outlineColor = new Color(0.6f, 0.25f, 0.0f, 1f);
            wtm.raycastTarget = false;

            // ⚡ Lightning bolt next to it (separate so we can spark from it)
            var boltGO = new GameObject("Bolt", typeof(RectTransform));
            boltGO.transform.SetParent(titleGO.transform, false);
            var brt = boltGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(195, 4);
            brt.sizeDelta = new Vector2(50, 60);
            var btm = boltGO.AddComponent<TextMeshProUGUI>();
            btm.text = "⚡";
            btm.fontSize = 50;
            btm.color = new Color(1f, 0.92f, 0.3f);
            btm.alignment = TextAlignmentOptions.Center;
            btm.outlineWidth = 0.2f;
            btm.outlineColor = new Color(0.7f, 0.4f, 0.0f, 1f);
            btm.raycastTarget = false;
            boltGO.AddComponent<BoltWiggle>();

            // Sparks emitter on the bolt
            boltGO.AddComponent<SparkEmitter>();
        }

        // Bolt wiggles slightly to feel alive
        private class BoltWiggle : MonoBehaviour
        {
            float t;
            void Awake() { t = Random.value * 5f; }
            void Update()
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3.5f) * 8f);
                float s = 1f + Mathf.Sin(t * 2.4f) * 0.04f;
                transform.localScale = new Vector3(s, s, 1f);
            }
        }

        // Spawns little yellow sparks every ~0.3s that fly off the bolt
        private class SparkEmitter : MonoBehaviour
        {
            float _next;
            void Update()
            {
                if (Time.time < _next) return;
                _next = Time.time + Random.Range(0.18f, 0.45f);
                SpawnSpark();
            }
            void SpawnSpark()
            {
                var sp = new GameObject("Spark", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                sp.transform.SetParent(transform.parent, false);
                var rt = sp.GetComponent<RectTransform>();
                var origin = ((RectTransform)transform).anchoredPosition;
                rt.anchorMin = ((RectTransform)transform).anchorMin;
                rt.anchorMax = ((RectTransform)transform).anchorMax;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = origin + new Vector2(Random.Range(-10f, 10f), Random.Range(-15f, 15f));
                rt.sizeDelta = new Vector2(Random.Range(4f, 10f), Random.Range(4f, 10f));
                var img = sp.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(1f, Random.Range(0.85f, 1f), Random.Range(0.2f, 0.6f), 1f);
                img.raycastTarget = false;
                var anim = sp.AddComponent<SparkFly>();
                anim.velocity = new Vector2(Random.Range(-30f, 60f), Random.Range(-20f, 80f));
                anim.life    = Random.Range(0.5f, 0.9f);
            }
        }

        private class SparkFly : MonoBehaviour
        {
            public Vector2 velocity;
            public float life;
            float t;
            UnityEngine.UI.Image img;
            RectTransform rt;
            void Awake() { img = GetComponent<UnityEngine.UI.Image>(); rt = (RectTransform)transform; }
            void Update()
            {
                t += Time.deltaTime;
                if (rt != null) rt.anchoredPosition += velocity * Time.deltaTime;
                velocity *= 0.96f;
                if (img != null) { var c = img.color; c.a = 1f - (t / life); img.color = c; }
                if (t >= life) Destroy(gameObject);
            }
        }

        private static void ReplaceNavButtons(Transform canvasT)
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Save controller, then wipe children
            var ctrl = bar.GetComponent<Sparq.UI.HomeNavBar>();
            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // Build replacements: MAP (green), SHOP (yellow), BAG (sky)
            BuildPaintedBtn(bar.transform, BTN_GREEN,  "MapBtn",  "MAP");
            BuildPaintedBtn(bar.transform, BTN_YELLOW, "ShopBtn", "SHOP");
            BuildPaintedBtn(bar.transform, BTN_SKY,    "BagBtn",  "BAG");

            // Re-attach controller (HomeNavBar.Start re-wires onClick at runtime)
            if (ctrl == null) bar.AddComponent<Sparq.UI.HomeNavBar>();

            // Resize bar a touch wider for the prettier buttons
            var brt = bar.GetComponent<RectTransform>();
            if (brt != null)
            {
                brt.sizeDelta = new Vector2(135, 280);
                brt.anchoredPosition = new Vector2(14f, -180f);
            }

            var vlg = bar.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.spacing = 14;
        }

        private static void BuildPaintedBtn(Transform parent, string prefabPath, string objName, string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = objName;

            // Force a sensible size for the side rail
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(120, 70);
            }
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 70;

            // Override the prefab's text label
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.text = label;
                tmp.fontSize = 24;
                tmp.fontStyle = FontStyles.Bold;
            }
        }
    }
}
