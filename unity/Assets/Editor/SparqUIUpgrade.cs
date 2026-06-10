using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace Sparq.Editor
{
    /// <summary>
    /// Upgrade the Home scene UI to GUI Pro - Fantasy Hero prefabs.
    /// Swaps the basic XP bar for a proper fantasy slider.
    /// </summary>
    public static class SparqUIUpgrade
    {
        private const string SLIDER_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Tapered_01_Purple.prefab";
        private const string FRAME_PATH  = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Frames/";

        [MenuItem("Sparq/11. Upgrade UI to Fantasy Hero")]
        public static void UpgradeUI()
        {
            // Find the Canvas
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Sparq UI", "No Canvas in scene. Run Sparq → 2 (Build Home Scene) first.", "OK");
                return;
            }

            // Load the purple tapered slider prefab
            var sliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLIDER_PATH);
            if (sliderPrefab == null)
            {
                EditorUtility.DisplayDialog("Sparq UI",
                    $"Slider prefab not found:\n{SLIDER_PATH}\n\nIs GUI Pro Fantasy Hero imported?", "OK");
                return;
            }

            // Remove old XP bar container if it exists
            var oldBar = GameObject.Find("XPBarContainer");
            if (oldBar != null) Object.DestroyImmediate(oldBar);

            // Instantiate new slider prefab
            var sliderGO = (GameObject)PrefabUtility.InstantiatePrefab(sliderPrefab, canvas.transform);
            sliderGO.name = "FantasyXPBar";
            var rt = sliderGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 80);
                // Keep the prefab's own size
            }

            // Find the Slider component inside the prefab and its fill Image
            var slider = sliderGO.GetComponentInChildren<Slider>();
            Image fillImg = null;
            if (slider != null && slider.fillRect != null)
            {
                fillImg = slider.fillRect.GetComponent<Image>();
            }
            else
            {
                // Fallback — search by name
                var fill = sliderGO.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(i => i.name.ToLower().Contains("fill"));
                fillImg = fill;
            }

            // Find or create Level Text + XP Text above the slider
            var lvlText = CreateOrGetText(sliderGO.transform, "LevelText", "Lv.1", new Vector2(-140, 60), TextAlignmentOptions.Left);
            var xpText  = CreateOrGetText(sliderGO.transform, "XPText",    "0 / 100 XP", new Vector2(140, 60), TextAlignmentOptions.Right);

            // Attach XPBarDisplay and wire references
            var xpCtrl = sliderGO.GetComponent<Sparq.UI.XPBarDisplay>();
            if (xpCtrl == null) xpCtrl = sliderGO.AddComponent<Sparq.UI.XPBarDisplay>();
            var t = typeof(Sparq.UI.XPBarDisplay);
            t.GetField("fillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(xpCtrl, fillImg);
            t.GetField("levelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(xpCtrl, lvlText);
            t.GetField("xpText",    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(xpCtrl, xpText);

            // Hide the prefab's internal "99/99" default label so our Level/XP text is cleaner
            var defaultLabels = sliderGO.GetComponentsInChildren<TMP_Text>(true)
                .Where(tm => tm.name != "LevelText" && tm.name != "XPText")
                .ToArray();
            foreach (var dl in defaultLabels)
            {
                dl.gameObject.SetActive(false);
            }
            // Also hide legacy (non-TMP) Text components from the prefab
            var legacyTexts = sliderGO.GetComponentsInChildren<Text>(true);
            foreach (var lt in legacyTexts)
            {
                lt.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(sliderGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq UI] Upgraded XP bar to: {sliderPrefab.name}. Fill wired: {fillImg != null}.");
            EditorUtility.DisplayDialog("Sparq UI",
                "✅ XP bar upgraded to GUI Pro Fantasy Hero!\n\n" +
                "• Purple tapered slider prefab instantiated\n" +
                "• Level + XP text added around it\n" +
                "• XPBarDisplay wired to the new fill image\n\n" +
                "Hit ▶ Play to see the fancy version.", "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Build the rival card (Volt + HP bar) in top-right of screen
        // ──────────────────────────────────────────────────────────────────────
        private const string RIVAL_FRAME_PATH  = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Frames/BaseFrame_Border_Rectangle_H80_Gradient.prefab";
        private const string RIVAL_SLIDER_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Rectangle_01_Yellow.prefab";

        [MenuItem("Sparq/12. Add Rival Card (Volt)")]
        public static void AddRivalCard()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Sparq UI", "No Canvas in scene.", "OK");
                return;
            }

            // Load rival's sprite (fitch.svg = Volt)
            Sprite voltSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/fitch.svg");
            if (voltSprite == null)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sparq/fitch.svg");
                foreach (var o in all) if (o is Sprite s) { voltSprite = s; break; }
            }

            // Remove existing rival card if present
            var oldRival = GameObject.Find("RivalCard");
            if (oldRival != null) Object.DestroyImmediate(oldRival);

            // Try to instantiate the frame prefab as the card background
            var framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIVAL_FRAME_PATH);
            GameObject card;
            if (framePrefab != null)
            {
                card = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, canvas.transform);
                card.name = "RivalCard";
            }
            else
            {
                // Fallback: create a simple Image as background
                card = new GameObject("RivalCard");
                card.transform.SetParent(canvas.transform, false);
                var img = card.AddComponent<Image>();
                img.color = new Color(0.15f, 0.1f, 0.25f, 0.9f);
            }

            // Position: top-right
            var cardRT = card.GetComponent<RectTransform>();
            if (cardRT == null) cardRT = card.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(1f, 1f);
            cardRT.anchorMax = new Vector2(1f, 1f);
            cardRT.pivot     = new Vector2(1f, 1f);
            cardRT.anchoredPosition = new Vector2(-20, -40);
            cardRT.sizeDelta = new Vector2(360, 180);

            // Portrait (Volt sprite) on the left side of the card
            var portraitGO = new GameObject("VoltPortrait");
            portraitGO.transform.SetParent(card.transform, false);
            var portraitRT = portraitGO.AddComponent<RectTransform>();
            portraitRT.anchorMin = new Vector2(0f, 0.5f);
            portraitRT.anchorMax = new Vector2(0f, 0.5f);
            portraitRT.pivot     = new Vector2(0f, 0.5f);
            portraitRT.anchoredPosition = new Vector2(14, 0);
            portraitRT.sizeDelta = new Vector2(110, 110);
            var portraitImg = portraitGO.AddComponent<Image>();
            if (voltSprite != null)
            {
                portraitImg.sprite = voltSprite;
                portraitImg.preserveAspect = true;
            }
            portraitImg.raycastTarget = false;

            // Name + Title on the right
            var nameText  = CreateOrGetText(card.transform, "RivalName",  "Volt",
                new Vector2(80, 35), TextAlignmentOptions.Left);
            nameText.fontSize = 30;
            nameText.color = new Color(1f, 0.95f, 0.6f);
            var nameRT = nameText.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.4f, 1f); nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.pivot = new Vector2(0f, 1f);
            nameRT.offsetMin = new Vector2(0, -45); nameRT.offsetMax = new Vector2(-10, -8);

            var titleText = CreateOrGetText(card.transform, "RivalTitle", "Electric Wolf",
                new Vector2(80, 10), TextAlignmentOptions.Left);
            titleText.fontSize = 18;
            titleText.color = new Color(0.7f, 0.85f, 1f);
            titleText.fontStyle = FontStyles.Italic;
            var titleRT = titleText.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.4f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0f, 1f);
            titleRT.offsetMin = new Vector2(0, -75); titleRT.offsetMax = new Vector2(-10, -45);

            // Small HP bar slider on the right lower area
            var sliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIVAL_SLIDER_PATH);
            GameObject hpBarGO;
            Image hpFillImg = null;
            if (sliderPrefab != null)
            {
                hpBarGO = (GameObject)PrefabUtility.InstantiatePrefab(sliderPrefab, card.transform);
                hpBarGO.name = "RivalHPBar";
                var slider = hpBarGO.GetComponentInChildren<Slider>();
                if (slider != null && slider.fillRect != null)
                    hpFillImg = slider.fillRect.GetComponent<Image>();
                else
                {
                    hpFillImg = hpBarGO.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(i => i.name.ToLower().Contains("fill"));
                }
            }
            else
            {
                // Fallback — simple image bar
                hpBarGO = new GameObject("RivalHPBar");
                hpBarGO.transform.SetParent(card.transform, false);
                var bg = hpBarGO.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.6f);
                var fillGO = new GameObject("Fill");
                fillGO.transform.SetParent(hpBarGO.transform, false);
                hpFillImg = fillGO.AddComponent<Image>();
                hpFillImg.color = new Color(1f, 0.25f, 0.25f);
                hpFillImg.type = Image.Type.Filled;
                hpFillImg.fillMethod = Image.FillMethod.Horizontal;
                hpFillImg.fillAmount = 1f;
                var fillRT = fillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
                fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            }

            var hpBarRT = hpBarGO.GetComponent<RectTransform>();
            hpBarRT.anchorMin = new Vector2(0.4f, 0f); hpBarRT.anchorMax = new Vector2(1f, 0f);
            hpBarRT.pivot = new Vector2(0.5f, 0f);
            hpBarRT.offsetMin = new Vector2(10, 20); hpBarRT.offsetMax = new Vector2(-14, 60);

            // Hide the slider prefab's default internal label
            foreach (var tm in hpBarGO.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tm != null) tm.gameObject.SetActive(false);
            }
            foreach (var lt in hpBarGO.GetComponentsInChildren<Text>(true))
            {
                lt.gameObject.SetActive(false);
            }

            // Our own HP text
            var hpText = CreateOrGetText(card.transform, "RivalHPText", "HP 100%",
                new Vector2(80, -20), TextAlignmentOptions.Center);
            hpText.fontSize = 16;
            hpText.color = Color.white;
            var hpTextRT = hpText.GetComponent<RectTransform>();
            hpTextRT.anchorMin = new Vector2(0.4f, 0f); hpTextRT.anchorMax = new Vector2(1f, 0f);
            hpTextRT.pivot = new Vector2(0.5f, 0f);
            hpTextRT.offsetMin = new Vector2(10, 22); hpTextRT.offsetMax = new Vector2(-14, 58);

            // Attach RivalDisplay and wire refs
            var rival = card.GetComponent<Sparq.UI.RivalDisplay>();
            if (rival == null) rival = card.AddComponent<Sparq.UI.RivalDisplay>();
            var t = typeof(Sparq.UI.RivalDisplay);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("hpFillImage",   flags)?.SetValue(rival, hpFillImg);
            t.GetField("hpText",        flags)?.SetValue(rival, hpText);
            t.GetField("nameText",      flags)?.SetValue(rival, nameText);
            t.GetField("titleText",     flags)?.SetValue(rival, titleText);
            t.GetField("portraitImage", flags)?.SetValue(rival, portraitImg);

            EditorUtility.SetDirty(card);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq UI] Rival card added. Fill wired: " + (hpFillImg != null));
            EditorUtility.DisplayDialog("Sparq UI",
                "✅ Volt rival card added top-right!\n\n" +
                "• Portrait (fitch.svg) on the left\n" +
                "• Name + title top-right\n" +
                "• HP bar draining as you close the XP gap\n\n" +
                "Hit ▶ Play → tap Karu a lot → watch Volt's HP empty.", "OK");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Nudge XP bar lower on the screen
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem("Sparq/Tweak/Lower XP Bar")]
        public static void LowerXPBar()
        {
            var bar = GameObject.Find("FantasyXPBar");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "No FantasyXPBar in scene.", "OK"); return; }
            var rt = bar.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 25f);
            EditorUtility.SetDirty(bar);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[Sparq] Lowered XP bar to Y=25.");
        }

        // ──────────────────────────────────────────────────────────────────────
        // Add the quest list UI to the Home scene
        // ──────────────────────────────────────────────────────────────────────
        [MenuItem("Sparq/13. Add Quest List")]
        public static void AddQuestList()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) { EditorUtility.DisplayDialog("Sparq UI", "No Canvas in scene.", "OK"); return; }

            // Ensure QuestManager exists
            var qm = Object.FindAnyObjectByType<Sparq.Systems.QuestManager>();
            if (qm == null)
            {
                var gmGO = GameObject.Find("GameManager");
                if (gmGO == null) gmGO = new GameObject("GameManager");
                gmGO.AddComponent<Sparq.Systems.QuestManager>();
            }

            // Remove old list if present
            var old = GameObject.Find("QuestList");
            if (old != null) Object.DestroyImmediate(old);

            // Container positioned left side of screen, below Karu
            var list = new GameObject("QuestList");
            list.transform.SetParent(canvas.transform, false);
            var rt = list.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(20, -60);
            rt.sizeDelta = new Vector2(420, 240);

            // Background panel for the quest list
            var bg = list.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.04f, 0.16f, 0.65f);

            // Title "Today's Quests"
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(list.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.offsetMin = new Vector2(12, -42); titleRT.offsetMax = new Vector2(-12, -6);
            var titleTm = titleGO.AddComponent<TextMeshProUGUI>();
            titleTm.text = "Today's Quests";
            titleTm.fontSize = 22;
            titleTm.color = new Color(1f, 0.92f, 0.5f);
            titleTm.fontStyle = FontStyles.Bold;
            titleTm.alignment = TextAlignmentOptions.MidlineLeft;

            // Rows container with VerticalLayoutGroup
            var rowsGO = new GameObject("Rows");
            rowsGO.transform.SetParent(list.transform, false);
            var rowsRT = rowsGO.AddComponent<RectTransform>();
            rowsRT.anchorMin = new Vector2(0f, 0f); rowsRT.anchorMax = new Vector2(1f, 1f);
            rowsRT.offsetMin = new Vector2(8, 8); rowsRT.offsetMax = new Vector2(-8, -44);
            var vlg = rowsGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Attach QuestListUI controller and wire rowsParent
            var listUI = list.AddComponent<Sparq.UI.QuestListUI>();
            var rowsField = typeof(Sparq.UI.QuestListUI).GetField("rowsParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rowsField?.SetValue(listUI, rowsGO.transform);

            EditorUtility.SetDirty(list);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq UI] QuestList added.");
            EditorUtility.DisplayDialog("Sparq UI",
                "✅ Quest list added!\n\n" +
                "• 3 default quests auto-seed on first run:\n  Morning walk, Drink water, Focus session\n" +
                "• Tap a row → complete it → gain XP\n\n" +
                "Hit ▶ Play → click the quests to see the loop.", "OK");
        }

        private static TMP_Text CreateOrGetText(Transform parent, string name, string initialText, Vector2 anchoredPos, TextAlignmentOptions align)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null) { go = existing.gameObject; }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.AddComponent<RectTransform>();
                go.AddComponent<TextMeshProUGUI>();
            }
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(200, 40);

            var txt = go.GetComponent<TMP_Text>();
            txt.text = initialText;
            txt.fontSize = 22;
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
