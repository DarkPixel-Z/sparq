using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Wires Shop + Bag panels and adds two icon buttons (left edge) to open them.
    /// </summary>
    public static class SparqShopBagSetup
    {
        private const string SHOP_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/Shop.prefab";
        private const string BAG_PATH  = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/Bag.prefab";

        [MenuItem("Sparq/32. Wire Shop + Bag panels")]
        public static void Wire()
        {
            // 1. Wire prefab references on PopupManager
            var pmGO = GameObject.Find("[PopupManager]");
            if (pmGO == null)
            {
                EditorUtility.DisplayDialog("Sparq Shop",
                    "[PopupManager] not in scene. Run Sparq → 18 first.", "OK");
                return;
            }
            var pm = pmGO.GetComponent<Sparq.UI.PopupManager>();
            var shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHOP_PATH);
            var bagPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(BAG_PATH);
            if (shopPrefab == null || bagPrefab == null)
            {
                EditorUtility.DisplayDialog("Sparq Shop",
                    $"Prefab(s) missing.\nShop: {(shopPrefab != null ? "ok" : "MISSING")}\nBag: {(bagPrefab != null ? "ok" : "MISSING")}",
                    "OK");
                return;
            }
            var so = new SerializedObject(pm);
            so.FindProperty("shopPrefab").objectReferenceValue = shopPrefab;
            so.FindProperty("bagPrefab").objectReferenceValue  = bagPrefab;
            so.ApplyModifiedProperties();

            // 2. Add Shop + Bag buttons to home screen (left edge, vertically stacked)
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Remove old buttons if re-running
            var oldBar = GameObject.Find("HomeNavButtons");
            if (oldBar != null) Object.DestroyImmediate(oldBar);

            var bar = new GameObject("HomeNavButtons", typeof(RectTransform));
            bar.transform.SetParent(canvas.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot     = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(14f, 120f);
            brt.sizeDelta = new Vector2(72, 220);

            var vlg = bar.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Shop button
            BuildIconButton(bar.transform, "ShopBtn", "SHOP", new Color(1f, 0.78f, 0.2f, 0.95f),
                () => Sparq.UI.PopupManager.Instance?.OpenShop());

            // Bag button
            BuildIconButton(bar.transform, "BagBtn", "BAG", new Color(0.5f, 0.85f, 0.95f, 0.95f),
                () => Sparq.UI.PopupManager.Instance?.OpenBag());

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Shop + Bag",
                "✅ Shop + Bag wired.\n\n" +
                "• Shop button (yellow) on left edge\n" +
                "• Bag button (cyan) below it\n" +
                "• Tap → full-screen panel opens\n" +
                "• Click outside or press X to close\n\n" +
                "Hit ▶ Play and tap one!", "OK");
        }

        private static void BuildIconButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 64;
            le.preferredHeight = 64;

            var img = go.GetComponent<Image>();
            img.color = color;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            // Label inside
            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(go.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 16;
            tm.color = new Color(0.1f, 0.05f, 0.2f);
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
        }
    }
}
