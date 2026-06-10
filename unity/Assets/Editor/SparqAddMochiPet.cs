using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// • Adds Mochi the pet (axolotl) next to Karu in the forest
    /// • Moves Una to a small "?" help icon in the bottom-left corner
    /// </summary>
    public static class SparqAddMochiPet
    {
        [MenuItem("Sparq/78. Add Mochi pet + relocate Una as help icon")]
        public static void Apply()
        {
            AddMochi();
            RelocateUna();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Mochi pet added + Una relocated.\n\n" +
                "• Mochi (small axolotl) bobbing next to Karu in forest\n" +
                "• Una shrunk to a small '?' help icon (bottom-left corner)\n" +
                "• Tap Una → opens tutorial popup as before\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void AddMochi()
        {
            // Remove old Mochi if present
            var old = GameObject.Find("Mochi");
            if (old != null) Object.DestroyImmediate(old);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/mochi.svg");
            if (sprite == null)
            {
                // Try as default sprite asset
                var allSubs = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sparq/mochi.svg");
                foreach (var o in allSubs)
                {
                    if (o is Sprite sp) { sprite = sp; break; }
                }
            }
            if (sprite == null)
            {
                Debug.LogWarning("[Sparq] mochi.svg sprite missing — skipping pet add.");
                return;
            }

            var mochi = new GameObject("Mochi");

            // Position near Karu, slightly to the right and lower
            var karu = GameObject.Find("Karu");
            Vector3 basePos = karu != null
                ? karu.transform.position + new Vector3(1.4f, -0.4f, 0)
                : new Vector3(1.5f, -1f, 0);
            mochi.transform.position = basePos;
            mochi.transform.localScale = Vector3.one * 0.5f; // smaller than Karu

            var sr = mochi.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;  // in front of forest, behind near foliage

            // Bob alongside Karu but offset phase so they look organic
            mochi.AddComponent<Sparq.Cinematic.IdleBreathing>();
        }

        private static void RelocateUna()
        {
            // Find Una even if disabled
            GameObject una = GameObject.Find("Una");
            if (una == null)
            {
                foreach (var unaRT in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (unaRT != null && unaRT.name == "Una") { una = unaRT.gameObject; break; }
                }
            }
            if (una != null)
            {
                // Hide the world-space Una sprite
                una.SetActive(false);
            }

            // Add a small UI '?' help button instead, anchored bottom-left above the bottom nav
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var oldHelp = GameObject.Find("HelpIcon");
            if (oldHelp != null) Object.DestroyImmediate(oldHelp);

            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);
            var rt = help.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(14f, 130f); // above bottom nav
            rt.sizeDelta = new Vector2(56, 56);

            // Round-ish background (just colored Image)
            var img = help.GetComponent<Image>();
            img.color = new Color(0.4f, 0.85f, 1f, 0.95f); // sky blue help icon

            // ? glyph
            var qGO = new GameObject("Q", typeof(RectTransform));
            qGO.transform.SetParent(help.transform, false);
            var qrt = qGO.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var tm = qGO.AddComponent<TextMeshProUGUI>();
            tm.text = "?";
            tm.fontSize = 36;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = new Color(0.05f, 0.15f, 0.30f);
            tm.raycastTarget = false;

            // Tap → open help popup
            var btn = help.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });

            // Subtle pulse so it's noticeable
            help.AddComponent<HelpPulse>();
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
