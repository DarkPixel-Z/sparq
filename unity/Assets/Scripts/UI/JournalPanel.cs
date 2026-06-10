using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Journal — mood log. Polished rebuild skinned with Layer Lab sprites
    /// (Popup_Box frame, circle buttons), modelled on the Mission-panel look.
    ///
    /// Sections (scrollable):
    ///   1. Today's mood logger — 5 crystal buttons (Sparq.Systems.MoodService)
    ///   2. Streak banner
    ///   3. Last-14-days mood calendar grid
    ///   4. Recent entries list
    ///
    /// All data lives in MoodService (PlayerPrefs-backed) — this panel is
    /// pure presentation, so logging a mood just calls MoodService.Log and
    /// rebuilds the content.
    /// </summary>
    public static class JournalPanel
    {
        // ── Sprite paths ──
        private const string POPUP_BG     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg.png";
        private const string POPUP_BORDER = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Border.png";
        private const string CIRCLE_BG    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png";
        // A closed leather tome with a strap + gold buckle — the locked-journal visual.
        private const string BOOK_CLOSED  = "Assets/Casual Fantasy Book Icon Pack – 10 Stylized Magic Books/Resources/Icons/book_icon_07.png";

        // ── Palette — "bright & colourful", high-contrast pass: the popup
        // backdrop is a soft teal so the bright white cards/pages clearly
        // pop off it (a white-on-white card was washing out). Charcoal text,
        // teal spine, coral + gold accents.
        private static readonly Color CARD_DARK  = new Color(0.69f, 0.84f, 0.83f, 1f);  // soft teal backdrop — cards pop on it
        private static readonly Color PAGE       = new Color(0.99f, 0.99f, 0.98f, 1f);  // bright white page card
        private static readonly Color TITLEBAR   = new Color(0.11f, 0.55f, 0.53f, 1f);  // vibrant teal spine
        private static readonly Color SECTION    = new Color(0.99f, 0.99f, 0.98f, 1f);  // bright white panel
        private static readonly Color CELL_EMPTY = new Color(0.80f, 0.85f, 0.88f, 1f);  // visible empty calendar day
        private static readonly Color LEATHER_LT = new Color(0.97f, 0.45f, 0.38f, 1f);  // coral action button
        private static readonly Color GOLD       = new Color(0.96f, 0.66f, 0.10f);      // bright gold accent
        private static readonly Color CREAM      = new Color(0.18f, 0.20f, 0.24f);      // charcoal body text
        private static readonly Color INK        = new Color(0.11f, 0.13f, 0.16f);      // near-black charcoal — titles
        private static readonly Color INK_SOFT   = new Color(0.20f, 0.23f, 0.28f);      // dark slate secondary

        private static GameObject _root;
        private static Transform  _listParent;   // VerticalLayoutGroup content
        private static Transform  _card;          // the popup card (host for cipher screen / content)
        private static bool       _unlocked;      // true once the cipher's been entered this session
        private static bool       _resetArmed;    // "forgot seal" — 2-tap confirm guard
        private static readonly int[] _dialPos = new int[3];   // current sigil index per dial

        // ── Cipher lock ──────────────────────────────────────────────────
        // An ornate combination lock on the tome: 3 dials of runic sigils.
        // First open ever → SET the seal; every open after → DIAL it in.
        private const string CIPHER_KEY = "sparq.journal.cipher";   // PlayerPrefs: "i,j,k"
        // Locking is OPTIONAL — this records the player's choice.
        // ""  = never asked (first open → choice screen)
        // "on"  = sealed, cipher required   "off" = open freely, no lock
        private const string LOCKMODE_KEY = "sparq.journal.lockmode";
        // 6 sigils — colour + roman-numeral glyph (font-safe, no tofu risk).
        private static readonly (Color color, string glyph)[] SIGILS = new[]
        {
            (new Color(0.55f, 0.85f, 1.00f), "I"),
            (new Color(1.00f, 0.85f, 0.35f), "II"),
            (new Color(0.65f, 0.55f, 0.85f), "III"),
            (new Color(0.85f, 0.40f, 0.45f), "IV"),
            (new Color(0.55f, 0.85f, 0.45f), "V"),
            (new Color(0.95f, 0.55f, 0.30f), "VI"),
        };

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_JournalPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            Stretch(rrt);
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop — tap to close
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card — a parchment page (rounded popup sprite, tinted cream)
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(980, 1620);
            var cardImg = card.GetComponent<Image>();
            var bgSp = LoadSprite(POPUP_BG);
            if (bgSp != null) { cardImg.sprite = bgSp; cardImg.type = Image.Type.Sliced; }
            cardImg.color = CARD_DARK;   // parchment

            // Ornate border — tinted leather-brown so it reads as a book edge.
            var border = NewGO("Border", card.transform, typeof(Image));
            Stretch(border.GetComponent<RectTransform>());
            var brImg = border.GetComponent<Image>();
            var brSp = LoadSprite(POPUP_BORDER);
            if (brSp != null) { brImg.sprite = brSp; brImg.type = Image.Type.Sliced; brImg.color = TITLEBAR; }
            else brImg.color = new Color(0.13f, 0.63f, 0.61f, 0.6f);
            brImg.raycastTarget = false;

            BuildTitleBar(card.transform);
            _card = card.transform;

            // Lock gate — locking is a CHOICE, not a requirement.
            //   first open ever → ask whether to seal it at all
            //   "off"           → open freely, no lock
            //   "on"            → cipher required
            if (_unlocked)
            {
                ShowContent();
            }
            else
            {
                string mode = PlayerPrefs.GetString(LOCKMODE_KEY, "");
                if (mode == "off")
                {
                    _unlocked = true;
                    ShowContent();
                }
                else if (mode == "on" && IsCipherSet())
                {
                    BuildCipherScreen(card.transform);
                }
                else
                {
                    BuildLockChoiceScreen(card.transform);
                }
            }

            Debug.Log("[JournalPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _listParent = null;
            _card = null;
            _unlocked = false;   // re-lock the tome — must dial the seal again next open
        }

        // Build the scrollable journal content into the card (called once the
        // cipher's been entered, or immediately if already unlocked).
        private static void ShowContent()
        {
            if (_card == null) return;
            var (scroll, content) = BuildScrollList(_card);
            _listParent = content;
            RebuildContent();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;   // start at top
        }

        // ─────────────────────────────────────────────────────────────────
        // CIPHER LOCK — an ornate combination dial on the tome
        // ─────────────────────────────────────────────────────────────────

        // First open ever — locking the journal is a CHOICE. Offer to seal
        // it with a rune cipher, or just open it freely. Either way the
        // choice is remembered and can be changed later from inside.
        private static void BuildLockChoiceScreen(Transform card)
        {
            var screen = NewGO("LockChoice", card, typeof(Image));
            var sRT = screen.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.offsetMin = new Vector2(40, 40); sRT.offsetMax = new Vector2(-40, -150);
            screen.GetComponent<Image>().color = new Color(0.13f, 0.63f, 0.61f, 0.06f);  // faint teal wash

            var book = NewGO("Book", screen.transform, typeof(Image));
            var bookRT = book.GetComponent<RectTransform>();
            bookRT.anchorMin = new Vector2(0.5f, 1); bookRT.anchorMax = new Vector2(0.5f, 1);
            bookRT.pivot = new Vector2(0.5f, 1);
            bookRT.anchoredPosition = new Vector2(0, -40);
            bookRT.sizeDelta = new Vector2(280, 280);
            var bookImg = book.GetComponent<Image>();
            var bookSp = LoadSprite(BOOK_CLOSED);
            if (bookSp != null) { bookImg.sprite = bookSp; bookImg.preserveAspect = true; }
            else bookImg.color = TITLEBAR;
            bookImg.raycastTarget = false;

            var info = MakeText(screen.transform, "Info",
                "Would you like to lock your journal?\nA secret rune seal keeps your entries private — you can change this anytime.",
                32, FontStyles.Bold, CREAM);
            var iRT = info.rectTransform;
            iRT.anchorMin = new Vector2(0, 1); iRT.anchorMax = new Vector2(1, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.offsetMin = new Vector2(24, -560); iRT.offsetMax = new Vector2(-24, -340);
            info.alignment = TextAlignmentOptions.Center;
            info.textWrappingMode = TextWrappingModes.Normal;

            // "Lock it with a seal" → go to the cipher setup screen.
            var sealBtn = NewGO("SealBtn", screen.transform, typeof(Image), typeof(Button));
            var sbRT = sealBtn.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.5f, 0.5f); sbRT.anchorMax = new Vector2(0.5f, 0.5f);
            sbRT.pivot = new Vector2(0.5f, 0.5f);
            sbRT.anchoredPosition = new Vector2(0, -10);
            sbRT.sizeDelta = new Vector2(560, 124);
            var sbImg = sealBtn.GetComponent<Image>();
            sbImg.color = GOLD; sbImg.raycastTarget = true;
            var sbBtn = sealBtn.GetComponent<Button>();
            sbBtn.targetGraphic = sbImg; sbBtn.interactable = true;
            var sbl = MakeText(sealBtn.transform, "L", "Lock it with a rune seal",
                34, FontStyles.Bold, INK);
            Stretch(sbl.rectTransform); sbl.alignment = TextAlignmentOptions.Center;
            sbBtn.onClick.AddListener(() => {
                Debug.Log("[JournalPanel] Lock choice: seal it.");
                UnityEngine.Object.Destroy(screen);
                if (_card != null) BuildCipherScreen(_card);
            });

            // "No lock, just open" → open freely, remember the choice.
            var openBtn = NewGO("OpenBtn", screen.transform, typeof(Image), typeof(Button));
            var obRT = openBtn.GetComponent<RectTransform>();
            obRT.anchorMin = new Vector2(0.5f, 0.5f); obRT.anchorMax = new Vector2(0.5f, 0.5f);
            obRT.pivot = new Vector2(0.5f, 0.5f);
            obRT.anchoredPosition = new Vector2(0, -164);
            obRT.sizeDelta = new Vector2(560, 124);
            var obImg = openBtn.GetComponent<Image>();
            obImg.color = LEATHER_LT; obImg.raycastTarget = true;
            var obBtn = openBtn.GetComponent<Button>();
            obBtn.targetGraphic = obImg; obBtn.interactable = true;
            var obl = MakeText(openBtn.transform, "L", "No lock — just open it",
                34, FontStyles.Bold, Color.white);
            Stretch(obl.rectTransform); obl.alignment = TextAlignmentOptions.Center;
            obBtn.onClick.AddListener(() => {
                Debug.Log("[JournalPanel] Lock choice: no lock.");
                PlayerPrefs.SetString(LOCKMODE_KEY, "off");
                PlayerPrefs.Save();
                _unlocked = true;
                UnityEngine.Object.Destroy(screen);
                try { ShowContent(); }
                catch (System.Exception ex)
                { Debug.LogError($"[JournalPanel] ShowContent failed after no-lock: {ex}"); }
            });
        }

        private static void BuildCipherScreen(Transform card)
        {
            bool setup = !IsCipherSet();
            _dialPos[0] = _dialPos[1] = _dialPos[2] = 0;   // reset dials each open
            _resetArmed = false;                            // disarm the "forgot" guard
            Debug.Log($"[JournalPanel] BuildCipherScreen: setup={setup} (cipherSet={IsCipherSet()})");

            var screen = NewGO("CipherScreen", card, typeof(Image));
            var sRT = screen.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.offsetMin = new Vector2(40, 40); sRT.offsetMax = new Vector2(-40, -150);
            screen.GetComponent<Image>().color = new Color(0.13f, 0.63f, 0.61f, 0.06f);  // faint teal wash  // faint leather wash

            // The sealed tome — a closed leather book with a gold buckle.
            var book = NewGO("Book", screen.transform, typeof(Image));
            var bookRT = book.GetComponent<RectTransform>();
            bookRT.anchorMin = new Vector2(0.5f, 1); bookRT.anchorMax = new Vector2(0.5f, 1);
            bookRT.pivot = new Vector2(0.5f, 1);
            bookRT.anchoredPosition = new Vector2(0, -24);
            bookRT.sizeDelta = new Vector2(264, 264);
            var bookImg = book.GetComponent<Image>();
            var bookSp = LoadSprite(BOOK_CLOSED);
            if (bookSp != null) { bookImg.sprite = bookSp; bookImg.preserveAspect = true; }
            else bookImg.color = TITLEBAR;
            bookImg.raycastTarget = false;

            var info = MakeText(screen.transform, "Info",
                setup
                  ? "Create your secret seal\nTap each rune to spin it — pick three you'll remember."
                  : "Enter your secret seal\nSpin the three runes to match the ones you chose.",
                32, FontStyles.Bold, CREAM);
            var iRT = info.rectTransform;
            iRT.anchorMin = new Vector2(0, 1); iRT.anchorMax = new Vector2(1, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.offsetMin = new Vector2(24, -430); iRT.offsetMax = new Vector2(-24, -296);
            info.alignment = TextAlignmentOptions.Center;
            info.textWrappingMode = TextWrappingModes.Normal;

            // 3 dials in a row (HorizontalLayoutGroup → even spacing)
            var dialRow = NewGO("DialRow", screen.transform, typeof(HorizontalLayoutGroup));
            var drRT = dialRow.GetComponent<RectTransform>();
            drRT.anchorMin = new Vector2(0.5f, 0.5f); drRT.anchorMax = new Vector2(0.5f, 0.5f);
            drRT.pivot = new Vector2(0.5f, 0.5f);
            drRT.anchoredPosition = new Vector2(0, 10);
            drRT.sizeDelta = new Vector2(760, 320);
            var drHlg = dialRow.GetComponent<HorizontalLayoutGroup>();
            drHlg.spacing = 30;
            drHlg.childAlignment = TextAnchor.MiddleCenter;
            drHlg.childForceExpandWidth = true; drHlg.childForceExpandHeight = true;
            drHlg.childControlWidth = true; drHlg.childControlHeight = true;

            var faces  = new Image[3];
            var glyphs = new TMP_Text[3];
            for (int d = 0; d < 3; d++) BuildDial(dialRow.transform, d, faces, glyphs);

            var btn = NewGO("Confirm", screen.transform, typeof(Image), typeof(Button));
            var bRT = btn.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 0); bRT.anchorMax = new Vector2(0.5f, 0);
            bRT.pivot = new Vector2(0.5f, 0);
            bRT.anchoredPosition = new Vector2(0, 44);
            bRT.sizeDelta = new Vector2(440, 116);
            var btnImg = btn.GetComponent<Image>();
            btnImg.color = GOLD;
            btnImg.raycastTarget = true;
            var btnComp = btn.GetComponent<Button>();
            btnComp.targetGraphic = btnImg;
            btnComp.interactable = true;
            var bl = MakeText(btn.transform, "L", setup ? "SEAL JOURNAL" : "UNLOCK",
                36, FontStyles.Bold, INK);
            Stretch(bl.rectTransform); bl.alignment = TextAlignmentOptions.Center;
            btnComp.onClick.AddListener(() => {
                Debug.Log("[JournalPanel] Confirm/Seal button tapped.");
                OnCipherConfirm(setup, screen, info);
            });

            // Enter mode only — an escape hatch so you can't be locked out of
            // your own journal forever. A real, visible button (the old subtle
            // italic line wasn't noticeable). Two taps to confirm — no wipe by
            // accident.
            if (!setup)
            {
                var forgot = NewGO("Forgot", screen.transform, typeof(Image), typeof(Button));
                var fRT = forgot.GetComponent<RectTransform>();
                fRT.anchorMin = new Vector2(0.5f, 0); fRT.anchorMax = new Vector2(0.5f, 0);
                fRT.pivot = new Vector2(0.5f, 0);
                fRT.anchoredPosition = new Vector2(0, 184);
                fRT.sizeDelta = new Vector2(400, 80);
                var fImg = forgot.GetComponent<Image>();
                fImg.color = LEATHER_LT;
                fImg.raycastTarget = true;
                var fBtn = forgot.GetComponent<Button>();
                fBtn.targetGraphic = fImg;
                fBtn.interactable = true;
                var fl = MakeText(forgot.transform, "L", "Forgot your seal?",
                    27, FontStyles.Bold, Color.white);
                Stretch(fl.rectTransform); fl.alignment = TextAlignmentOptions.Center;
                fBtn.onClick.AddListener(() => {
                    Debug.Log("[JournalPanel] Forgot-seal button tapped.");
                    OnForgotSeal(screen, info, fl);
                });
            }
        }

        // "Forgot your seal?" — 2-tap: arm, then clear the seal and restart in
        // setup mode. Entries are NEVER touched — only the cipher is cleared.
        private static void OnForgotSeal(GameObject screen, TMP_Text info, TMP_Text forgotLabel)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            if (!_resetArmed)
            {
                _resetArmed = true;
                if (info != null)
                {
                    info.text  = "This clears your seal so you can set a new one.\nYour entries are kept.";
                    info.color = new Color(0.95f, 0.55f, 0.30f);
                }
                if (forgotLabel != null)
                {
                    forgotLabel.text      = "Tap again to clear your seal";
                    forgotLabel.color     = new Color(0.95f, 0.45f, 0.45f);
                    forgotLabel.fontStyle = FontStyles.Bold;
                }
                return;
            }

            PlayerPrefs.DeleteKey(CIPHER_KEY);
            PlayerPrefs.Save();
            _resetArmed = false;
            UnityEngine.Object.Destroy(screen);
            if (_card != null) BuildCipherScreen(_card);   // rebuilds in setup mode
            Debug.Log("[JournalPanel] Cipher seal reset — entries kept.");
        }

        // One dial: an ordinal cap + a tappable sigil disc + a hint.
        // The disc IS the button — tap it to spin to the next sigil. One
        // gesture, like turning a real combination lock (no fiddly +/-).
        private static void BuildDial(Transform row, int dialIndex, Image[] faces, TMP_Text[] glyphs)
        {
            var dial = NewGO("Dial" + dialIndex, row, typeof(Image));
            dial.GetComponent<Image>().color = new Color(0, 0, 0, 0);   // invisible layout cell

            // Ordinal cap — "1st" / "2nd" / "3rd" so the order is unmistakable.
            string[] ord = { "1st", "2nd", "3rd" };
            var cap = MakeText(dial.transform, "Cap",
                ord[Mathf.Clamp(dialIndex, 0, 2)], 28, FontStyles.Bold, INK_SOFT);
            var capRT = cap.rectTransform;
            capRT.anchorMin = new Vector2(0.5f, 1); capRT.anchorMax = new Vector2(0.5f, 1);
            capRT.pivot = new Vector2(0.5f, 1);
            capRT.anchoredPosition = new Vector2(0, 0);
            capRT.sizeDelta = new Vector2(160, 40);
            cap.alignment = TextAlignmentOptions.Center;

            // The sigil disc — IS the button. Tap to spin to the next sigil.
            var disc = NewGO("Disc", dial.transform, typeof(Image), typeof(Button));
            var dscRT = disc.GetComponent<RectTransform>();
            dscRT.anchorMin = new Vector2(0.5f, 0.5f); dscRT.anchorMax = new Vector2(0.5f, 0.5f);
            dscRT.pivot = new Vector2(0.5f, 0.5f);
            dscRT.anchoredPosition = new Vector2(0, -8);
            dscRT.sizeDelta = new Vector2(196, 196);
            var discImg = disc.GetComponent<Image>();
            var circ = LoadSprite(CIRCLE_BG);
            if (circ != null) discImg.sprite = circ;
            discImg.raycastTarget = true;
            var discBtn = disc.GetComponent<Button>();
            discBtn.targetGraphic = discImg;
            discBtn.interactable = true;
            faces[dialIndex] = discImg;
            var glyph = MakeText(disc.transform, "G", "", 88, FontStyles.Bold, INK);
            Stretch(glyph.rectTransform); glyph.alignment = TextAlignmentOptions.Center;
            glyphs[dialIndex] = glyph;
            int di = dialIndex;
            discBtn.onClick.AddListener(() => RotateDial(di, +1, faces, glyphs));

            // "tap to spin" hint under the disc.
            var hint = MakeText(dial.transform, "Hint", "tap to spin", 24, FontStyles.Italic, INK_SOFT);
            var hRT = hint.rectTransform;
            hRT.anchorMin = new Vector2(0.5f, 0); hRT.anchorMax = new Vector2(0.5f, 0);
            hRT.pivot = new Vector2(0.5f, 0);
            hRT.anchoredPosition = new Vector2(0, 4);
            hRT.sizeDelta = new Vector2(180, 34);
            hint.alignment = TextAlignmentOptions.Center;

            ApplyDialFace(dialIndex, faces, glyphs);
        }

        private static void RotateDial(int dialIndex, int dir, Image[] faces, TMP_Text[] glyphs)
        {
            _dialPos[dialIndex] = (_dialPos[dialIndex] + dir + SIGILS.Length) % SIGILS.Length;
            ApplyDialFace(dialIndex, faces, glyphs);
            Debug.Log($"[JournalPanel] Dial {dialIndex} → sigil {_dialPos[dialIndex]} ('{SIGILS[_dialPos[dialIndex]].glyph}')");
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
        }

        private static void ApplyDialFace(int dialIndex, Image[] faces, TMP_Text[] glyphs)
        {
            var sig = SIGILS[_dialPos[dialIndex]];
            if (faces[dialIndex]  != null) faces[dialIndex].color = sig.color;
            if (glyphs[dialIndex] != null) glyphs[dialIndex].text = sig.glyph;
        }

        private static void OnCipherConfirm(bool setup, GameObject screen, TMP_Text info)
        {
            Debug.Log($"[JournalPanel] OnCipherConfirm: setup={setup}, dialed=[{_dialPos[0]},{_dialPos[1]},{_dialPos[2]}]");
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            if (setup)
            {
                PlayerPrefs.SetString(CIPHER_KEY, $"{_dialPos[0]},{_dialPos[1]},{_dialPos[2]}");
                PlayerPrefs.SetString(LOCKMODE_KEY, "on");   // sealing turns the lock on
                PlayerPrefs.Save();
                _unlocked = true;
                if (screen != null) UnityEngine.Object.Destroy(screen);
                try { ShowContent(); }
                catch (System.Exception ex)
                { Debug.LogError($"[JournalPanel] ShowContent failed after seal: {ex}"); }
                Debug.Log("[JournalPanel] Cipher seal set — tome unlocked.");
                return;
            }

            int[] saved = LoadCipher();
            bool match = saved != null
                && saved[0] == _dialPos[0] && saved[1] == _dialPos[1] && saved[2] == _dialPos[2];
            Debug.Log($"[JournalPanel] Cipher check: saved=[{(saved != null ? string.Join(",", saved) : "null")}], match={match}");
            if (match)
            {
                _unlocked = true;
                if (screen != null) UnityEngine.Object.Destroy(screen);
                try { ShowContent(); }
                catch (System.Exception ex)
                { Debug.LogError($"[JournalPanel] ShowContent failed after unlock: {ex}"); }
                Debug.Log("[JournalPanel] Cipher accepted — tome unlocked.");
            }
            else
            {
                if (info != null)
                {
                    info.text  = "The sigils do not align.\nTry again.";
                    info.color = new Color(0.78f, 0.30f, 0.18f);   // deep red, reads on parchment
                }
                Debug.Log("[JournalPanel] Cipher rejected.");
            }
        }

        private static bool IsCipherSet()
            => !string.IsNullOrEmpty(PlayerPrefs.GetString(CIPHER_KEY, ""));

        private static int[] LoadCipher()
        {
            string raw = PlayerPrefs.GetString(CIPHER_KEY, "");
            if (string.IsNullOrEmpty(raw)) return null;
            var parts = raw.Split(',');
            if (parts.Length != 3) return null;
            var r = new int[3];
            for (int i = 0; i < 3; i++)
                if (!int.TryParse(parts[i], out r[i])) return null;
            return r;
        }

        // Reward for logging today's mood — coins + XP, with a streak bonus.
        private static void GrantMoodReward()
        {
            int streak = 1;
            try { streak = Mathf.Max(1, Sparq.Systems.MoodService.StreakDays()); } catch {}
            int coins = 20 + streak * 5;
            int xp = 15;
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null)
                {
                    d.sparqCoins += coins;
                    // Route through Progression so currentXP / xpToNextLevel /
                    // level advance and any level-up resolves. Direct totalXP
                    // writes here would leave the player's *level* frozen.
                    Sparq.Systems.Progression.GrantXp(d, xp);
                    Sparq.Core.SaveService.Save();
                }
            }
            catch (System.Exception ex)
            { Debug.LogError($"[JournalPanel] Reward grant failed: {ex.Message}"); }

            try
            {
                if (_root != null)
                    XPFloater.Spawn(_root.transform,
                        new Vector3(Screen.width / 2f, Screen.height * 0.66f, 0),
                        $"+{coins} coins   +{xp} XP", new Color(1f, 0.85f, 0.35f));
            }
            catch {}
            Debug.Log($"[JournalPanel] Mood logged — granted {coins} coins, {xp} XP (streak {streak}).");
        }

        // ─────────────────────────────────────────────────────────────────
        // CONTENT
        // ─────────────────────────────────────────────────────────────────

        private static void RebuildContent()
        {
            if (_listParent == null) return;
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);

            BuildMoodLogger(_listParent);
            BuildStreakBanner(_listParent);
            BuildNewEntryButton(_listParent);
            BuildJournalEntries(_listParent);
            BuildCalendar(_listParent);
            BuildRecentEntries(_listParent);
            BuildLockSettings(_listParent);
        }

        // ─────────────────────────────────────────────────────────────────
        // REAL JOURNAL ENTRIES — write / list / read text entries
        // (storage lives in Sparq.Systems.JournalService)
        // ─────────────────────────────────────────────────────────────────

        // A prominent "Write a journal entry" call-to-action.
        private static void BuildNewEntryButton(Transform parent)
        {
            var card = Section(parent, 132);
            card.GetComponent<Image>().color = SECTION;

            var btn = NewGO("WriteBtn", card.transform, typeof(Image), typeof(Button));
            var bRT = btn.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
            bRT.offsetMin = new Vector2(24, 22); bRT.offsetMax = new Vector2(-24, -22);
            var bImg = btn.GetComponent<Image>();
            bImg.color = GOLD;
            bImg.raycastTarget = true;
            var bComp = btn.GetComponent<Button>();
            bComp.targetGraphic = bImg; bComp.interactable = true;
            var bl = MakeText(btn.transform, "L", "Write a journal entry", 32, FontStyles.Bold, INK);
            Stretch(bl.rectTransform); bl.alignment = TextAlignmentOptions.Center;
            bComp.onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                if (_card == null) return;
                var scroll = _card.Find("Scroll");
                if (scroll != null) UnityEngine.Object.Destroy(scroll.gameObject);
                _listParent = null;
                BuildWriteScreen(_card);
            });
        }

        // Full-card composer for a new journal entry.
        private static void BuildWriteScreen(Transform card)
        {
            var screen = NewGO("WriteScreen", card, typeof(Image));
            var sRT = screen.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.offsetMin = new Vector2(40, 40); sRT.offsetMax = new Vector2(-40, -150);
            screen.GetComponent<Image>().color = new Color(0.13f, 0.63f, 0.61f, 0.06f);

            var hdr = MakeText(screen.transform, "Hdr", "New Journal Entry", 58, FontStyles.Bold, INK);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(20, -102); hRT.offsetMax = new Vector2(-20, -16);
            hdr.alignment = TextAlignmentOptions.Center;

            var dateLbl = MakeText(screen.transform, "Date",
                DateTime.Now.ToString("dddd, MMM d  -  h:mm tt"), 40, FontStyles.Bold, CREAM);
            var dRT = dateLbl.rectTransform;
            dRT.anchorMin = new Vector2(0, 1); dRT.anchorMax = new Vector2(1, 1);
            dRT.pivot = new Vector2(0.5f, 1);
            dRT.offsetMin = new Vector2(20, -156); dRT.offsetMax = new Vector2(-20, -106);
            dateLbl.alignment = TextAlignmentOptions.Center;

            // The writing area — a multiline input field.
            var input = MakeInputField(screen.transform, "Entry", "Write whatever's on your mind...");
            var fRT = input.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0, 0); fRT.anchorMax = new Vector2(1, 1);
            fRT.offsetMin = new Vector2(20, 150); fRT.offsetMax = new Vector2(-20, -176);

            // Save
            var saveBtn = NewGO("Save", screen.transform, typeof(Image), typeof(Button));
            var svRT = saveBtn.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0.5f, 0); svRT.anchorMax = new Vector2(0.5f, 0);
            svRT.pivot = new Vector2(0.5f, 0);
            svRT.anchoredPosition = new Vector2(-130, 30);
            svRT.sizeDelta = new Vector2(280, 100);
            var svImg = saveBtn.GetComponent<Image>();
            svImg.color = GOLD; svImg.raycastTarget = true;
            var svBtn = saveBtn.GetComponent<Button>();
            svBtn.targetGraphic = svImg; svBtn.interactable = true;
            var svl = MakeText(saveBtn.transform, "L", "Save", 44, FontStyles.Bold, INK);
            Stretch(svl.rectTransform); svl.alignment = TextAlignmentOptions.Center;
            svBtn.onClick.AddListener(() => {
                string txt = input.text != null ? input.text.Trim() : "";
                if (string.IsNullOrEmpty(txt))
                {
                    hdr.text  = "Write something first";
                    hdr.color = LEATHER_LT;
                    return;
                }
                try { Sparq.Systems.JournalService.Add(txt); }
                catch (System.Exception ex)
                { Debug.LogError($"[JournalPanel] JournalService.Add failed: {ex.Message}"); }
                GrantEntryReward();
                UnityEngine.Object.Destroy(screen);
                ShowContent();
                Debug.Log("[JournalPanel] Journal entry saved.");
            });

            // Cancel
            var cancelBtn = NewGO("Cancel", screen.transform, typeof(Image), typeof(Button));
            var cnRT = cancelBtn.GetComponent<RectTransform>();
            cnRT.anchorMin = new Vector2(0.5f, 0); cnRT.anchorMax = new Vector2(0.5f, 0);
            cnRT.pivot = new Vector2(0.5f, 0);
            cnRT.anchoredPosition = new Vector2(130, 30);
            cnRT.sizeDelta = new Vector2(280, 100);
            var cnImg = cancelBtn.GetComponent<Image>();
            cnImg.color = LEATHER_LT; cnImg.raycastTarget = true;
            var cnBtn = cancelBtn.GetComponent<Button>();
            cnBtn.targetGraphic = cnImg; cnBtn.interactable = true;
            var cnl = MakeText(cancelBtn.transform, "L", "Cancel", 44, FontStyles.Bold, Color.white);
            Stretch(cnl.rectTransform); cnl.alignment = TextAlignmentOptions.Center;
            cnBtn.onClick.AddListener(() => {
                UnityEngine.Object.Destroy(screen);
                ShowContent();
            });
        }

        // ── Section: saved journal entries (text), newest first ──
        private static void BuildJournalEntries(Transform parent)
        {
            List<Sparq.Systems.JournalService.Entry> entries;
            try { entries = Sparq.Systems.JournalService.All(); }
            catch { entries = new List<Sparq.Systems.JournalService.Entry>(); }

            var hdrCard = Section(parent, 54);
            hdrCard.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var hdr = MakeText(hdrCard.transform, "Hdr", "Journal Entries", 36, FontStyles.Bold, INK);
            var hrRT = hdr.rectTransform;
            hrRT.anchorMin = new Vector2(0, 0); hrRT.anchorMax = new Vector2(1, 1);
            hrRT.offsetMin = new Vector2(30, 0); hrRT.offsetMax = new Vector2(-30, 0);
            hdr.alignment = TextAlignmentOptions.MidlineLeft;

            if (entries == null || entries.Count == 0)
            {
                var empty = Section(parent, 90);
                var et = MakeText(empty.transform, "E",
                    "No entries yet — tap \"Write a journal entry\" above.",
                    28, FontStyles.Normal, CREAM);
                Stretch(et.rectTransform);
                et.alignment = TextAlignmentOptions.Center;
                return;
            }

            int shown = 0;
            foreach (var e in entries)   // already newest-first
            {
                if (shown >= 10) break;
                shown++;

                var row = Section(parent, 210);
                var rowImg = row.GetComponent<Image>();
                rowImg.color = PAGE;               // bright white page card
                rowImg.raycastTarget = true;
                var open = row.AddComponent<Button>();
                open.targetGraphic = rowImg;

                var date = DateTimeOffset.FromUnixTimeSeconds(e.unix).LocalDateTime;
                var dateLbl = MakeText(row.transform, "D",
                    date.ToString("dddd, MMM d  -  h:mm tt"), 30, FontStyles.Bold, INK);
                var dRT = dateLbl.rectTransform;
                dRT.anchorMin = new Vector2(0, 1); dRT.anchorMax = new Vector2(1, 1);
                dRT.pivot = new Vector2(0.5f, 1);
                dRT.offsetMin = new Vector2(22, -60); dRT.offsetMax = new Vector2(-86, -10);
                dateLbl.alignment = TextAlignmentOptions.MidlineLeft;

                var body = MakeText(row.transform, "B", e.text ?? "", 32, FontStyles.Normal, CREAM);
                var bRT = body.rectTransform;
                bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
                bRT.offsetMin = new Vector2(22, 16); bRT.offsetMax = new Vector2(-22, -66);
                body.alignment = TextAlignmentOptions.TopLeft;
                body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Ellipsis;

                var capturedId = e.id;
                open.onClick.AddListener(() => {
                    var entry = FindEntry(capturedId);
                    if (entry == null || _card == null) return;
                    var scroll = _card.Find("Scroll");
                    if (scroll != null) UnityEngine.Object.Destroy(scroll.gameObject);
                    _listParent = null;
                    BuildReadScreen(_card, entry);
                });

                // delete (small red X, top-right)
                var del = NewGO("Del", row.transform, typeof(Image), typeof(Button));
                var xRT = del.GetComponent<RectTransform>();
                xRT.anchorMin = new Vector2(1, 1); xRT.anchorMax = new Vector2(1, 1);
                xRT.pivot = new Vector2(1, 1);
                xRT.anchoredPosition = new Vector2(-12, -10);
                xRT.sizeDelta = new Vector2(54, 54);
                var xImg = del.GetComponent<Image>();
                xImg.color = new Color(0.82f, 0.26f, 0.26f, 1f);
                xImg.raycastTarget = true;
                var xBtn = del.GetComponent<Button>();
                xBtn.targetGraphic = xImg; xBtn.interactable = true;
                var xl = MakeText(del.transform, "X", "X", 28, FontStyles.Bold, Color.white);
                Stretch(xl.rectTransform); xl.alignment = TextAlignmentOptions.Center;
                var delId = e.id;
                xBtn.onClick.AddListener(() => {
                    try { Sparq.Systems.JournalService.Delete(delId); } catch {}
                    RebuildContent();
                });
            }
        }

        // Full-card read view for one saved entry — scrollable body.
        private static void BuildReadScreen(Transform card, Sparq.Systems.JournalService.Entry entry)
        {
            var screen = NewGO("ReadScreen", card, typeof(Image));
            var sRT = screen.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.offsetMin = new Vector2(40, 40); sRT.offsetMax = new Vector2(-40, -150);
            screen.GetComponent<Image>().color = PAGE;

            var date = DateTimeOffset.FromUnixTimeSeconds(entry.unix).LocalDateTime;
            var hdr = MakeText(screen.transform, "Hdr",
                date.ToString("dddd, MMM d, yyyy"), 54, FontStyles.Bold, INK);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(24, -102); hRT.offsetMax = new Vector2(-24, -16);
            hdr.alignment = TextAlignmentOptions.Center;

            var timeLbl = MakeText(screen.transform, "Time",
                date.ToString("h:mm tt"), 40, FontStyles.Bold, CREAM);
            var tRT = timeLbl.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(24, -156); tRT.offsetMax = new Vector2(-24, -106);
            timeLbl.alignment = TextAlignmentOptions.Center;

            // Scrollable body.
            var scrollGO = NewGO("BodyScroll", screen.transform, typeof(Image), typeof(ScrollRect));
            var scRT = scrollGO.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0, 0); scRT.anchorMax = new Vector2(1, 1);
            scRT.offsetMin = new Vector2(24, 132); scRT.offsetMax = new Vector2(-24, -170);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sc = scrollGO.GetComponent<ScrollRect>();
            sc.horizontal = false; sc.vertical = true; sc.scrollSensitivity = 30f;

            var vp = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            var vpRT = vp.GetComponent<RectTransform>();
            Stretch(vpRT);
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("C", vp.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0, cRT.sizeDelta.y);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 4, 4);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var body = MakeText(content.transform, "Body", entry.text ?? "", 40, FontStyles.Normal, CREAM);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;

            sc.viewport = vpRT; sc.content = cRT;
            Canvas.ForceUpdateCanvases();
            sc.verticalNormalizedPosition = 1f;

            // Back
            var back = NewGO("Back", screen.transform, typeof(Image), typeof(Button));
            var bkRT = back.GetComponent<RectTransform>();
            bkRT.anchorMin = new Vector2(0.5f, 0); bkRT.anchorMax = new Vector2(0.5f, 0);
            bkRT.pivot = new Vector2(0.5f, 0);
            bkRT.anchoredPosition = new Vector2(-130, 28);
            bkRT.sizeDelta = new Vector2(280, 92);
            var bkImg = back.GetComponent<Image>();
            bkImg.color = GOLD; bkImg.raycastTarget = true;
            var bkBtn = back.GetComponent<Button>();
            bkBtn.targetGraphic = bkImg; bkBtn.interactable = true;
            var bkl = MakeText(back.transform, "L", "Back", 44, FontStyles.Bold, INK);
            Stretch(bkl.rectTransform); bkl.alignment = TextAlignmentOptions.Center;
            bkBtn.onClick.AddListener(() => { UnityEngine.Object.Destroy(screen); ShowContent(); });

            // Delete
            var del = NewGO("Delete", screen.transform, typeof(Image), typeof(Button));
            var dlRT = del.GetComponent<RectTransform>();
            dlRT.anchorMin = new Vector2(0.5f, 0); dlRT.anchorMax = new Vector2(0.5f, 0);
            dlRT.pivot = new Vector2(0.5f, 0);
            dlRT.anchoredPosition = new Vector2(130, 28);
            dlRT.sizeDelta = new Vector2(280, 92);
            var dlImg = del.GetComponent<Image>();
            dlImg.color = new Color(0.82f, 0.26f, 0.26f, 1f); dlImg.raycastTarget = true;
            var dlBtn = del.GetComponent<Button>();
            dlBtn.targetGraphic = dlImg; dlBtn.interactable = true;
            var dll = MakeText(del.transform, "L", "Delete", 44, FontStyles.Bold, Color.white);
            Stretch(dll.rectTransform); dll.alignment = TextAlignmentOptions.Center;
            var rid = entry.id;
            dlBtn.onClick.AddListener(() => {
                try { Sparq.Systems.JournalService.Delete(rid); } catch {}
                UnityEngine.Object.Destroy(screen);
                ShowContent();
            });
        }

        private static Sparq.Systems.JournalService.Entry FindEntry(string id)
        {
            try
            {
                foreach (var e in Sparq.Systems.JournalService.All())
                    if (e.id == id) return e;
            }
            catch {}
            return null;
        }

        // A multiline TMP_InputField skinned to the bright theme. Children are
        // created before the input component is added, so its OnEnable sees a
        // fully-formed text area.
        private static TMP_InputField MakeInputField(Transform parent, string name, string placeholder)
        {
            var go = NewGO(name, parent, typeof(Image));
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.95f);

            var area = NewGO("TextArea", go.transform, typeof(RectMask2D));
            var aRT = area.GetComponent<RectTransform>();
            Stretch(aRT);
            aRT.offsetMin = new Vector2(18, 14); aRT.offsetMax = new Vector2(-18, -14);

            var ph = MakeText(area.transform, "Placeholder", placeholder, 40, FontStyles.Italic, INK_SOFT);
            Stretch(ph.rectTransform);
            ph.alignment = TextAlignmentOptions.TopLeft;
            ph.textWrappingMode = TextWrappingModes.Normal;

            var txt = MakeText(area.transform, "Text", "", 40, FontStyles.Normal, INK);
            Stretch(txt.rectTransform);
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.textWrappingMode = TextWrappingModes.Normal;

            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = img;
            input.textViewport = aRT;
            input.textComponent = txt;
            input.placeholder = ph;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.lineLimit = 0;
            input.characterLimit = 2000;
            input.text = "";
            return input;
        }

        // Reward for writing a journal entry — coins + XP.
        private static void GrantEntryReward()
        {
            int coins = 25, xp = 20;
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null)
                {
                    d.sparqCoins += coins;
                    // Same reason as GrantMoodReward — go through Progression.
                    Sparq.Systems.Progression.GrantXp(d, xp);
                    Sparq.Core.SaveService.Save();
                }
            }
            catch (System.Exception ex)
            { Debug.LogError($"[JournalPanel] Entry reward failed: {ex.Message}"); }

            try
            {
                if (_root != null)
                    XPFloater.Spawn(_root.transform,
                        new Vector3(Screen.width / 2f, Screen.height * 0.66f, 0),
                        $"+{coins} coins   +{xp} XP", new Color(1f, 0.85f, 0.35f));
            }
            catch {}
            Debug.Log($"[JournalPanel] Journal entry — granted {coins} coins, {xp} XP.");
        }

        // ── Section 5: lock settings — locking is optional, toggle anytime ──
        private static void BuildLockSettings(Transform parent)
        {
            bool locked = PlayerPrefs.GetString(LOCKMODE_KEY, "") == "on" && IsCipherSet();

            var card = Section(parent, 160);
            var hdr = MakeText(card.transform, "Hdr",
                locked ? "Your journal is sealed with a rune lock"
                       : "Your journal is open — no lock",
                26, FontStyles.Bold, GOLD);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(24, -58); hRT.offsetMax = new Vector2(-24, -14);
            hdr.alignment = TextAlignmentOptions.Center;

            var btn = NewGO("LockToggle", card.transform, typeof(Image), typeof(Button));
            var bRT = btn.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 0); bRT.anchorMax = new Vector2(0.5f, 0);
            bRT.pivot = new Vector2(0.5f, 0);
            bRT.anchoredPosition = new Vector2(0, 22);
            bRT.sizeDelta = new Vector2(520, 80);
            var bImg = btn.GetComponent<Image>();
            bImg.color = locked ? LEATHER_LT : GOLD;
            bImg.raycastTarget = true;
            var bComp = btn.GetComponent<Button>();
            bComp.targetGraphic = bImg; bComp.interactable = true;
            var bl = MakeText(btn.transform, "L",
                locked ? "Remove the lock" : "Add a rune lock",
                28, FontStyles.Bold, locked ? Color.white : INK);
            Stretch(bl.rectTransform); bl.alignment = TextAlignmentOptions.Center;
            bComp.onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                if (locked)
                {
                    PlayerPrefs.SetString(LOCKMODE_KEY, "off");
                    PlayerPrefs.DeleteKey(CIPHER_KEY);
                    PlayerPrefs.Save();
                    Debug.Log("[JournalPanel] Lock removed — entries kept.");
                    RebuildContent();
                }
                else
                {
                    // Add a lock — swap the content out for the seal setup screen.
                    Debug.Log("[JournalPanel] Adding a lock — opening seal setup.");
                    if (_card != null)
                    {
                        var scroll = _card.Find("Scroll");
                        if (scroll != null) UnityEngine.Object.Destroy(scroll.gameObject);
                        _listParent = null;
                        BuildCipherScreen(_card);
                    }
                }
            });
        }

        // ── Section 1: today's mood logger — 5 crystal buttons ──
        // Uses a HorizontalLayoutGroup so the 5 buttons auto-distribute across
        // the section width — no magic offsets that can drift off-card.
        private static void BuildMoodLogger(Transform parent)
        {
            bool loggedToday = false;
            try { loggedToday = Sparq.Systems.MoodService.LoggedToday(); } catch {}

            var card = Section(parent, 300);

            var hdr = MakeText(card.transform, "Hdr",
                loggedToday ? "Today's mood is logged — tap to change" : "How are you feeling today?",
                36, FontStyles.Bold, INK);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(28, -66); hRT.offsetMax = new Vector2(-28, -14);
            hdr.alignment = TextAlignmentOptions.Center;

            // Crystal row — HorizontalLayoutGroup distributes the buttons.
            var rowGO = NewGO("CrystalRow", card.transform, typeof(HorizontalLayoutGroup));
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1); rowRT.anchorMax = new Vector2(1, 1);
            rowRT.pivot = new Vector2(0.5f, 1);
            rowRT.offsetMin = new Vector2(20, -286); rowRT.offsetMax = new Vector2(-20, -78);
            var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            var crystals = GetCrystals();
            foreach (var (mood, label, color) in crystals)
            {
                var btn = NewGO("Crystal_" + label, rowGO.transform, typeof(Image), typeof(Button));
                btn.GetComponent<Image>().color = new Color(0, 0, 0, 0);   // transparent hit area

                var gem = NewGO("Gem", btn.transform, typeof(Image));
                var gRT = gem.GetComponent<RectTransform>();
                gRT.anchorMin = new Vector2(0.5f, 1); gRT.anchorMax = new Vector2(0.5f, 1);
                gRT.pivot = new Vector2(0.5f, 1);
                gRT.anchoredPosition = new Vector2(0, -6);
                gRT.sizeDelta = new Vector2(104, 104);
                var gemImg = gem.GetComponent<Image>();
                var circ = LoadSprite(CIRCLE_BG);
                if (circ != null) gemImg.sprite = circ;
                gemImg.color = color;
                gemImg.raycastTarget = false;

                var lbl = MakeText(btn.transform, "L", label, 24, FontStyles.Bold, INK);
                var lRT = lbl.rectTransform;
                lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 0);
                lRT.pivot = new Vector2(0.5f, 0);
                lRT.anchoredPosition = new Vector2(0, 8);
                lRT.sizeDelta = new Vector2(0, 36);
                lbl.alignment = TextAlignmentOptions.Center;
                // Force single line — "Triumphant" was wrapping to "Triumphan / t"
                // at 28pt because the 5-column row only gives each label ~180px.
                // 24pt + NoWrap fits cleanly.
                lbl.textWrappingMode = TextWrappingModes.NoWrap;
                lbl.overflowMode = TextOverflowModes.Truncate;

                var capturedMood = mood;
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    bool wasLoggedToday = false;
                    try { wasLoggedToday = Sparq.Systems.MoodService.LoggedToday(); } catch {}
                    try { Sparq.Systems.MoodService.Log(capturedMood); } catch (System.Exception ex)
                    { Debug.LogError($"[JournalPanel] MoodService.Log failed: {ex.Message}"); }
                    // First mood of the day → reward (coins + XP + streak bonus).
                    if (!wasLoggedToday) GrantMoodReward();
                    RebuildContent();
                });
            }
        }

        // ── Section 2: streak banner ──
        private static void BuildStreakBanner(Transform parent)
        {
            int streak = 0;
            try { streak = Sparq.Systems.MoodService.StreakDays(); } catch {}

            var card = Section(parent, 96);
            var t = MakeText(card.transform, "Streak",
                streak <= 0 ? "Log a mood to start a streak"
                            : (streak == 1 ? "1 day streak — keep it going!"
                                           : $"{streak} day streak — keep it going!"),
                32, FontStyles.Bold, streak > 0 ? GOLD : INK_SOFT);
            Stretch(t.rectTransform);
            t.alignment = TextAlignmentOptions.Center;
            // accent stripe when there's an active streak
            card.GetComponent<Image>().color = streak > 0
                ? new Color(1.00f, 0.88f, 0.55f, 1f)   // sunny highlight for an active streak
                : SECTION;
        }

        // ── Section 3: last-14-days mood calendar ──
        private static void BuildCalendar(Transform parent)
        {
            var byDay = new Dictionary<DateTime, int>();
            try
            {
                foreach (var e in Sparq.Systems.MoodService.LoadAll())
                {
                    var d = DateTimeOffset.FromUnixTimeSeconds(e.unix).UtcDateTime.Date;
                    byDay[d] = (int)e.mood;   // last entry of the day wins
                }
            }
            catch {}

            var card = Section(parent, 340);
            var hdr = MakeText(card.transform, "Hdr", "Last 14 Days", 36, FontStyles.Bold, INK);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(30, -54); hRT.offsetMax = new Vector2(-30, -12);
            hdr.alignment = TextAlignmentOptions.MidlineLeft;

            // 14 cells — a GridLayoutGroup arranges 2 rows of 7, centered,
            // so they always fit the section regardless of its measured width.
            var gridGO = NewGO("Grid", card.transform, typeof(GridLayoutGroup));
            var gridRT = gridGO.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0, 1); gridRT.anchorMax = new Vector2(1, 1);
            gridRT.pivot = new Vector2(0.5f, 1);
            gridRT.offsetMin = new Vector2(24, -322); gridRT.offsetMax = new Vector2(-24, -64);
            var grid = gridGO.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(104, 104);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            grid.childAlignment = TextAnchor.MiddleCenter;

            var crystals = GetCrystals();
            var today = DateTime.UtcNow.Date;
            for (int i = 0; i < 14; i++)
            {
                var day = today.AddDays(-(13 - i));
                var c = NewGO("Day", gridGO.transform, typeof(Image));
                var cImg = c.GetComponent<Image>();
                cImg.raycastTarget = false;
                if (byDay.TryGetValue(day, out int moodIdx) && moodIdx >= 0 && moodIdx < crystals.Length)
                    cImg.color = crystals[moodIdx].color;
                else
                    cImg.color = CELL_EMPTY;
                var num = MakeText(c.transform, "N", day.Day.ToString(), 26, FontStyles.Bold,
                    new Color(0.18f, 0.20f, 0.24f, 1f));
                Stretch(num.rectTransform);
                num.alignment = TextAlignmentOptions.Center;
            }
        }

        // ── Section 4: recent entries list ──
        private static void BuildRecentEntries(Transform parent)
        {
            List<Sparq.Systems.MoodService.Entry> all = null;
            try { all = Sparq.Systems.MoodService.LoadAll(); }
            catch { all = new List<Sparq.Systems.MoodService.Entry>(); }

            var crystals = GetCrystals();

            // header
            var hdrCard = Section(parent, 56);
            hdrCard.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var hdr = MakeText(hdrCard.transform, "Hdr", "Recent Entries", 36, FontStyles.Bold, INK);
            var hrRT = hdr.rectTransform;
            hrRT.anchorMin = new Vector2(0, 0); hrRT.anchorMax = new Vector2(1, 1);
            hrRT.offsetMin = new Vector2(30, 0); hrRT.offsetMax = new Vector2(-30, 0);
            hdr.alignment = TextAlignmentOptions.MidlineLeft;

            if (all == null || all.Count == 0)
            {
                var empty = Section(parent, 90);
                var et = MakeText(empty.transform, "E", "No entries yet — log your first mood above.",
                    28, FontStyles.Normal, CREAM);
                Stretch(et.rectTransform);
                et.alignment = TextAlignmentOptions.Center;
                return;
            }

            // newest first, cap at 12
            int shown = 0;
            for (int i = all.Count - 1; i >= 0 && shown < 12; i--, shown++)
            {
                var e = all[i];
                int mi = (int)e.mood;
                var crystal = (mi >= 0 && mi < crystals.Length) ? crystals[mi] : crystals[0];
                var date = DateTimeOffset.FromUnixTimeSeconds(e.unix).LocalDateTime;

                var row = Section(parent, 96);
                // color swatch
                var sw = NewGO("Sw", row.transform, typeof(Image));
                var swRT = sw.GetComponent<RectTransform>();
                swRT.anchorMin = new Vector2(0, 0.5f); swRT.anchorMax = new Vector2(0, 0.5f);
                swRT.pivot = new Vector2(0, 0.5f);
                swRT.anchoredPosition = new Vector2(22, 0);
                swRT.sizeDelta = new Vector2(56, 56);
                var swImg = sw.GetComponent<Image>();
                var circ = LoadSprite(CIRCLE_BG);
                if (circ != null) swImg.sprite = circ;
                swImg.color = crystal.color;
                swImg.raycastTarget = false;

                var moodLbl = MakeText(row.transform, "M", crystal.label, 30, FontStyles.Bold, CREAM);
                var mRT = moodLbl.rectTransform;
                mRT.anchorMin = new Vector2(0, 0); mRT.anchorMax = new Vector2(1, 1);
                mRT.offsetMin = new Vector2(100, 0); mRT.offsetMax = new Vector2(-260, 0);
                moodLbl.alignment = TextAlignmentOptions.MidlineLeft;

                var dateLbl = MakeText(row.transform, "D", date.ToString("MMM d  h:mm tt"),
                    28, FontStyles.Bold, CREAM);
                var dRT = dateLbl.rectTransform;
                dRT.anchorMin = new Vector2(1, 0); dRT.anchorMax = new Vector2(1, 1);
                dRT.pivot = new Vector2(1, 0.5f);
                dRT.anchoredPosition = new Vector2(-22, 0);
                dRT.sizeDelta = new Vector2(240, 0);
                dateLbl.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // SHELL HELPERS
        // ─────────────────────────────────────────────────────────────────

        private static void BuildTitleBar(Transform card)
        {
            var bar = NewGO("TitleBar", card, typeof(Image));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -14);
            rt.sizeDelta = new Vector2(-36, 120);
            bar.GetComponent<Image>().color = TITLEBAR;

            var title = MakeText(bar.transform, "Title", "JOURNAL", 56, FontStyles.Bold, GOLD);
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0.05f, 0.22f, 0.21f); } catch {}

            var close = NewGO("Close", bar.transform, typeof(Image), typeof(Button));
            var xrt = close.GetComponent<RectTransform>();
            xrt.anchorMin = new Vector2(1, 0.5f); xrt.anchorMax = new Vector2(1, 0.5f);
            xrt.pivot = new Vector2(1, 0.5f);
            xrt.anchoredPosition = new Vector2(-20, 0);
            xrt.sizeDelta = new Vector2(78, 78);
            close.GetComponent<Image>().color = new Color(0.82f, 0.26f, 0.26f, 1f);
            var xl = MakeText(close.transform, "X", "X", 44, FontStyles.Bold, Color.white);
            Stretch(xl.rectTransform); xl.alignment = TextAlignmentOptions.Center;
            close.GetComponent<Button>().onClick.AddListener(Hide);
        }

        // A full-width section card with a fixed preferred height.
        private static GameObject Section(Transform parent, float height)
        {
            var s = NewGO("Section", parent, typeof(Image), typeof(LayoutElement));
            s.GetComponent<LayoutElement>().preferredHeight = height;
            s.GetComponent<Image>().color = SECTION;
            return s;
        }

        // ScrollRect + VerticalLayoutGroup content, fills the card below the title bar.
        private static (ScrollRect, Transform) BuildScrollList(Transform card)
        {
            var scrollGO = NewGO("Scroll", card, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            srRT.offsetMin = new Vector2(28, 28); srRT.offsetMax = new Vector2(-28, -150);
            scrollGO.GetComponent<Image>().color = new Color(0.13f, 0.63f, 0.61f, 0.05f);  // faint teal inset
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var viewport = NewGO("Viewport", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            var vpRT = viewport.GetComponent<RectTransform>();
            Stretch(vpRT); vpRT.offsetMin = new Vector2(8, 8); vpRT.offsetMax = new Vector2(-8, -8);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", viewport.transform,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            ctRT.sizeDelta = new Vector2(0, ctRT.sizeDelta.y);   // no horizontal overhang past the mask
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRT; sr.content = ctRT;
            return (sr, content.transform);
        }

        // MoodService.Crystals → tuple array (defensive wrapper).
        private static (Sparq.Systems.MoodService.Mood mood, string label, Color color)[] GetCrystals()
        {
            try { return Sparq.Systems.MoodService.Crystals; }
            catch
            {
                return new[]
                {
                    (Sparq.Systems.MoodService.Mood.Calm, "Calm", new Color(0.55f, 0.85f, 1f)),
                };
            }
        }

        // ── Tiny UI helpers ──
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

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static Sprite LoadSprite(string assetPath) => Sparq.Core.SpriteLoader.Load(assetPath);

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
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
