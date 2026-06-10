using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Replaces the big Karu stats card with a small top-right player HUD:
    /// • Karu avatar (small circle)
    /// • User name
    /// • Lv badge
    /// Tap → opens a popup with full stats (ATK/DEF/SPD).
    /// Also makes the quest list smaller and the home stuff fits better.
    /// </summary>
    public static class SparqCompactHUD
    {
        [MenuItem("Sparq/55. Compact HUD (small Karu top-right + username)")]
        public static void Apply()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Remove old big card
            var old = GameObject.Find("KaruStatsCard");
            if (old != null) Object.DestroyImmediate(old);

            // Build the compact HUD
            BuildPlayerHUD(canvas.transform);

            // Shrink + reposition quest list — top-center, compact
            ShrinkQuests();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq HUD",
                "✅ Compact HUD applied:\n\n" +
                "• Big Karu card REMOVED\n" +
                "• Small player widget in top-right (avatar + name + Lv)\n" +
                "• Tap the avatar → full stats popup\n" +
                "• Quest list shrunk + centered\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildPlayerHUD(Transform canvasT)
        {
            var oldHUD = GameObject.Find("PlayerHUD");
            if (oldHUD != null) Object.DestroyImmediate(oldHUD);

            // Root in top-right
            var hud = new GameObject("PlayerHUD", typeof(RectTransform), typeof(Image), typeof(Button));
            hud.transform.SetParent(canvasT, false);
            var rt = hud.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.sizeDelta = new Vector2(220, 70);
            hud.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.85f);

            // Yellow accent on top
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(hud.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(0, 3);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.9f);
            accent.GetComponent<Image>().raycastTarget = false;

            // Avatar circle
            var avatarBg = new GameObject("AvatarBg", typeof(RectTransform), typeof(Image));
            avatarBg.transform.SetParent(hud.transform, false);
            var abrt = avatarBg.GetComponent<RectTransform>();
            abrt.anchorMin = new Vector2(0, 0.5f); abrt.anchorMax = new Vector2(0, 0.5f);
            abrt.pivot = new Vector2(0, 0.5f);
            abrt.anchoredPosition = new Vector2(8, 0);
            abrt.sizeDelta = new Vector2(54, 54);
            avatarBg.GetComponent<Image>().color = new Color(0.25f, 0.15f, 0.35f, 0.9f);

            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(avatarBg.transform, false);
            var avrt = avatar.GetComponent<RectTransform>();
            avrt.anchorMin = Vector2.zero; avrt.anchorMax = Vector2.one;
            avrt.offsetMin = new Vector2(4, 4); avrt.offsetMax = new Vector2(-4, -4);
            var aImg = avatar.GetComponent<Image>();
            aImg.preserveAspect = true;
            aImg.raycastTarget = false;

            // Use the assembled BearCatOwl prefab thumbnail (full character, not just head)
            var bearPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/2D Animal Character Pack/Prefabs/BearCatOwl.prefab");
            Texture2D thumb = null;
            if (bearPrefab != null)
            {
                thumb = AssetPreview.GetAssetPreview(bearPrefab);
                // GetAssetPreview is async — request again until ready
                int safety = 0;
                while (thumb == null && safety < 30)
                {
                    thumb = AssetPreview.GetAssetPreview(bearPrefab);
                    safety++;
                }
            }
            if (thumb != null)
            {
                aImg.sprite = Sprite.Create(thumb, new Rect(0, 0, thumb.width, thumb.height), new Vector2(0.5f, 0.5f));
                aImg.color = new Color(1f, 0.55f, 0.35f); // red-panda tint
            }
            else
            {
                // Fallback to head sprite
                var bearSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/2D Animal Character Pack/Sprites/Characters/Bears/Bear.png");
                foreach (var o in bearSprites)
                {
                    if (o is Sprite sp && sp.name.Contains("Head", System.StringComparison.OrdinalIgnoreCase))
                    {
                        aImg.sprite = sp;
                        aImg.color = new Color(1f, 0.55f, 0.35f);
                        break;
                    }
                }
            }

            // Username (use petName from save data — fallback "Karu")
            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(hud.transform, false);
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f); nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.pivot = new Vector2(0, 0.5f);
            nrt.anchoredPosition = new Vector2(72, 12);
            nrt.sizeDelta = new Vector2(140, 28);
            var nameTM = nameGO.AddComponent<TextMeshProUGUI>();
            nameTM.text = "Karu";
            nameTM.fontSize = 22;
            nameTM.fontStyle = FontStyles.Bold;
            nameTM.color = Color.white;
            nameTM.alignment = TextAlignmentOptions.Left;
            nameTM.raycastTarget = false;

            // Lv badge below name
            var levelGO = new GameObject("Level", typeof(RectTransform), typeof(Image));
            levelGO.transform.SetParent(hud.transform, false);
            var lrt = levelGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0.5f); lrt.anchorMax = new Vector2(0, 0.5f);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(72, -16);
            lrt.sizeDelta = new Vector2(60, 22);
            levelGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f);
            var levelTextGO = new GameObject("LvText", typeof(RectTransform));
            levelTextGO.transform.SetParent(levelGO.transform, false);
            var ltrt = levelTextGO.GetComponent<RectTransform>();
            ltrt.anchorMin = Vector2.zero; ltrt.anchorMax = Vector2.one;
            ltrt.offsetMin = Vector2.zero; ltrt.offsetMax = Vector2.zero;
            var levelTM = levelTextGO.AddComponent<TextMeshProUGUI>();
            levelTM.text = "Lv.1";
            levelTM.fontSize = 14;
            levelTM.fontStyle = FontStyles.Bold;
            levelTM.color = new Color(0.1f, 0.05f, 0.2f);
            levelTM.alignment = TextAlignmentOptions.Center;
            levelTM.raycastTarget = false;

            // XP mini bar to the right of level
            var xpBg = new GameObject("XPBg", typeof(RectTransform), typeof(Image));
            xpBg.transform.SetParent(hud.transform, false);
            var xpRT = xpBg.GetComponent<RectTransform>();
            xpRT.anchorMin = new Vector2(0, 0.5f); xpRT.anchorMax = new Vector2(0, 0.5f);
            xpRT.pivot = new Vector2(0, 0.5f);
            xpRT.anchoredPosition = new Vector2(140, -16);
            xpRT.sizeDelta = new Vector2(70, 12);
            xpBg.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.3f, 0.9f);
            var xpFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            xpFill.transform.SetParent(xpBg.transform, false);
            var xpfRT = xpFill.GetComponent<RectTransform>();
            xpfRT.anchorMin = Vector2.zero; xpfRT.anchorMax = new Vector2(1, 1);
            xpfRT.pivot = new Vector2(0, 0.5f);
            xpfRT.offsetMin = new Vector2(1, 1); xpfRT.offsetMax = new Vector2(-1, -1);
            xpFill.GetComponent<Image>().color = new Color(0.4f, 0.95f, 0.45f);
            var xpSlider = xpBg.AddComponent<Slider>();
            xpSlider.fillRect = xpfRT;
            xpSlider.minValue = 0; xpSlider.maxValue = 1; xpSlider.value = 0;
            xpSlider.interactable = false;

            // Wire to KaruStatsCard controller (still works since fields are private but we're using it)
            var ctrl = hud.AddComponent<Sparq.UI.KaruStatsCard>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("nameText").objectReferenceValue   = nameTM;
            so.FindProperty("levelText").objectReferenceValue  = levelTM;
            so.FindProperty("xpSlider").objectReferenceValue   = xpSlider;
            so.ApplyModifiedProperties();

            // Tap → open stats popup
            var btn = hud.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.PlayerStatsPopup.Show();
            });
        }

        private static void ShrinkQuests()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null) return;
            var qrt = ql.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(0.5f, 1f);
            qrt.anchorMax = new Vector2(0.5f, 1f);
            qrt.pivot     = new Vector2(0.5f, 1f);
            qrt.anchoredPosition = new Vector2(0f, -110f);
            qrt.sizeDelta = new Vector2(440, 280);
        }
    }
}
