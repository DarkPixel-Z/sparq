// WelcomePanel.cs — first-run onboarding (FTUE).
//
// A short, skippable 4-page intro shown ONCE on first launch that teaches the
// thing that makes Sparq different: doing real self-care powers up your game
// squad. Without this, a new player sees a generic idle RPG and misses the hook.
//
// Shown via WelcomePanel.ShowIfFirstTime() from the lobby. Gated by a one-time
// PlayerPrefs flag so it never nags; re-runnable from Settings → Replay intro.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class WelcomePanel
    {
        private const string SEEN_KEY = "sparq.ftue.welcomed";

        private static readonly Color CARD_BG  = new Color(0.15f, 0.15f, 0.20f, 1f);
        private static readonly Color CREAM    = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK_SOFT = new Color(0.80f, 0.82f, 0.90f, 1f);
        private static readonly Color GOLD     = new Color(0.99f, 0.80f, 0.30f, 1f);
        private static readonly Color BTN_GREEN= new Color(0.32f, 0.74f, 0.42f, 1f);
        private static readonly Color PIP_OFF  = new Color(0.34f, 0.36f, 0.44f, 1f);

        // ── Page content ──
        private static readonly string[] TITLES =
        {
            "Welcome to Sparq",
            "Self-Care = Power",
            "Explore & Battle",
            "Grow Every Day",
        };
        private static readonly string[] BODIES =
        {
            "A little fantasy RPG that grows stronger when you take care of YOU in real life.",
            "Check in, finish a quest, or care for your pet to raise your VITALITY — it powers up your whole squad for the day.",
            "Send your hero squad up the world map, win battles, level up, and forge better gear.",
            "Come back daily for rewards and to keep your streak going. Your heroes grow right alongside you. Ready?",
        };
        private static readonly Color[] ACCENTS =
        {
            new Color(0.30f, 0.72f, 0.74f),   // teal
            new Color(0.42f, 0.82f, 0.50f),   // green (vitality)
            new Color(0.98f, 0.66f, 0.30f),   // gold/orange (battle)
            new Color(0.66f, 0.50f, 0.95f),   // purple (growth)
        };

        private static GameObject _root;
        private static int _page;
        private static Image _accent;
        private static TMP_Text _title, _body, _btnLbl;
        private static Image[] _pips;

        /// <summary>Show the intro only on a brand-new player's first launch.</summary>
        public static void ShowIfFirstTime()
        {
            if (PlayerPrefs.GetInt(SEEN_KEY, 0) == 1) return;
            Show();
        }

        /// <summary>Force-show (Settings → Replay intro).</summary>
        public static void Show()
        {
            if (_root != null) return;
            EnsureEventSystem();
            _page = 0;

            _root = new GameObject("Sparq_WelcomePanel",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(_root.GetComponent<RectTransform>());
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16800;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var dim = NewGO("Dim", _root.transform, typeof(Image));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.92f);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(860, 1120);
            card.GetComponent<Image>().color = CARD_BG;

            // Accent banner (top) — colour changes per page.
            _accent = NewGO("Accent", card.transform, typeof(Image)).GetComponent<Image>();
            var art = _accent.rectTransform;
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1);
            art.anchoredPosition = Vector2.zero; art.sizeDelta = new Vector2(0, 360);
            _accent.raycastTarget = false;

            // Sparq logo, centred in the accent banner.
            var logo = NewGO("SparqLogo", _accent.transform, typeof(Image));
            var emRT = logo.GetComponent<RectTransform>();
            emRT.anchorMin = emRT.anchorMax = new Vector2(0.5f, 0.5f);
            emRT.pivot = new Vector2(0.5f, 0.5f);
            emRT.sizeDelta = new Vector2(360, 240);
            var emImg = logo.GetComponent<Image>();
            var logoSp = LoadSprite("Assets/Art/Sparq/sparq-logo-transparent.png");
            if (logoSp != null) { emImg.sprite = logoSp; emImg.preserveAspect = true; }
            else emImg.color = new Color(1f, 1f, 1f, 0.22f);   // graceful fallback
            emImg.raycastTarget = false;

            // Skip (top-right)
            var skip = NewGO("Skip", card.transform, typeof(Image), typeof(Button));
            var skRT = skip.GetComponent<RectTransform>();
            skRT.anchorMin = new Vector2(1, 1); skRT.anchorMax = new Vector2(1, 1);
            skRT.pivot = new Vector2(1, 1);
            skRT.anchoredPosition = new Vector2(-18, -18);
            skRT.sizeDelta = new Vector2(150, 64);
            skip.GetComponent<Image>().color = new Color(0, 0, 0, 0.30f);
            var skBtn = skip.GetComponent<Button>(); skBtn.targetGraphic = skip.GetComponent<Image>();
            skBtn.onClick.AddListener(Finish);
            var skLbl = MakeText(skip.transform, "L", "Skip", 28, FontStyles.Bold, CREAM);
            Stretch(skLbl.rectTransform); skLbl.alignment = TextAlignmentOptions.Center;

            // Title — big and gold so it reads as the page header.
            _title = MakeText(card.transform, "Title", "", 64, FontStyles.Bold, GOLD);
            try { _title.outlineWidth = 0.25f; _title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = _title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(36, -490); tRT.offsetMax = new Vector2(-36, -376);
            _title.alignment = TextAlignmentOptions.Center;

            // Body — bigger + more line spacing so it's easy to read end-to-end.
            _body = MakeText(card.transform, "Body", "", 40, FontStyles.Normal, INK_SOFT);
            var bRT = _body.rectTransform;
            bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.offsetMin = new Vector2(48, -800); bRT.offsetMax = new Vector2(-48, -500);
            _body.alignment = TextAlignmentOptions.Top;
            _body.textWrappingMode = TextWrappingModes.Normal;
            _body.lineSpacing = 10f;

            // Progress pips
            _pips = new Image[TITLES.Length];
            var pipHolder = NewGO("Pips", card.transform, typeof(RectTransform));
            var phRT = pipHolder.GetComponent<RectTransform>();
            phRT.anchorMin = new Vector2(0.5f, 0); phRT.anchorMax = new Vector2(0.5f, 0);
            phRT.pivot = new Vector2(0.5f, 0);
            phRT.anchoredPosition = new Vector2(0, 230);
            phRT.sizeDelta = new Vector2(400, 30);
            float step = 56f, startX = -(step * (TITLES.Length - 1)) / 2f;
            for (int i = 0; i < TITLES.Length; i++)
            {
                var pip = NewGO($"Pip{i}", pipHolder.transform, typeof(Image));
                var pRT = pip.GetComponent<RectTransform>();
                pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
                pRT.pivot = new Vector2(0.5f, 0.5f);
                pRT.anchoredPosition = new Vector2(startX + i * step, 0);
                pRT.sizeDelta = new Vector2(22, 22);
                _pips[i] = pip.GetComponent<Image>();
                _pips[i].raycastTarget = false;
            }

            // Next / Start button
            var next = NewGO("Next", card.transform, typeof(Image), typeof(Button));
            var nRT = next.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0.5f, 0); nRT.anchorMax = new Vector2(0.5f, 0);
            nRT.pivot = new Vector2(0.5f, 0);
            nRT.anchoredPosition = new Vector2(0, 56);
            nRT.sizeDelta = new Vector2(560, 130);
            next.GetComponent<Image>().color = BTN_GREEN;
            var nBtn = next.GetComponent<Button>(); nBtn.targetGraphic = next.GetComponent<Image>();
            nBtn.onClick.AddListener(OnNext);
            _btnLbl = MakeText(next.transform, "L", "Next", 42, FontStyles.Bold, Color.white);
            try { _btnLbl.outlineWidth = 0.22f; _btnLbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Stretch(_btnLbl.rectTransform); _btnLbl.alignment = TextAlignmentOptions.Center;

            RenderPage();
            Debug.Log("[WelcomePanel] FTUE shown.");
        }

        private static void OnNext()
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            if (_page >= TITLES.Length - 1) { Finish(); return; }
            _page++;
            RenderPage();
        }

        private static void RenderPage()
        {
            int p = Mathf.Clamp(_page, 0, TITLES.Length - 1);
            if (_accent != null) _accent.color = ACCENTS[p];
            if (_title != null) _title.text = TITLES[p];
            if (_body != null) _body.text = BODIES[p];
            if (_btnLbl != null) _btnLbl.text = (p >= TITLES.Length - 1) ? "Start Playing" : "Next";
            if (_pips != null)
                for (int i = 0; i < _pips.Length; i++)
                    if (_pips[i] != null) _pips[i].color = (i == p) ? GOLD : PIP_OFF;
        }

        private static void Finish()
        {
            PlayerPrefs.SetInt(SEEN_KEY, 1);
            PlayerPrefs.Save();
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Victory); } catch {}
            Hide();
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _accent = null; _title = null; _body = null; _btnLbl = null; _pips = null;
        }

        /// <summary>Settings → Replay intro: clear the flag and show it again.</summary>
        public static void Replay()
        {
            PlayerPrefs.SetInt(SEEN_KEY, 0);
            PlayerPrefs.Save();
            Show();
        }

        // ── primitives ──
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static Sprite LoadSprite(string path) => Sparq.Core.SpriteLoader.Load(path);

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (existing != null && existing.isActiveAndEnabled) return;
            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<EventSystem>();
            if (es != null) es.enabled = true;
        }
    }
}
