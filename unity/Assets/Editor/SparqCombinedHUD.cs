using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqCombinedHUD
    {
        [MenuItem("Sparq/107. Combine Karu+Mochi into ONE stats box")]
        public static void Apply()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Find existing HUDs to copy data from + then destroy them
            var oldKaru = GameObject.Find("PlayerHUD");
            var oldMochi = GameObject.Find("MochiHUD");

            // Capture sprite + ctrl from Karu HUD before destruction
            Sprite karuAvatar = null;
            Sparq.UI.KaruStatsCard ctrl = null;
            if (oldKaru != null)
            {
                ctrl = oldKaru.GetComponent<Sparq.UI.KaruStatsCard>();
                foreach (var img in oldKaru.GetComponentsInChildren<Image>(true))
                {
                    if (img != null && img.gameObject.name == "Avatar")
                    {
                        karuAvatar = img.sprite;
                        break;
                    }
                }
            }

            Sprite mochiAvatar = null;
            if (oldMochi != null)
            {
                foreach (var img in oldMochi.GetComponentsInChildren<Image>(true))
                {
                    if (img != null && img.gameObject.name == "Avatar")
                    {
                        mochiAvatar = img.sprite;
                        break;
                    }
                }
            }
            if (mochiAvatar == null)
                mochiAvatar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/una-mage.png");

            if (oldKaru != null) Object.DestroyImmediate(oldKaru);
            if (oldMochi != null) Object.DestroyImmediate(oldMochi);

            // Build combined HUD
            var hud = new GameObject("PlayerHUD", typeof(RectTransform), typeof(Image));
            hud.transform.SetParent(canvas.transform, false);
            var rt = hud.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -8f);
            rt.sizeDelta = new Vector2(420, 200);  // 2 equal-size rows + accent

            hud.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.95f);

            // Yellow accent strip
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(hud.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);

            // ── Karu row (top half, 90px) ──
            BuildHeroRow(hud.transform, "Karu", karuAvatar, "Karu", "Lv.2", true,
                         new Color(1f, 0.85f, 0.35f), 0f);

            // Divider in the exact middle
            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(hud.transform, false);
            var drt = divider.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 1); drt.anchorMax = new Vector2(1, 1);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0, -100f);
            drt.sizeDelta = new Vector2(-20, 1);
            divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            // ── Mochi row (bottom half, 90px — SAME SIZE) ──
            BuildHeroRow(hud.transform, "Mochi", mochiAvatar, "Mochi", "Lv.1", true,
                         new Color(0.85f, 0.55f, 1f), -100f);

            // Re-attach KaruStatsCard controller to drive the Karu row's text
            var newCtrl = hud.AddComponent<Sparq.UI.KaruStatsCard>();
            var so = new SerializedObject(newCtrl);
            // Find the Karu row's name/level/XPBar refs
            var karuName = hud.transform.Find("KaruRow/Name");
            var karuLevel = hud.transform.Find("KaruRow/LevelText");
            var karuXP = hud.transform.Find("KaruRow/XPSlider");
            if (karuName  != null) so.FindProperty("nameText").objectReferenceValue  = karuName.GetComponent<TMP_Text>();
            if (karuLevel != null) so.FindProperty("levelText").objectReferenceValue = karuLevel.GetComponent<TMP_Text>();
            if (karuXP    != null) so.FindProperty("xpSlider").objectReferenceValue  = karuXP.GetComponent<Slider>();
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Combined stats box.\n\n" +
                "• Single 420×180 HUD top-right\n" +
                "• Karu row (top) + divider + Mochi row (bottom)\n" +
                "• Both share the same plate + yellow accent strip\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildHeroRow(Transform parent, string rowName, Sprite avatarSprite,
                                          string defaultName, string defaultLv, bool showXPBar,
                                          Color levelColor, float yOffset)
        {
            var row = new GameObject($"{rowName}Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, yOffset - 8);
            rt.sizeDelta = new Vector2(-16, 90);  // both rows same height

            // Avatar bg
            var avBg = new GameObject("AvatarBg", typeof(RectTransform), typeof(Image));
            avBg.transform.SetParent(row.transform, false);
            var arrt = avBg.GetComponent<RectTransform>();
            arrt.anchorMin = new Vector2(0, 0.5f); arrt.anchorMax = new Vector2(0, 0.5f);
            arrt.pivot = new Vector2(0, 0.5f);
            arrt.anchoredPosition = new Vector2(8, 0);
            float avSize = 70;  // both rows same avatar size
            arrt.sizeDelta = new Vector2(avSize, avSize);
            avBg.GetComponent<Image>().color = new Color(0.30f, 0.20f, 0.40f, 0.9f);

            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(avBg.transform, false);
            var avrt = avatar.GetComponent<RectTransform>();
            avrt.anchorMin = Vector2.zero; avrt.anchorMax = Vector2.one;
            avrt.offsetMin = new Vector2(3, 3); avrt.offsetMax = new Vector2(-3, -3);
            var aImg = avatar.GetComponent<Image>();
            aImg.preserveAspect = true;
            if (avatarSprite != null) aImg.sprite = avatarSprite;
            else aImg.color = new Color(0.5f, 0.5f, 0.5f);

            // Name text
            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(row.transform, false);
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.pivot = new Vector2(0, 0.5f);
            nrt.anchoredPosition = new Vector2(avSize + 16, showXPBar ? 18 : 8);
            nrt.sizeDelta = new Vector2(220, 32);
            var ntm = nameGO.AddComponent<TextMeshProUGUI>();
            ntm.text = defaultName;
            ntm.fontSize = showXPBar ? 28 : 22;
            ntm.fontStyle = FontStyles.Bold;
            ntm.color = Color.white;
            ntm.alignment = TextAlignmentOptions.Left;

            // Level badge
            var lvl = new GameObject("Level", typeof(RectTransform), typeof(Image));
            lvl.transform.SetParent(row.transform, false);
            var lrt = lvl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(avSize + 16, showXPBar ? -16 : -16);
            lrt.sizeDelta = new Vector2(60, 24);
            lvl.GetComponent<Image>().color = levelColor;

            var ltGO = new GameObject("LevelText", typeof(RectTransform));
            ltGO.transform.SetParent(lvl.transform, false);
            var ltrt = ltGO.GetComponent<RectTransform>();
            ltrt.anchorMin = Vector2.zero; ltrt.anchorMax = Vector2.one;
            ltrt.offsetMin = Vector2.zero; ltrt.offsetMax = Vector2.zero;
            var lttm = ltGO.AddComponent<TextMeshProUGUI>();
            lttm.text = defaultLv;
            lttm.fontSize = 14;
            lttm.fontStyle = FontStyles.Bold;
            lttm.color = new Color(0.05f, 0.02f, 0.10f);
            lttm.alignment = TextAlignmentOptions.Center;

            if (showXPBar)
            {
                // XP slider (Karu only)
                var xpBg = new GameObject("XPBg", typeof(RectTransform), typeof(Image));
                xpBg.transform.SetParent(row.transform, false);
                var xrt = xpBg.GetComponent<RectTransform>();
                xrt.anchorMin = new Vector2(0, 0.5f); xrt.anchorMax = new Vector2(0, 0.5f);
                xrt.pivot = new Vector2(0, 0.5f);
                xrt.anchoredPosition = new Vector2(avSize + 86, -16);
                xrt.sizeDelta = new Vector2(220, 18);
                xpBg.GetComponent<Image>().color = new Color(0.20f, 0.10f, 0.30f, 0.9f);

                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(xpBg.transform, false);
                var frt = fill.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1, 1);
                frt.pivot = new Vector2(0, 0.5f);
                frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);
                fill.GetComponent<Image>().color = new Color(0.4f, 0.95f, 0.45f);

                // Add Slider component to xpBg
                var slider = xpBg.AddComponent<Slider>();
                slider.fillRect = frt;
                slider.minValue = 0; slider.maxValue = 1; slider.value = 0;
                slider.interactable = false;

                xpBg.name = "XPSlider"; // so the controller can find it
            }
            else
            {
                // Subtitle for Mochi
                var sub = new GameObject("Subtitle", typeof(RectTransform));
                sub.transform.SetParent(row.transform, false);
                var srt = sub.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0, 0.5f); srt.anchorMax = new Vector2(0, 0.5f);
                srt.pivot = new Vector2(0, 0.5f);
                srt.anchoredPosition = new Vector2(avSize + 86, -16);
                srt.sizeDelta = new Vector2(220, 22);
                var stm = sub.AddComponent<TextMeshProUGUI>();
                stm.text = "Loyal Companion";
                stm.fontSize = 13;
                stm.fontStyle = FontStyles.Italic;
                stm.color = new Color(0.85f, 0.85f, 1f, 0.85f);
                stm.alignment = TextAlignmentOptions.Left;
            }
        }
    }
}
