using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqGrandLayout
    {
        [MenuItem("Sparq/104. Grand layout (HUD top, buttons over quests, hero left, big Mochi)")]
        public static void Apply()
        {
            // 1. HUD → ABSOLUTE TOP RIGHT (no margin)
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(1f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(1f, 1f);
                hrt.anchoredPosition = new Vector2(-4f, -2f);   // very top
                hrt.sizeDelta = new Vector2(420, 110);
            }

            // 2. Top button row → above quest box on the RIGHT side
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(1f, 1f);
                brt.anchoredPosition = new Vector2(-14f, -120f); // below HUD, above quest
                brt.sizeDelta = new Vector2(360, 36);

                // Tighter buttons
                foreach (Transform t in bar.transform)
                {
                    var rt2 = t.GetComponent<RectTransform>();
                    if (rt2 != null) rt2.sizeDelta = new Vector2(66, 32);
                    var le = t.GetComponent<LayoutElement>();
                    if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 66;
                    le.preferredHeight = 32;
                    le.flexibleWidth = 0;
                    foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tmp.fontSize = 10;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                        tmp.overflowMode = TextOverflowModes.Overflow;
                    }
                }
            }

            // 3. Quest box → RIGHT, below the buttons
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchorMin = new Vector2(1f, 1f);
                qrt.anchorMax = new Vector2(1f, 1f);
                qrt.pivot = new Vector2(1f, 1f);
                qrt.anchoredPosition = new Vector2(-14f, -170f); // below buttons
                qrt.sizeDelta = new Vector2(360, 280);
            }

            // 4. Logo → top-left, button-shape (yellow frame around dark plate around image)
            RebuildLogoButton();

            // 5. Hero LEFT + Mochi BIG
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.position = new Vector3(-3.0f, -0.7f, 0f);  // pulled further left
                karu.transform.localScale = Vector3.one * 0.65f;
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 50;
            }
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 0.55f;  // bigger (was 0.32)
                mochi.transform.position = new Vector3(-1.0f, -1.2f, 0f);
                var sr = mochi.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sortingOrder = 49; var c = sr.color; c.a = 1f; sr.color = c; }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Grand layout applied:\n\n" +
                "• HUD: very top right (-4, -2)\n" +
                "• Top buttons: above quests on right (-14, -120), 66×32\n" +
                "• Quest box: right side, below buttons\n" +
                "• Logo: button-shape (yellow frame + dark plate + image)\n" +
                "• Karu: x=-3.0 (further left)\n" +
                "• Mochi: scale 0.55 (bigger sidekick)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void RebuildLogoButton()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            var titleGO = new GameObject("GameTitle", typeof(RectTransform), typeof(Image));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -8f);
            rt.sizeDelta = new Vector2(280, 110);

            // Yellow button frame
            var frame = titleGO.GetComponent<Image>();
            frame.color = new Color(1f, 0.85f, 0.30f, 0.95f);
            frame.raycastTarget = false;

            // Inner dark plate
            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(titleGO.transform, false);
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(4, 4); prt.offsetMax = new Vector2(-4, -4);
            plate.GetComponent<Image>().color = new Color(0.20f, 0.05f, 0.30f, 0.95f);
            plate.GetComponent<Image>().raycastTarget = false;

            // Inner logo image
            string logoPath = "Assets/Art/Sparq/sparq-logo.png";
            var imp = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
            if (sprite != null)
            {
                var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
                logo.transform.SetParent(plate.transform, false);
                var lrt = logo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(2, 2); lrt.offsetMax = new Vector2(-2, -2);
                var img = logo.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }
    }
}
