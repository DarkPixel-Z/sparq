using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqBottomFix
    {
        [MenuItem("Sparq/82. Lift forest up + smaller bottom nav (no overlap)")]
        public static void Apply()
        {
            // 1. Lift the forest up so bushes don't cross bottom nav
            var forest = GameObject.Find("[Forest]");
            if (forest != null)
            {
                forest.transform.position = new Vector3(0f, 2.0f, 0f);
            }

            // Lift Karu and Mochi too
            var karu = GameObject.Find("Karu");
            if (karu != null && karu.activeSelf)
            {
                karu.transform.position = new Vector3(0f, 0.5f, 0f);
            }

            // Ensure Mochi exists; recreate if missing
            var mochi = GameObject.Find("Mochi");
            if (mochi == null)
            {
                mochi = CreateMochi();
            }
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(1.4f, 0.1f, 0f);
            }

            // Hide Una sprite if visible (she becomes the ? help icon UI button)
            var una = GameObject.Find("Una");
            if (una != null) una.SetActive(false);
            else
            {
                foreach (var unaRT in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (unaRT != null && unaRT.name == "Una") { unaRT.gameObject.SetActive(false); break; }
                }
            }
            // Make sure HelpIcon ? button exists
            EnsureHelpIcon();

            // 2. Shrink bottom nav + fix text wrapping
            var bar = GameObject.Find("BottomNav");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(0, 70);   // shorter

                var hlg = bar.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    hlg.padding = new RectOffset(6, 6, 6, 6);
                    hlg.spacing = 4;
                }

                foreach (Transform tab in bar.transform)
                {
                    if (!tab.name.StartsWith("Tab_")) continue;
                    var le = tab.GetComponent<LayoutElement>();
                    if (le != null) le.preferredHeight = 56;

                    // Make all text inside fit on one line
                    foreach (var tmp in tab.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (tmp == null) continue;
                        tmp.fontSize = 12;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                        tmp.overflowMode = TextOverflowModes.Overflow;
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Layout fixed:\n\n" +
                "• Forest lifted up (bushes no longer cross bottom nav)\n" +
                "• Karu + Mochi raised with the forest\n" +
                "• Bottom nav: shorter (70px), 12pt bold, no word wrap\n" +
                "• HOME/JOURNAL/REMIND/FEED/PROFILE all fit on one line\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static GameObject CreateMochi()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/mochi.svg");
            if (sprite == null)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sparq/mochi.svg"))
                {
                    if (o is Sprite sp) { sprite = sp; break; }
                }
            }
            if (sprite == null) return null;

            var mochi = new GameObject("Mochi");
            mochi.transform.position = new Vector3(1.4f, 0.1f, 0f);
            mochi.transform.localScale = Vector3.one * 0.5f;
            var sr = mochi.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;
            mochi.AddComponent<Sparq.Cinematic.IdleBreathing>();
            return mochi;
        }

        private static void EnsureHelpIcon()
        {
            if (GameObject.Find("HelpIcon") != null) return;

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);
            var rt = help.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(14f, 90f);
            rt.sizeDelta = new Vector2(48, 48);

            help.GetComponent<Image>().color = new Color(0.4f, 0.85f, 1f, 0.95f);

            var qGO = new GameObject("Q", typeof(RectTransform));
            qGO.transform.SetParent(help.transform, false);
            var qrt = qGO.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var tm = qGO.AddComponent<TextMeshProUGUI>();
            tm.text = "?";
            tm.fontSize = 30;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = new Color(0.05f, 0.15f, 0.30f);
            tm.raycastTarget = false;

            var btn = help.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });
        }
    }
}
