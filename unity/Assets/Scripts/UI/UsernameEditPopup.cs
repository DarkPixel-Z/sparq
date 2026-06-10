using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Username editor. Instantiates Layer Lab's polished
    /// Popup_Change_Name.prefab and wires it to PlayerData.playerName.
    ///
    /// Usage:
    ///   - UsernameEditPopup.Show()       — manual open (e.g. from ProfilePanel "Edit Name" tap)
    ///   - UsernameEditPopup.ShowIfFirstTime() — auto-open if name is still default "Sparq User"
    /// </summary>
    public static class UsernameEditPopup
    {
        private const string POPUP_PREFAB_PATH =
            "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/Popup_Change_Name.prefab";
        private const string DEFAULT_NAME = "Sparq User";

        private static GameObject _root;
        private static System.Action _onSaved;

        public static void Show(System.Action onSaved = null)
        {
            if (_root != null) Object.Destroy(_root);
            _onSaved = onSaved;

            EnsureEventSystem();

            // Top-sort canvas above everything
            _root = new GameObject("Sparq_UsernameEdit",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            c.sortingOrder = maxSort + 10;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop (tappable to dismiss)
            var bg = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            bg.transform.SetParent(_root.transform, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.72f);
            bg.GetComponent<Button>().onClick.AddListener(Hide);

            // Switched away from Layer Lab's prefab (broken Feel MMF_Player
            // dependencies kept it in a non-interactive state). Build a clean
            // procedural popup we fully control — no prefab, no broken scripts,
            // no animation gating. Guaranteed to work.
            BuildProceduralPopup();

            Debug.Log("[UsernameEditPopup] Ready.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
            _onSaved = null;
        }

        // Auto-show on first home load if name hasn't been set yet
        public static void ShowIfFirstTime()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;
            if (string.IsNullOrEmpty(data.playerName) || data.playerName == DEFAULT_NAME)
                Show();
        }

        // ─────────────────────────────────────────────────────────────────
        // PREFAB WIRING
        // ─────────────────────────────────────────────────────────────────

        private static void WirePopup(GameObject popup)
        {
            // Find the TMP_InputField (where the user types the name)
            TMP_InputField input = popup.GetComponentInChildren<TMP_InputField>(true);
            // Find buttons — typically a "Save"/"OK"/"Confirm" and a "Cancel"/"Close"
            var data = Sparq.Core.SaveService.Data;
            string current = data != null ? data.playerName : DEFAULT_NAME;
            if (string.IsNullOrEmpty(current)) current = DEFAULT_NAME;

            if (input != null)
            {
                input.text = current;
                // Match the popup's hint text ("6-12 characters")
                input.characterLimit = 12;
                input.contentType = TMP_InputField.ContentType.Standard;
                input.ActivateInputField();
            }
            else
            {
                Debug.LogWarning("[UsernameEditPopup] No TMP_InputField found in Popup_Change_Name.");
            }

            // Determine popup's RectTransform bounds for position-based close detection
            var popupRT = popup.transform as RectTransform;
            Vector3[] pc = new Vector3[4];
            if (popupRT != null) popupRT.GetWorldCorners(pc);

            int wiredSave = 0, wiredClose = 0;
            // Dump ALL buttons first so we can see what's in the popup
            var allBtns = popup.GetComponentsInChildren<Button>(true);
            Debug.Log($"[UsernameEditPopup] Found {allBtns.Length} button(s) in popup:");
            foreach (var b in allBtns)
            {
                if (b == null) continue;
                var t = b.GetComponentInChildren<TMP_Text>(true);
                string tt = t != null ? t.text : "<no text>";
                var rt2 = b.GetComponent<RectTransform>();
                Vector3[] bc2 = new Vector3[4]; rt2.GetWorldCorners(bc2);
                Vector3 ctr = (bc2[0] + bc2[2]) * 0.5f;
                Debug.Log($"  Button '{b.gameObject.name}' text='{tt}' worldCenter=({ctr.x:F0},{ctr.y:F0}) size=({rt2.rect.width:F0}x{rt2.rect.height:F0})");
            }

            foreach (var btn in allBtns)
            {
                if (btn == null) continue;
                var tmp = btn.GetComponentInChildren<TMP_Text>(true);
                string lblRaw = (tmp != null ? tmp.text : "") ?? "";
                string lbl = lblRaw.Trim().ToUpper();
                string goName = (btn.gameObject.name ?? "").ToLower();
                // (DON'T remove listeners yet — only after we decide we're wiring)

                // ── CLOSE detection — name OR Unicode glyph OR top-right position ──
                bool isCloseByName =
                    goName.Contains("close") || goName.Contains("cancel") ||
                    goName.Contains("btn_x") || goName == "x" || goName.EndsWith("_x") ||
                    goName.Contains("xbtn") || goName.Contains("dismiss");
                bool isCloseByGlyph =
                    lblRaw.Contains("✕") || lblRaw.Contains("✖") || lblRaw.Contains("×") ||
                    lbl == "X";
                bool isCloseByText =
                    lbl.Contains("CANCEL") || lbl.Contains("CLOSE") || lbl.Contains("DISMISS");

                // Top-right corner of popup = traditional X close-button spot
                bool isCloseByPosition = false;
                if (popupRT != null)
                {
                    Vector3[] bc = new Vector3[4];
                    btn.GetComponent<RectTransform>().GetWorldCorners(bc);
                    Vector3 bCenter = (bc[0] + bc[2]) * 0.5f;
                    float popupW = pc[2].x - pc[0].x;
                    float popupH = pc[1].y - pc[0].y;
                    if (popupW > 1 && popupH > 1)
                    {
                        float relX = (bCenter.x - pc[0].x) / popupW;
                        float relY = (bCenter.y - pc[0].y) / popupH;
                        // Upper-right corner of the popup (top 25%, right 25%)
                        if (relX > 0.75f && relY > 0.75f) isCloseByPosition = true;
                    }
                }

                // ── SAVE detection ──
                bool isSaveByName =
                    goName.Contains("save") || goName.Contains("ok") ||
                    goName.Contains("confirm") || goName.Contains("apply") ||
                    goName.Contains("btn_change") || goName.Contains("changename");
                bool isSaveByText =
                    lbl.Contains("SAVE") || lbl.Contains("OK") || lbl.Contains("CONFIRM") ||
                    lbl.Contains("DONE") || lbl.Contains("APPLY") || lbl.Contains("CHANGE");

                // Position-based — bottom-center of popup is the main action button
                // (Layer Lab's "12 gems to change name" button sits there).
                bool isSaveByPosition = false;
                if (popupRT != null && !isCloseByName && !isCloseByGlyph &&
                    !isCloseByText && !isCloseByPosition)
                {
                    Vector3[] bc = new Vector3[4];
                    btn.GetComponent<RectTransform>().GetWorldCorners(bc);
                    Vector3 bCenter = (bc[0] + bc[2]) * 0.5f;
                    float popupW = pc[2].x - pc[0].x;
                    float popupH = pc[1].y - pc[0].y;
                    if (popupW > 1 && popupH > 1)
                    {
                        float relX = (bCenter.x - pc[0].x) / popupW;
                        float relY = (bCenter.y - pc[0].y) / popupH;
                        // Bottom-third, center-half of popup = main action
                        if (relY < 0.40f && relX > 0.20f && relX < 0.80f)
                            isSaveByPosition = true;
                    }
                }

                if (isCloseByName || isCloseByGlyph || isCloseByText || isCloseByPosition)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Hide);
                    Debug.Log($"[UsernameEditPopup] Close button wired: '{btn.gameObject.name}' text='{lblRaw}' (name={isCloseByName} glyph={isCloseByGlyph} text={isCloseByText} pos={isCloseByPosition})");
                    wiredClose++;
                }
                else if (isSaveByName || isSaveByText || isSaveByPosition)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SaveAndClose(input));
                    Debug.Log($"[UsernameEditPopup] Save button wired: '{btn.gameObject.name}' text='{lblRaw}' (name={isSaveByName} text={isSaveByText} pos={isSaveByPosition})");
                    wiredSave++;
                }
                else
                {
                    Debug.Log($"[UsernameEditPopup] Button '{btn.gameObject.name}' text='{lblRaw}' NOT wired (didn't match close or save).");
                }
            }
            Debug.Log($"[UsernameEditPopup] Wired {wiredSave} save, {wiredClose} close button(s).");

            // ── Fallback: if NO close button was matched, force-wire the
            //    SMALLEST button as close (X icons are always the tiniest). ──
            if (wiredClose == 0 && allBtns.Length > 1)
            {
                Button smallest = null;
                float smallestArea = float.PositiveInfinity;
                foreach (var b in allBtns)
                {
                    if (b == null) continue;
                    var rt = b.GetComponent<RectTransform>();
                    float area = rt.rect.width * rt.rect.height;
                    if (area > 1 && area < smallestArea)
                    { smallestArea = area; smallest = b; }
                }
                if (smallest != null)
                {
                    smallest.onClick.RemoveAllListeners();
                    smallest.onClick.AddListener(Hide);
                    Debug.Log($"[UsernameEditPopup] FALLBACK: smallest button '{smallest.gameObject.name}' wired as close (area={smallestArea:F0}).");
                }
            }
        }

        private static void SaveAndClose(TMP_InputField input)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            string newName = input != null ? input.text.Trim() : "";
            if (string.IsNullOrEmpty(newName)) newName = DEFAULT_NAME;
            // Content moderation — block unsafe usernames
            var verdict = Sparq.Safety.ContentModerator.InspectUsername(newName);
            if (!verdict.Allowed)
            {
                Debug.LogWarning($"[UsernameEditPopup] Blocked: {verdict.UserFacingMessage}");
                if (input != null) input.text = verdict.SanitizedText;
                return;   // don't save, don't close
            }
            var data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.playerName = newName;
                try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
                Debug.Log($"[UsernameEditPopup] Saved playerName='{newName}'.");
            }
            _onSaved?.Invoke();
            Hide();
        }

        // ─────────────────────────────────────────────────────────────────
        // PROCEDURAL POPUP — clean, simple, guaranteed to work
        // ─────────────────────────────────────────────────────────────────
        //
        //   ┌──────────── Card ────────────┐
        //   │ Change Your Nickname     [X] │  ← title + close
        //   │                              │
        //   │ Use a nickname to represent  │  ← subtitle
        //   │       yourself!              │
        //   │                              │
        //   │  ┌────────────────────────┐  │  ← input field
        //   │  │ Sparq User             │  │
        //   │  └────────────────────────┘  │
        //   │   Must be 6-12 characters    │  ← hint
        //   │                              │
        //   │         [   SAVE   ]         │  ← save button
        //   └──────────────────────────────┘
        //
        private static void BuildProceduralPopup()
        {
            string current = "Sparq User";
            try { var d = Sparq.Core.SaveService.Data;
                  if (d != null && !string.IsNullOrEmpty(d.playerName)) current = d.playerName; } catch {}

            // ── Card ──
            var card = NewGO("PopupCard", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(820, 720);
            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.18f, 0.12f, 0.30f, 1f);
            cardImg.raycastTarget = true;   // blocks clicks behind it

            // ── Title bar ──
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = new Vector2(0, 0);
            tbRT.sizeDelta = new Vector2(0, 110);
            titleBar.GetComponent<Image>().color = new Color(0.55f, 0.40f, 0.85f, 1f);
            titleBar.GetComponent<Image>().raycastTarget = false;

            var titleTxt = MakeProcText(titleBar.transform, "Title", "Change Your Nickname",
                48, FontStyles.Bold, Color.white);
            var ttRT = titleTxt.rectTransform;
            ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = new Vector2(20, 0); ttRT.offsetMax = new Vector2(-120, 0);

            // ── Close X button (in title bar, top-right) ──
            var closeBtn = NewGO("CloseBtn", card.transform,
                typeof(Image), typeof(Button));
            var cbRT = closeBtn.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1, 1); cbRT.anchorMax = new Vector2(1, 1);
            cbRT.pivot = new Vector2(1, 1);
            cbRT.anchoredPosition = new Vector2(-20, -20);
            cbRT.sizeDelta = new Vector2(75, 75);
            var closeImg = closeBtn.GetComponent<Image>();
            closeImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);
            closeImg.raycastTarget = true;
            var closeBtnComp = closeBtn.GetComponent<Button>();
            closeBtnComp.interactable = true;
            closeBtnComp.targetGraphic = closeImg;
            var xTxt = MakeProcText(closeBtn.transform, "X", "X",
                52, FontStyles.Bold, Color.white);
            var xRT = xTxt.rectTransform;
            xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
            xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;
            xTxt.alignment = TextAlignmentOptions.Center;
            closeBtn.GetComponent<Button>().onClick.AddListener(() => {
                Debug.Log("[UsernameEditPopup] ✓ Close button onClick fired.");
                Hide();
            });

            // ── Subtitle ──
            var sub = MakeProcText(card.transform, "Sub", "Use a nickname to represent yourself!",
                28, FontStyles.Normal, new Color(1f, 0.95f, 0.85f));
            var srt = sub.rectTransform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -160);
            srt.sizeDelta = new Vector2(0, 40);
            sub.alignment = TextAlignmentOptions.Center;

            // ── Input field with PROPER TMP hierarchy ──
            // Structure required by TMP_InputField:
            //   InputField (Image + TMP_InputField, raycast target = Image)
            //     ├── Text Area (RectMask2D — clips overflow, no raycast)
            //     │   ├── Text (visible text, no raycast)
            //     │   └── Placeholder (hint, no raycast)
            //
            // Without the Text Area + RectMask2D the InputField won't accept
            // typed input correctly (text update path expects masked container).
            var fieldGO = NewGO("Input", card.transform, typeof(Image));
            var fRT = fieldGO.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0.5f, 0.5f); fRT.anchorMax = new Vector2(0.5f, 0.5f);
            fRT.pivot = new Vector2(0.5f, 0.5f);
            fRT.anchoredPosition = new Vector2(0, 60);
            fRT.sizeDelta = new Vector2(680, 100);
            var fieldBg = fieldGO.GetComponent<Image>();
            fieldBg.color = new Color(0.10f, 0.05f, 0.20f, 1f);
            fieldBg.raycastTarget = true;

            // Text Area — masked container REQUIRED by TMP_InputField
            var textAreaGO = NewGO("Text Area", fieldGO.transform,
                typeof(UnityEngine.UI.RectMask2D));
            var taRT = textAreaGO.GetComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(20, 6); taRT.offsetMax = new Vector2(-20, -6);

            // Visible text component
            var fieldTextGO = NewGO("Text", textAreaGO.transform);
            var ftRT = fieldTextGO.GetComponent<RectTransform>();
            ftRT.anchorMin = Vector2.zero; ftRT.anchorMax = Vector2.one;
            ftRT.offsetMin = Vector2.zero; ftRT.offsetMax = Vector2.zero;
            var fieldText = fieldTextGO.AddComponent<TextMeshProUGUI>();
            fieldText.font = TMP_Settings.defaultFontAsset;
            fieldText.fontSize = 40;
            fieldText.color = Color.white;
            fieldText.alignment = TextAlignmentOptions.MidlineLeft;
            fieldText.raycastTarget = false;
            fieldText.textWrappingMode = TextWrappingModes.NoWrap;

            // Placeholder
            var phGO = NewGO("Placeholder", textAreaGO.transform);
            var phRT = phGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
            var phTxt = phGO.AddComponent<TextMeshProUGUI>();
            phTxt.font = TMP_Settings.defaultFontAsset;
            phTxt.fontSize = 40;
            phTxt.color = new Color(0.55f, 0.50f, 0.65f);
            phTxt.text = "Enter name";
            phTxt.alignment = TextAlignmentOptions.MidlineLeft;
            phTxt.raycastTarget = false;
            phTxt.textWrappingMode = TextWrappingModes.NoWrap;

            // NOW add the TMP_InputField — AFTER text/placeholder exist so its
            // OnEnable hook can wire them. Add it to the fieldGO root.
            var inp = fieldGO.AddComponent<TMP_InputField>();
            inp.targetGraphic = fieldBg;
            inp.textViewport = taRT;
            inp.textComponent = fieldText;
            inp.placeholder = phTxt;
            inp.characterLimit = 12;
            inp.contentType = TMP_InputField.ContentType.Standard;
            inp.lineType = TMP_InputField.LineType.SingleLine;
            inp.interactable = true;
            inp.text = current;

            // ── Hint ──
            var hint = MakeProcText(card.transform, "Hint", "Must be 6-12 characters",
                24, FontStyles.Italic, new Color(0.70f, 0.65f, 0.80f));
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0, 0.5f); hrt.anchorMax = new Vector2(1, 0.5f);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.anchoredPosition = new Vector2(0, -40);
            hrt.sizeDelta = new Vector2(0, 32);
            hint.alignment = TextAlignmentOptions.Center;

            // ── Save button ──
            var saveBtn = NewGO("SaveBtn", card.transform,
                typeof(Image), typeof(Button));
            var sbRT = saveBtn.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.5f, 0); sbRT.anchorMax = new Vector2(0.5f, 0);
            sbRT.pivot = new Vector2(0.5f, 0);
            sbRT.anchoredPosition = new Vector2(0, 60);
            sbRT.sizeDelta = new Vector2(420, 110);
            // Belt-and-braces: explicitly set everything on the Image + Button
            var saveImg = saveBtn.GetComponent<Image>();
            saveImg.color = new Color(1f, 0.78f, 0.22f, 1f);
            saveImg.raycastTarget = true;
            var saveBtnComp = saveBtn.GetComponent<Button>();
            saveBtnComp.interactable = true;
            saveBtnComp.targetGraphic = saveImg;
            var saveTxt = MakeProcText(saveBtn.transform, "Lbl", "SAVE",
                44, FontStyles.Bold, new Color(0.10f, 0.05f, 0.20f));
            var stRT = saveTxt.rectTransform;
            stRT.anchorMin = Vector2.zero; stRT.anchorMax = Vector2.one;
            stRT.offsetMin = Vector2.zero; stRT.offsetMax = Vector2.zero;
            saveTxt.alignment = TextAlignmentOptions.Center;
            saveBtn.GetComponent<Button>().onClick.AddListener(() => {
                Debug.Log("[UsernameEditPopup] ✓ Save button onClick fired.");
                SaveAndClose(inp);
            });

            // Focus input
            inp.ActivateInputField();
            Debug.Log($"[UsernameEditPopup] Procedural popup built. current name='{current}'");
        }

        // Lightweight GO factory — fewer typing
        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static TMP_Text MakeProcText(Transform parent, string name, string text,
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
            try { tm.outlineWidth = 0.22f; tm.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f); } catch {}
            return tm;
        }

        // ─────────────────────────────────────────────────────────────────
        // FALLBACK (if prefab missing or in build)
        // ─────────────────────────────────────────────────────────────────

        private static void BuildSimpleFallback()
        {
            // Minimal procedural popup — works without the prefab
            var card = new GameObject("FallbackCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(720, 420);
            card.GetComponent<Image>().color = new Color(0.15f, 0.10f, 0.25f, 1f);

            var title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(card.transform, false);
            var titleTm = title.AddComponent<TextMeshProUGUI>();
            titleTm.text = "Choose Your Name";
            titleTm.fontSize = 44;
            titleTm.fontStyle = FontStyles.Bold;
            titleTm.color = new Color(1f, 0.92f, 0.55f);
            titleTm.alignment = TextAlignmentOptions.Center;
            titleTm.font = TMP_Settings.defaultFontAsset;
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -40);
            trt.sizeDelta = new Vector2(0, 60);

            // No input field in fallback — just use the existing name
            var data = Sparq.Core.SaveService.Data;
            if (data != null) data.playerName = "Sparq Hero";
            try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────

        // Force every Selectable to interactable=true, every CanvasGroup to
        // alpha=1 + interactable + blocksRaycasts. Layer Lab's popup has a
        // missing Feel MMF_Player script reference — that script would normally
        // animate alpha 0→1 and enable interactivity, but without it the popup
        // spawns inert (alpha=0, interactable=false). This forces final state.
        //
        // Also kills any missing-script MonoBehaviour components on the prefab
        // (those are the broken Feel references showing in the warnings).
        private static void ForceInteractableAll(GameObject root)
        {
            int fixedCount = 0;
            foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
            {
                if (sel == null) continue;
                if (!sel.interactable) { sel.interactable = true; fixedCount++; }
            }
            foreach (var cg in root.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (cg == null) continue;
                if (cg.alpha < 0.99f) { cg.alpha = 1f; fixedCount++; }
                if (!cg.interactable) { cg.interactable = true; fixedCount++; }
                if (!cg.blocksRaycasts) { cg.blocksRaycasts = true; fixedCount++; }
            }
            Debug.Log($"[UsernameEditPopup] ForceInteractableAll: re-enabled/un-faded {fixedCount} item(s).");

            // Strip the broken "missing script" MonoBehaviour stubs from every
            // child — they're Feel MMF_Player references that don't exist in
            // this project. They generate "The referenced script (Unknown) is
            // missing!" warnings and may interfere with the popup state.
            #if UNITY_EDITOR
            int stripped = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                stripped += UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            if (stripped > 0)
                Debug.Log($"[UsernameEditPopup] Stripped {stripped} missing-script MonoBehaviour(s) from popup.");
            #endif
        }

        // Forensic dump — what's blocking the X button
        private static void DiagnosePopup(GameObject root)
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            Debug.Log($"[PopupDiag] EventSystem: {(es != null ? $"'{es.name}' enabled={es.enabled}" : "MISSING")}");

            int totalSel = 0, dis = 0, noRay = 0;
            foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
            {
                if (sel == null) continue;
                totalSel++;
                if (!sel.interactable) { dis++; Debug.LogWarning($"[PopupDiag] DISABLED: '{sel.gameObject.name}'"); }
                if (sel.targetGraphic != null && !sel.targetGraphic.raycastTarget)
                { noRay++; Debug.LogWarning($"[PopupDiag] NO-RAYCAST graphic on '{sel.gameObject.name}'"); }
            }
            foreach (var cg in root.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (cg == null) continue;
                Debug.Log($"[PopupDiag] CanvasGroup on '{cg.gameObject.name}' interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts} alpha={cg.alpha}");
            }
            Debug.Log($"[PopupDiag] Total Selectables: {totalSel} (disabled={dis}, no-raycast-on-graphic={noRay}).");
        }

        // Disable raycast on decoration Images. Keep raycast on any Image
        // whose ancestor chain contains a Selectable (Button/InputField/etc.) —
        // that's how Unity routes clicks into the Button's onClick.
        private static void KillNonButtonRaycasts(GameObject root)
        {
            int killed = 0;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img == null || !img.raycastTarget) continue;
                if (img.GetComponent<Selectable>() != null) continue;

                bool hasSelectableAncestor = false;
                Transform t = img.transform.parent;
                while (t != null && t != root.transform.parent)
                {
                    if (t.GetComponent<Selectable>() != null)
                    { hasSelectableAncestor = true; break; }
                    t = t.parent;
                }
                if (hasSelectableAncestor) continue;

                img.raycastTarget = false;
                killed++;
            }
            Debug.Log($"[UsernameEditPopup] Killed raycastTarget on {killed} decoration Image(s).");
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
