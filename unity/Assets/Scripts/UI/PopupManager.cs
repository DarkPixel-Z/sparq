using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Spawns full-screen popups (level up, victory, defeat, etc.).
    /// Place a [PopupManager] object in the scene with prefabs wired.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] public GameObject levelUpPrefab;
        [SerializeField] public GameObject dailyBonusPrefab;
        [SerializeField] public GameObject shopPrefab;
        [SerializeField] public GameObject bagPrefab;
        [SerializeField] public GameObject stageMapPrefab;
        [SerializeField] public GameObject lootDropPrefab;
        [SerializeField] public GameObject victoryPrefab;

        private Canvas _canvas;
        private GameObject _current;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = Object.FindAnyObjectByType<Canvas>();
        }

        public void ShowLevelUp(int newLevel)
        {
            if (levelUpPrefab == null)
            {
                Debug.LogWarning("[PopupManager] levelUpPrefab not assigned.");
                return;
            }
            if (_canvas == null) _canvas = Object.FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            if (_current != null) Destroy(_current);

            // Root wrapper: full-screen dimmer behind popup that catches clicks to dismiss.
            _current = new GameObject("LevelUpPopupRoot", typeof(RectTransform), typeof(CanvasGroup));
            _current.transform.SetParent(_canvas.transform, false);
            var rootRT = _current.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
            _current.transform.SetAsLastSibling();

            // Dimmer
            var dim = new GameObject("Dimmer", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            dim.transform.SetParent(_current.transform, false);
            var dimRT = dim.GetComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.55f);

            // Dismiss on click anywhere on dimmer
            var dimBtn = dim.AddComponent<UnityEngine.UI.Button>();
            dimBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            dimBtn.onClick.AddListener(Dismiss);

            // Instantiate the actual prefab on top of dimmer
            var popup = Instantiate(levelUpPrefab, _current.transform);
            popup.name = "LevelUpContent";

            // Disable any buttons inside the prefab so they don't block our dismiss
            foreach (var b in popup.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(Dismiss);
            }
            // Disable raycast on Images so clicks pass through to dimmer
            foreach (var img in popup.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                // Keep buttons alive for dismiss, but images without buttons shouldn't block
                if (img.GetComponent<UnityEngine.UI.Button>() == null)
                    img.raycastTarget = false;
            }

            // Populate dynamic text fields — replace ALL instances, not just first
            SetTextAll(popup, "Text_Num",   newLevel.ToString());
            SetTextAll(popup, "Text_Title", "LEVEL UP!");
            // Replace the default "Max Energy..." subtitle with our ADHD-flavor reward
            ReplaceTextContaining(popup, "Max Energy Increased", GetLevelReward(newLevel));
            ReplaceTextContaining(popup, "reached the Next Level", "You levelled up!");

            StartCoroutine(BouncePopup(popup.transform));
            StartCoroutine(AutoDismiss(6f));
        }

        private string GetLevelReward(int level)
        {
            // Flavor text ADHD-themed
            switch (level)
            {
                case 2: return "You unlocked Focus Dungeon!";
                case 3: return "Max Pet Energy increased.";
                case 5: return "New rival appeared: Vex the Shadow Fox!";
                case 10: return "Your streak shield is now unbreakable.";
                default: return $"Keep crushing those quests.";
            }
        }

        private IEnumerator BouncePopup(Transform t)
        {
            if (t == null) yield break;
            float dur = 0.45f;
            float e = 0f;
            Vector3 baseScale = Vector3.one;
            t.localScale = Vector3.zero;
            while (e < dur)
            {
                if (t == null) yield break;
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / dur);
                float s = k < 0.7f
                    ? Mathf.Lerp(0f, 1.15f, k / 0.7f)
                    : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
                t.localScale = baseScale * s;
                yield return null;
            }
            if (t != null) t.localScale = baseScale;
        }

        private IEnumerator AutoDismiss(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_current != null) Dismiss();
        }

        // ── Shop / Bag panels ─────────────────────────────────────────────────
        public void OpenShop() => OpenFullScreenPanel(shopPrefab, "ShopRoot");
        public void OpenBag()  => OpenFullScreenPanel(bagPrefab,  "BagRoot");
        public void OpenMap()
        {
            OpenFullScreenPanel(stageMapPrefab, "MapRoot");
            // After panel spawns, wire map controller
            if (_current != null)
            {
                var content = _current.transform.Find("MapRootContent");
                if (content != null)
                {
                    var ctrl = content.gameObject.AddComponent<Sparq.UI.StageMapController>();
                    ctrl.Init();
                }
            }
        }

        private void OpenFullScreenPanel(GameObject prefab, string rootName)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PopupManager] {rootName} prefab not assigned.");
                return;
            }
            if (_canvas == null) _canvas = Object.FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            if (_current != null) Destroy(_current);

            _current = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas), typeof(GraphicRaycaster));
            _current.transform.SetParent(_canvas.transform, false);
            var rootRT = _current.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;

            // Override canvas to render on top of everything
            var overrideCanvas = _current.GetComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 1000;
            _current.transform.SetAsLastSibling();

            // Dim background
            var dim = new GameObject("Dimmer", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            dim.transform.SetParent(_current.transform, false);
            var dimRT = dim.GetComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.7f);
            // Dimmer click closes the panel
            var dimBtn = dim.AddComponent<UnityEngine.UI.Button>();
            dimBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            dimBtn.onClick.AddListener(Dismiss);

            // Panel content (instantiated prefab)
            var panel = Instantiate(prefab, _current.transform);
            panel.name = rootName + "Content";
            // Force the panel to fit the canvas
            var pRT = panel.GetComponent<RectTransform>();
            if (pRT != null)
            {
                pRT.anchorMin = new Vector2(0.05f, 0.05f);
                pRT.anchorMax = new Vector2(0.95f, 0.95f);
                pRT.offsetMin = Vector2.zero;
                pRT.offsetMax = Vector2.zero;
                pRT.localScale = Vector3.one;
            }

            // Add a corner Close ✕ button (top right of panel)
            BuildCloseButton(panel);

            StartCoroutine(BouncePopup(panel.transform));
        }

        private void BuildCloseButton(GameObject panel)
        {
            var closeGO = new GameObject("CloseBtn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            closeGO.transform.SetParent(panel.transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot     = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-12f, -12f);
            crt.sizeDelta = new Vector2(56, 56);
            var img = closeGO.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.15f, 0.05f, 0.2f, 0.9f);
            var btn = closeGO.GetComponent<UnityEngine.UI.Button>();
            btn.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            btn.onClick.AddListener(Dismiss);

            // X label
            var xGO = new GameObject("X", typeof(RectTransform));
            xGO.transform.SetParent(closeGO.transform, false);
            var xrt = xGO.GetComponent<RectTransform>();
            xrt.anchorMin = Vector2.zero; xrt.anchorMax = Vector2.one;
            xrt.offsetMin = Vector2.zero; xrt.offsetMax = Vector2.zero;
            var tm = xGO.AddComponent<TextMeshProUGUI>();
            tm.text = "X";
            tm.fontSize = 32;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = Color.white;
            tm.fontStyle = FontStyles.Bold;
            tm.raycastTarget = false;

            closeGO.transform.SetAsLastSibling();
        }

        // ── Loot Drop (rival defeated) ────────────────────────────────────────
        public void ShowLootDrop(string rivalName, int xp, int coins)
        {
            if (lootDropPrefab == null)
            {
                Debug.LogWarning("[PopupManager] lootDropPrefab not assigned. Falling back to floater.");
                return;
            }
            if (_canvas == null) _canvas = Object.FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            if (_current != null) Destroy(_current);

            _current = new GameObject("LootRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas), typeof(GraphicRaycaster));
            _current.transform.SetParent(_canvas.transform, false);
            var rootRT = _current.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;

            var ovr = _current.GetComponent<Canvas>();
            ovr.overrideSorting = true; ovr.sortingOrder = 1100;
            _current.transform.SetAsLastSibling();

            // Dimmer
            var dim = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(_current.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0,0,0,0.7f);

            // Spawn the prefab
            var popup = Instantiate(lootDropPrefab, _current.transform);
            popup.name = "LootContent";

            // Override the reward numbers — replace any visible numeric text with our coins/xp
            var tmps = popup.GetComponentsInChildren<TMP_Text>(true);
            int textIndex = 0;
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;
                var t = tmp.text ?? "";
                // The prefab has placeholder numbers like "1000" and "1"
                if (t == "1000" || t == "100" || t == "5000")
                {
                    tmp.text = coins.ToString();
                }
                else if (t == "1" && textIndex == 0)
                {
                    tmp.text = "+" + xp + " XP";
                    textIndex++;
                }
            }

            // Wire ANY button (and the dimmer) to dismiss
            foreach (var b in popup.GetComponentsInChildren<Button>(true))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(Dismiss);
            }
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Dismiss);

            // Top-most invisible click-catcher (full-screen dismiss)
            var clickAll = new GameObject("ClickAll", typeof(RectTransform), typeof(Image), typeof(Button));
            clickAll.transform.SetParent(_current.transform, false);
            var crt = clickAll.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            clickAll.GetComponent<Image>().color = new Color(0,0,0,0.001f);
            var caBtn = clickAll.GetComponent<Button>();
            caBtn.transition = Selectable.Transition.None;
            caBtn.onClick.AddListener(Dismiss);
            clickAll.transform.SetAsLastSibling();

            StartCoroutine(BouncePopup(popup.transform));
        }

        // ── Daily Bonus ───────────────────────────────────────────────────────
        public void ShowDailyBonus(int dayToShow, System.Action<int> onClaim)
        {
            if (dailyBonusPrefab == null)
            {
                Debug.LogWarning("[PopupManager] dailyBonusPrefab not assigned.");
                onClaim?.Invoke(dayToShow);
                return;
            }
            if (_canvas == null) _canvas = Object.FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            if (_current != null) Destroy(_current);

            // Wrapper with dimmer
            _current = new GameObject("DailyBonusRoot", typeof(RectTransform), typeof(CanvasGroup));
            _current.transform.SetParent(_canvas.transform, false);
            var rootRT = _current.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
            _current.transform.SetAsLastSibling();

            var dim = new GameObject("Dimmer", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            dim.transform.SetParent(_current.transform, false);
            var dimRT = dim.GetComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.7f);

            var popup = Instantiate(dailyBonusPrefab, _current.transform);
            popup.name = "DailyBonusContent";

            // Wire any Button inside the popup to claim + dismiss
            foreach (var b in popup.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                b.onClick.RemoveAllListeners();
                int capturedDay = dayToShow;
                var capturedClaim = onClaim;
                b.onClick.AddListener(() =>
                {
                    capturedClaim?.Invoke(capturedDay);
                    Dismiss();
                });
            }

            // Dimmer click also claims+dismisses (fallback)
            var dimBtn = dim.AddComponent<UnityEngine.UI.Button>();
            dimBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            int capDay = dayToShow;
            var capClaim = onClaim;
            dimBtn.onClick.AddListener(() => { capClaim?.Invoke(capDay); Dismiss(); });

            // Top-most invisible click-catcher so ANY tap claims + dismisses,
            // even if the prefab's internal buttons swallow the dimmer click.
            var clickAll = new GameObject("ClickAnywhereToClaim", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            clickAll.transform.SetParent(_current.transform, false);
            var caRT = clickAll.GetComponent<RectTransform>();
            caRT.anchorMin = Vector2.zero; caRT.anchorMax = Vector2.one;
            caRT.offsetMin = Vector2.zero; caRT.offsetMax = Vector2.zero;
            var caImg = clickAll.GetComponent<UnityEngine.UI.Image>();
            caImg.color = new Color(0, 0, 0, 0.001f); // nearly invisible but raycastable
            var caBtn = clickAll.GetComponent<UnityEngine.UI.Button>();
            caBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            int caDay = dayToShow;
            var caClaim = onClaim;
            caBtn.onClick.AddListener(() => { caClaim?.Invoke(caDay); Dismiss(); });
            clickAll.transform.SetAsLastSibling();

            // Highlight today's reward by scaling its corresponding day card
            HighlightDay(popup, dayToShow);

            // Label "Touch to Continue" → "Tap to Claim"
            ReplaceTextContaining(popup, "Touch to Contionue", "Tap to claim");
            ReplaceTextContaining(popup, "Touch to Continue",  "Tap to claim");

            StartCoroutine(BouncePopup(popup.transform));
        }

        private static void HighlightDay(GameObject root, int day)
        {
            // The prefab has Text_Day fields like "DAY 1" .. "DAY 7"
            // Find the one matching `day`, scale up its parent for emphasis.
            string target = $"DAY {day}";
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.text != null && tmp.text.Trim().Equals(target, System.StringComparison.OrdinalIgnoreCase))
                {
                    var card = tmp.transform.parent != null ? tmp.transform.parent : tmp.transform;
                    card.localScale = Vector3.one * 1.12f;
                    // Tint color slightly gold
                    foreach (var img in card.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    {
                        if (img.transform == card) img.color = new Color(1f, 0.95f, 0.6f, 1f);
                    }
                    break;
                }
            }
        }

        public void Dismiss()
        {
            if (_current == null) return;
            var toFade = _current;
            _current = null;
            StartCoroutine(FadeOutAndDestroy(toFade));
        }

        private IEnumerator FadeOutAndDestroy(GameObject go)
        {
            if (go == null) yield break;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            float e = 0f;
            float dur = 0.25f;
            while (e < dur)
            {
                if (go == null || cg == null) yield break;
                e += Time.deltaTime;
                cg.alpha = 1f - (e / dur);
                go.transform.localScale = Vector3.one * (1f - 0.2f * (e / dur));
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        // -- Set ALL matches by GameObject name --
        private static void SetTextAll(GameObject root, string childName, string value)
        {
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject.name == childName)
                    tmp.text = value;
            }
        }

        // -- Replace any TMP_Text that currently contains the substring --
        private static void ReplaceTextContaining(GameObject root, string substring, string newValue)
        {
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!string.IsNullOrEmpty(tmp.text) && tmp.text.Contains(substring))
                    tmp.text = newValue;
            }
        }
    }
}
