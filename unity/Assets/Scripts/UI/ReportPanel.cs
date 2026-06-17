using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Report Player dialog. Opens when a user taps "Report" on a chat
    /// message (or from a player profile). Lets them pick a category,
    /// add optional notes, and choose to ALSO block the player.
    ///
    /// On Submit:
    ///   - Writes a Report to Sparq.Safety.ModerationQueue
    ///   - If "Also block" is checked → Sparq.Safety.BlockList.Block(player)
    ///   - Shows a confirmation toast and closes
    ///
    /// Built procedurally so it works regardless of Layer Lab dependencies.
    /// </summary>
    public static class ReportPanel
    {
        private static GameObject _root;
        private static Sparq.Safety.ModerationQueue.ReportReason _selected =
            Sparq.Safety.ModerationQueue.ReportReason.Harassment;
        private static System.Collections.Generic.List<(Image bg, TMP_Text lbl,
            Sparq.Safety.ModerationQueue.ReportReason r)> _categoryButtons;
        private static bool _alsoBlock = true;

        // Palette
        private static readonly Color GOLD    = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CARD_BG = new Color(0.16f, 0.10f, 0.28f, 1f);
        private static readonly Color ROW_BG  = new Color(0.10f, 0.06f, 0.18f, 0.85f);
        private static readonly Color SEL_BG  = new Color(0.85f, 0.30f, 0.30f, 1f);
        private static readonly Color UNSEL_BG= new Color(0.30f, 0.22f, 0.45f, 1f);

        /// <summary>Open the report dialog for a given player + message.</summary>
        public static void Show(string reportedPlayer, string offendingMessage)
        {
            if (_root != null) Object.Destroy(_root);
            _selected = Sparq.Safety.ModerationQueue.ReportReason.Harassment;
            _alsoBlock = true;
            _categoryButtons = new System.Collections.Generic.List<(Image, TMP_Text,
                Sparq.Safety.ModerationQueue.ReportReason)>();
            EnsureEventSystem();

            // Top-sort overlay canvas
            _root = new GameObject("Sparq_ReportPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 30;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop (tap to cancel)
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(940, 1420);
            card.GetComponent<Image>().color = CARD_BG;

            // Title bar — red
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero;
            tbRT.sizeDelta = new Vector2(0, 130);
            titleBar.GetComponent<Image>().color = new Color(0.75f, 0.25f, 0.25f, 1f);
            var titleTm = MakeText(titleBar.transform, "Title", "REPORT PLAYER",
                52, FontStyles.Bold, Color.white);
            var titleRT = titleTm.rectTransform;
            titleRT.anchorMin = Vector2.zero; titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(20, 0); titleRT.offsetMax = new Vector2(-120, 0);
            titleTm.alignment = TextAlignmentOptions.Center;

            // Close X (top-right)
            var closeBtn = NewGO("CloseBtn", card.transform, typeof(Image), typeof(Button));
            var cbRT = closeBtn.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1, 1); cbRT.anchorMax = new Vector2(1, 1);
            cbRT.pivot = new Vector2(1, 1);
            cbRT.anchoredPosition = new Vector2(-25, -25);
            cbRT.sizeDelta = new Vector2(85, 85);
            closeBtn.GetComponent<Image>().color = new Color(0.20f, 0.12f, 0.30f, 1f);
            var xLbl = MakeText(closeBtn.transform, "X", "X",
                48, FontStyles.Bold, Color.white);
            var xRT = xLbl.rectTransform;
            xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
            xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;
            xLbl.alignment = TextAlignmentOptions.Center;
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

            // ── Target player + offending message preview ────────────────
            float y = -180;

            BuildSectionLabel(card.transform, "REPORTING", y);
            y -= 60;

            var targetRow = NewGO("Target", card.transform, typeof(Image));
            var trRT = targetRow.GetComponent<RectTransform>();
            trRT.anchorMin = new Vector2(0, 1); trRT.anchorMax = new Vector2(1, 1);
            trRT.pivot = new Vector2(0.5f, 1);
            trRT.anchoredPosition = new Vector2(0, y);
            trRT.sizeDelta = new Vector2(-60, 90);
            targetRow.GetComponent<Image>().color = ROW_BG;
            var tName = MakeText(targetRow.transform, "Name",
                string.IsNullOrEmpty(reportedPlayer) ? "(unknown player)" : reportedPlayer,
                34, FontStyles.Bold, GOLD);
            var tnRT = tName.rectTransform;
            tnRT.anchorMin = Vector2.zero; tnRT.anchorMax = Vector2.one;
            tnRT.offsetMin = new Vector2(28, 0); tnRT.offsetMax = new Vector2(-28, 0);
            tName.alignment = TextAlignmentOptions.MidlineLeft;
            y -= 110;

            // Offending message preview
            if (!string.IsNullOrEmpty(offendingMessage))
            {
                var msgRow = NewGO("Msg", card.transform, typeof(Image));
                var mrRT = msgRow.GetComponent<RectTransform>();
                mrRT.anchorMin = new Vector2(0, 1); mrRT.anchorMax = new Vector2(1, 1);
                mrRT.pivot = new Vector2(0.5f, 1);
                mrRT.anchoredPosition = new Vector2(0, y);
                mrRT.sizeDelta = new Vector2(-60, 140);
                msgRow.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.12f, 0.95f);
                string preview = offendingMessage.Length > 220
                    ? offendingMessage.Substring(0, 220) + "…" : offendingMessage;
                var mTm = MakeText(msgRow.transform, "MsgTxt", "\"" + preview + "\"",
                    24, FontStyles.Italic, new Color(0.90f, 0.85f, 0.95f));
                var mRT = mTm.rectTransform;
                mRT.anchorMin = Vector2.zero; mRT.anchorMax = Vector2.one;
                mRT.offsetMin = new Vector2(20, 12); mRT.offsetMax = new Vector2(-20, -12);
                mTm.alignment = TextAlignmentOptions.TopLeft;
                mTm.textWrappingMode = TextWrappingModes.Normal;
                y -= 160;
            }

            // ── Categories ───────────────────────────────────────────────
            BuildSectionLabel(card.transform, "REASON (TAP ONE)", y);
            y -= 60;

            // 2 columns × 4 rows of category chips
            var allReasons = new (Sparq.Safety.ModerationQueue.ReportReason r, string label)[]
            {
                (Sparq.Safety.ModerationQueue.ReportReason.Harassment, "Harassment / Bullying"),
                (Sparq.Safety.ModerationQueue.ReportReason.Predator,   "Predatory / Grooming"),
                (Sparq.Safety.ModerationQueue.ReportReason.Scam,       "Scam / Phishing"),
                (Sparq.Safety.ModerationQueue.ReportReason.DrugSlang,  "Drug References"),
                (Sparq.Safety.ModerationQueue.ReportReason.SelfHarm,   "Self-Harm Concern"),
                (Sparq.Safety.ModerationQueue.ReportReason.Spam,       "Spam / Advertising"),
                (Sparq.Safety.ModerationQueue.ReportReason.Other,      "Other"),
            };

            float chipW = 410f, chipH = 90f, gap = 14f;
            for (int i = 0; i < allReasons.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                float xPos = (col == 0 ? -(chipW / 2f + gap / 2f) : (chipW / 2f + gap / 2f));
                float yPos = y - row * (chipH + gap);
                var (r, label) = allReasons[i];
                BuildCategoryChip(card.transform, xPos, yPos, chipW, chipH, label, r);
            }
            y -= ((allReasons.Length + 1) / 2) * (chipH + gap) + 20;

            // ── Notes field ──────────────────────────────────────────────
            BuildSectionLabel(card.transform, "NOTES (OPTIONAL)", y);
            y -= 60;

            var notes = BuildInputField(card.transform, y, "Anything else moderators should know?");
            y -= 170;

            // ── "Also block" toggle ──────────────────────────────────────
            BuildAlsoBlockRow(card.transform, y);
            y -= 120;

            // ── Submit + Cancel ──────────────────────────────────────────
            var cancelBtn = MakeButton(card.transform, -210, -100, 380, 110,
                "Cancel", new Color(0.35f, 0.25f, 0.50f), Color.white, Hide);
            cancelBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
            cancelBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            cancelBtn.GetComponent<RectTransform>().pivot     = new Vector2(0.5f, 0);
            cancelBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-210, 40);

            var submitBtn = MakeButton(card.transform, 210, -100, 380, 110,
                "Submit Report", new Color(0.85f, 0.25f, 0.25f), Color.white,
                () => OnSubmit(reportedPlayer, offendingMessage, notes));
            submitBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
            submitBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            submitBtn.GetComponent<RectTransform>().pivot     = new Vector2(0.5f, 0);
            submitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(210, 40);

            // Reflect default selection
            RefreshCategorySelection();

            Debug.Log($"[ReportPanel] Opened for target='{reportedPlayer}'.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // SUBMIT
        // ─────────────────────────────────────────────────────────────────

        private static void OnSubmit(string player, string msg, TMP_InputField notesInput)
        {
            string notes = notesInput == null ? "" : (notesInput.text ?? "");

            // Surface crisis/threat panels if the reporter is themselves in
            // distress or expressing threats — but DON'T block the report
            // (users need to be able to quote what they're reporting, even
            // if the quote contains threat language). Inspection is
            // additive, not gating.
            if (!string.IsNullOrWhiteSpace(notes))
            {
                var verdict = Sparq.Safety.ContentModerator.Inspect(notes, "report");
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.SelfHarmIdeation)
                    && !Sparq.UI.CrisisResourcesPanel.RecentlyDismissed())
                { try { Sparq.UI.CrisisResourcesPanel.Show(); } catch {} }
                if (verdict.Reasons.Contains(Sparq.Safety.ContentModerator.Category.ThreatViolence)
                    && !Sparq.UI.ThreatResponsePanel.RecentlyDismissed())
                { try { Sparq.UI.ThreatResponsePanel.Show(); } catch {} }
            }

            try
            {
                Sparq.Safety.ModerationQueue.Submit(player, msg, _selected, notes);
                if (_alsoBlock && !string.IsNullOrEmpty(player))
                {
                    Sparq.Safety.BlockList.Block(player);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ReportPanel] Submit failed: {ex.Message}");
            }

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Confirmation toast (find any top canvas)
            try
            {
                var anyCanvas = Object.FindFirstObjectByType<Canvas>();
                if (anyCanvas != null)
                    XPFloater.Spawn(anyCanvas.transform,
                        new Vector3(Screen.width / 2f, Screen.height * 0.85f, 0),
                        "Report sent. Thank you.", new Color(0.55f, 0.95f, 0.55f));
            }
            catch {}

            Hide();
        }

        // ─────────────────────────────────────────────────────────────────
        // CATEGORY CHIPS
        // ─────────────────────────────────────────────────────────────────

        private static void BuildCategoryChip(Transform parent, float xPos, float yPos,
            float w, float h, string label, Sparq.Safety.ModerationQueue.ReportReason r)
        {
            var chip = NewGO("Chip_" + r, parent, typeof(Image), typeof(Button));
            var rt = chip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.sizeDelta = new Vector2(w, h);
            var img = chip.GetComponent<Image>();
            img.color = UNSEL_BG;
            var lbl = MakeText(chip.transform, "Lbl", label,
                26, FontStyles.Bold, Color.white);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(14, 0); lrt.offsetMax = new Vector2(-14, 0);
            lbl.alignment = TextAlignmentOptions.Center;

            chip.GetComponent<Button>().onClick.AddListener(() => {
                _selected = r;
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                RefreshCategorySelection();
            });

            _categoryButtons.Add((img, lbl, r));
        }

        private static void RefreshCategorySelection()
        {
            if (_categoryButtons == null) return;
            foreach (var (bg, lbl, r) in _categoryButtons)
            {
                bool sel = r == _selected;
                bg.color = sel ? SEL_BG : UNSEL_BG;
                lbl.color = sel ? Color.white : new Color(0.92f, 0.90f, 1f);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // ALSO-BLOCK ROW
        // ─────────────────────────────────────────────────────────────────

        private static void BuildAlsoBlockRow(Transform parent, float y)
        {
            var row = NewGO("AlsoBlock", parent, typeof(Image), typeof(Button));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-60, 100);
            row.GetComponent<Image>().color = ROW_BG;

            var lbl = MakeText(row.transform, "Lbl", "Also block this player",
                28, FontStyles.Bold, Color.white);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(28, 0);
            lrt.sizeDelta = new Vector2(600, 0);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var checkbox = NewGO("Check", row.transform, typeof(Image));
            var ckRT = checkbox.GetComponent<RectTransform>();
            ckRT.anchorMin = new Vector2(1, 0.5f); ckRT.anchorMax = new Vector2(1, 0.5f);
            ckRT.pivot = new Vector2(1, 0.5f);
            ckRT.anchoredPosition = new Vector2(-30, 0);
            ckRT.sizeDelta = new Vector2(70, 70);
            var cbImg = checkbox.GetComponent<Image>();

            var tick = MakeText(checkbox.transform, "Tick", "✓",
                48, FontStyles.Bold, new Color(0.10f, 0.05f, 0.18f));
            var tRT = tick.rectTransform;
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
            tick.alignment = TextAlignmentOptions.Center;

            void Refresh()
            {
                cbImg.color = _alsoBlock ? GOLD : new Color(0.30f, 0.22f, 0.45f, 1f);
                tick.gameObject.SetActive(_alsoBlock);
            }
            Refresh();

            row.GetComponent<Button>().onClick.AddListener(() => {
                _alsoBlock = !_alsoBlock;
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                Refresh();
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // INPUT FIELD (TMP_InputField with proper Text Area + RectMask2D)
        // ─────────────────────────────────────────────────────────────────

        private static TMP_InputField BuildInputField(Transform parent, float y, string placeholder)
        {
            // Outer frame
            var frame = NewGO("Notes", parent, typeof(Image), typeof(TMP_InputField));
            var fRT = frame.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0, 1); fRT.anchorMax = new Vector2(1, 1);
            fRT.pivot = new Vector2(0.5f, 1);
            fRT.anchoredPosition = new Vector2(0, y);
            fRT.sizeDelta = new Vector2(-60, 150);
            frame.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.12f, 0.95f);

            // Text Area (with RectMask2D)
            var textArea = NewGO("Text Area", frame.transform, typeof(RectMask2D));
            var taRT = textArea.GetComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(18, 14); taRT.offsetMax = new Vector2(-18, -14);

            // Placeholder
            var ph = NewGO("Placeholder", textArea.transform);
            var phRT = ph.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
            var phTm = ph.AddComponent<TextMeshProUGUI>();
            phTm.text = placeholder;
            phTm.fontSize = 24;
            phTm.fontStyle = FontStyles.Italic;
            phTm.color = new Color(0.6f, 0.55f, 0.75f, 1f);
            phTm.font = TMP_Settings.defaultFontAsset;
            phTm.alignment = TextAlignmentOptions.TopLeft;
            phTm.textWrappingMode = TextWrappingModes.Normal;
            phTm.raycastTarget = false;

            // Text
            var txt = NewGO("Text", textArea.transform);
            var txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
            var txtTm = txt.AddComponent<TextMeshProUGUI>();
            txtTm.text = "";
            txtTm.fontSize = 24;
            txtTm.color = Color.white;
            txtTm.font = TMP_Settings.defaultFontAsset;
            txtTm.alignment = TextAlignmentOptions.TopLeft;
            txtTm.textWrappingMode = TextWrappingModes.Normal;
            txtTm.raycastTarget = false;

            var input = frame.GetComponent<TMP_InputField>();
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = txtTm;
            input.placeholder = phTm;
            input.characterLimit = 250;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.fontAsset = TMP_Settings.defaultFontAsset;
            input.pointSize = 24;
            return input;
        }

        // ─────────────────────────────────────────────────────────────────
        // SHARED HELPERS
        // ─────────────────────────────────────────────────────────────────

        private static void BuildSectionLabel(Transform parent, string text, float y)
        {
            var go = NewGO("Section", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-80, 50);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = 26;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(1f, 0.82f, 0.42f);
            tm.font = TMP_Settings.defaultFontAsset;
            tm.alignment = TextAlignmentOptions.MidlineLeft;
            tm.raycastTarget = false;
        }

        private static GameObject MakeButton(Transform parent, float xPos, float yPos,
            float w, float h, string text, Color bg, Color fg, System.Action onTap)
        {
            var btn = NewGO("Btn_" + text, parent, typeof(Image), typeof(Button));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.sizeDelta = new Vector2(w, h);
            btn.GetComponent<Image>().color = bg;
            var lbl = MakeText(btn.transform, "Lbl", text, 32, FontStyles.Bold, fg);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
            btn.GetComponent<Button>().onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onTap();
            });
            return btn;
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
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
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
