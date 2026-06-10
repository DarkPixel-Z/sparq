using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Replaces the plain "?" help icon with an Una-faced help button.
    /// • Una sprite as the icon visual
    /// • Small "?" badge in the top-left corner
    /// • Placed on the LEFT side of the screen, above the bottom nav
    /// </summary>
    public static class SparqUnaHelpButton
    {
        [MenuItem("Sparq/84. Una as help button (? badge top-left)")]
        public static void Apply()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Hide world-space Una sprite (the help icon replaces her)
            var una = GameObject.Find("Una");
            if (una != null) una.SetActive(false);
            else
            {
                foreach (var rt in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (rt != null && rt.name == "Una") { rt.gameObject.SetActive(false); break; }
                }
            }

            // Remove old HelpIcon
            var old = GameObject.Find("HelpIcon");
            if (old != null) Object.DestroyImmediate(old);

            // Build the new Una help button
            var go = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas.transform, false);
            var rt2 = go.GetComponent<RectTransform>();
            rt2.anchorMin = new Vector2(0f, 0f);
            rt2.anchorMax = new Vector2(0f, 0f);
            rt2.pivot = new Vector2(0f, 0f);
            rt2.anchoredPosition = new Vector2(14f, 90f);
            rt2.sizeDelta = new Vector2(72, 72);

            // Background plate (transparent so the Una sprite shows through clean)
            go.GetComponent<Image>().color = new Color(0.20f, 0.10f, 0.30f, 0.5f);

            // Una sprite
            var avatar = new GameObject("Una", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(go.transform, false);
            var avRT = avatar.GetComponent<RectTransform>();
            avRT.anchorMin = Vector2.zero; avRT.anchorMax = Vector2.one;
            avRT.offsetMin = new Vector2(4, 4); avRT.offsetMax = new Vector2(-4, -4);
            var avImg = avatar.GetComponent<Image>();
            avImg.preserveAspect = true;
            avImg.raycastTarget = false;

            // Load una.svg
            Sprite unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/una.svg");
            if (unaSprite == null)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sparq/una.svg"))
                {
                    if (o is Sprite sp) { unaSprite = sp; break; }
                }
            }
            if (unaSprite != null) avImg.sprite = unaSprite;

            // "?" badge in TOP-LEFT corner of the button
            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(go.transform, false);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(-4f, 4f);
            brt.sizeDelta = new Vector2(28, 28);
            badge.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);
            badge.GetComponent<Image>().raycastTarget = false;

            var qGO = new GameObject("Q", typeof(RectTransform));
            qGO.transform.SetParent(badge.transform, false);
            var qrt = qGO.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var qtm = qGO.AddComponent<TextMeshProUGUI>();
            qtm.text = "?";
            qtm.fontSize = 22;
            qtm.fontStyle = FontStyles.Bold;
            qtm.alignment = TextAlignmentOptions.Center;
            qtm.color = new Color(0.05f, 0.02f, 0.10f);
            qtm.raycastTarget = false;

            // Wire button → open help popup
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });

            // Subtle pulse so it's noticeable
            go.AddComponent<HelpPulse>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Una is now the HELP button.\n\n" +
                "• Una's sprite as the visual\n" +
                "• Yellow '?' badge top-left of the icon\n" +
                "• Placed on left side, above bottom nav\n" +
                "• Tap → opens tutorial popup\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private class HelpPulse : MonoBehaviour
        {
            float t;
            void Update()
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(t * 2f) * 0.06f;
                transform.localScale = new Vector3(s, s, 1);
            }
        }
    }
}
