using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// First-launch sign-up / sign-in flow.
    ///
    /// Sparq uses local-storage accounts (no backend auth yet) so:
    ///   - New user (PlayerData.playerName is empty or default) → shows the
    ///     auth panel asking for a username. Save → go to lobby.
    ///   - Returning user → name already saved, skip directly to lobby.
    ///
    /// Conceptually "sign up = log in" — picking a name IS the login.
    /// </summary>
    public static class AuthPanel
    {
        private const string DEFAULT_NAME = "Sparq User";

        private static GameObject _root;

        /// <summary>
        /// Entry point. Called instead of HomeLobbyPanel.Show() at app start.
        /// Routes to auth or directly to lobby based on saved state.
        /// </summary>
        public static void ShowOrSkipToLobby()
        {
            var data = Sparq.Core.SaveService.Data;
            bool needsSignup = data == null ||
                               string.IsNullOrEmpty(data.playerName) ||
                               data.playerName == DEFAULT_NAME;

            if (needsSignup)
            {
                Debug.Log("[AuthPanel] No saved username — showing sign-up screen.");
                Show();
            }
            else
            {
                // Returning user — but they may have signed up before the
                // hero picker existed and have an empty heroClass. Route
                // through the same hero-or-lobby check used after signup.
                Debug.Log($"[AuthPanel] Returning user '{data.playerName}' — routing.");
                RouteToHeroOrLobby();
            }
        }

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);

            EnsureEventSystem();

            // ── Top-sort canvas ──
            _root = new GameObject("Sparq_AuthPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 14000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            c.sortingOrder = maxSort + 100;   // well above lobby
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // ── Solid backdrop — completely covers the home scene ──
            var bg = NewGO("Bg", _root.transform, typeof(Image));
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.05f, 0.18f, 1f);   // deep navy
            bgImg.raycastTarget = true;   // blocks clicks behind

            // ── Sparq logo / title ──
            var title = NewGO("Title", _root.transform);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 1); trt.anchorMax = new Vector2(0.5f, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -180);
            trt.sizeDelta = new Vector2(800, 110);
            var titleTm = title.AddComponent<TextMeshProUGUI>();
            titleTm.text = "<color=#FFE078><b>Welcome to Sparq</b></color>";
            titleTm.fontSize = 64;
            titleTm.font = TMP_Settings.defaultFontAsset;
            titleTm.alignment = TextAlignmentOptions.Center;
            titleTm.raycastTarget = false;
            try { titleTm.outlineWidth = 0.30f; titleTm.outlineColor = new Color(0.40f, 0.20f, 0.05f); } catch {}

            // ── Subtitle ──
            var sub = NewGO("Sub", _root.transform);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 1); srt.anchorMax = new Vector2(0.5f, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -320);
            srt.sizeDelta = new Vector2(900, 60);
            var subTm = sub.AddComponent<TextMeshProUGUI>();
            subTm.text = "Choose a hero name to begin your journey";
            subTm.fontSize = 40;   // bumped from 32 — was too small to read at phone distance
            subTm.font = TMP_Settings.defaultFontAsset;
            subTm.color = new Color(0.92f, 0.88f, 1.00f);   // slightly brighter for contrast
            subTm.alignment = TextAlignmentOptions.Center;
            subTm.raycastTarget = false;

            // ── Sparq logo (was a tiny hero portrait; the logo reads at-a-glance
            //    and matches the welcome banner the rest of the FTUE uses) ──
            try
            {
                var logoSp = LoadSpriteSafe("Sparq/sparq-logo-transparent",
                                            "Assets/Art/Sparq/sparq-logo-transparent.png");
                if (logoSp != null)
                {
                    var logo = NewGO("SparqLogo", _root.transform, typeof(Image));
                    var lrt = logo.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(0.5f, 0.5f); lrt.anchorMax = new Vector2(0.5f, 0.5f);
                    lrt.pivot = new Vector2(0.5f, 0.5f);
                    lrt.anchoredPosition = new Vector2(0, 220);
                    lrt.sizeDelta = new Vector2(560, 360);
                    var lImg = logo.GetComponent<Image>();
                    lImg.sprite = logoSp;
                    lImg.preserveAspect = true;
                    lImg.raycastTarget = false;
                }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[AuthPanel] Logo load failed: {ex.Message}"); }

            // ── Username input field ──
            var fieldGO = NewGO("Input", _root.transform, typeof(Image));
            var fRT = fieldGO.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0.5f, 0.5f); fRT.anchorMax = new Vector2(0.5f, 0.5f);
            fRT.pivot = new Vector2(0.5f, 0.5f);
            fRT.anchoredPosition = new Vector2(0, -160);
            fRT.sizeDelta = new Vector2(720, 110);
            var fieldBg = fieldGO.GetComponent<Image>();
            fieldBg.color = new Color(0.16f, 0.10f, 0.28f, 1f);
            fieldBg.raycastTarget = true;

            // Text Area + Mask + Text + Placeholder (proper TMP_InputField hierarchy)
            var textAreaGO = NewGO("Text Area", fieldGO.transform, typeof(RectMask2D));
            var taRT = textAreaGO.GetComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(28, 8); taRT.offsetMax = new Vector2(-28, -8);

            var textGO = NewGO("Text", textAreaGO.transform);
            var ttRT = textGO.GetComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = Vector2.zero; ttRT.offsetMax = Vector2.zero;
            var fieldText = textGO.AddComponent<TextMeshProUGUI>();
            fieldText.font = TMP_Settings.defaultFontAsset;
            fieldText.fontSize = 44;
            fieldText.color = Color.white;
            fieldText.alignment = TextAlignmentOptions.MidlineLeft;
            fieldText.raycastTarget = false;
            fieldText.textWrappingMode = TextWrappingModes.NoWrap;

            var phGO = NewGO("Placeholder", textAreaGO.transform);
            var phRT = phGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
            var phTxt = phGO.AddComponent<TextMeshProUGUI>();
            phTxt.font = TMP_Settings.defaultFontAsset;
            phTxt.fontSize = 44;
            phTxt.color = new Color(0.55f, 0.50f, 0.65f);
            phTxt.text = "Hero name";
            phTxt.alignment = TextAlignmentOptions.MidlineLeft;
            phTxt.raycastTarget = false;
            phTxt.textWrappingMode = TextWrappingModes.NoWrap;

            var inp = fieldGO.AddComponent<TMP_InputField>();
            inp.targetGraphic = fieldBg;
            inp.textViewport = taRT;
            inp.textComponent = fieldText;
            inp.placeholder = phTxt;
            inp.characterLimit = 12;
            inp.contentType = TMP_InputField.ContentType.Standard;
            inp.lineType = TMP_InputField.LineType.SingleLine;
            inp.interactable = true;
            inp.text = "";

            // Submit on soft-keyboard Done/Enter — gives the user an escape
            // hatch if the BEGIN button isn't receiving taps for any reason
            // (Android keyboard occlusion, input-module quirks, etc). Two paths
            // to commit; either works.
            inp.onSubmit.AddListener(_ => SaveAndEnter(inp));
            inp.onEndEdit.AddListener(value => {
                // Some Android keyboards fire onEndEdit instead of onSubmit when
                // the user dismisses the keyboard with Done. Catch that too.
                if (!string.IsNullOrEmpty(value) && value.Trim().Length >= 3)
                    SaveAndEnter(inp);
            });

            // ── Hint ──
            var hint = NewGO("Hint", _root.transform);
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 0.5f); hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(0, -244);
            hrt.sizeDelta = new Vector2(720, 56);   // bumped from 36 to fit 32pt italic
            var hintTm = hint.AddComponent<TextMeshProUGUI>();
            hintTm.text = "3 to 12 characters";
            hintTm.fontSize = 32;   // bumped from 24 — italic at 24pt was illegible on a phone
            hintTm.font = TMP_Settings.defaultFontAsset;
            hintTm.color = new Color(0.82f, 0.78f, 0.92f);   // brighter than before for contrast
            hintTm.fontStyle = FontStyles.Italic;
            hintTm.alignment = TextAlignmentOptions.Center;
            hintTm.raycastTarget = false;

            // ── BEGIN button ──
            var btnGO = NewGO("BeginBtn", _root.transform, typeof(Image), typeof(Button));
            var sbRT = btnGO.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.5f, 0.5f); sbRT.anchorMax = new Vector2(0.5f, 0.5f);
            sbRT.pivot = new Vector2(0.5f, 0.5f);
            sbRT.anchoredPosition = new Vector2(0, -400);
            sbRT.sizeDelta = new Vector2(480, 120);
            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(1f, 0.78f, 0.22f, 1f);   // gold
            btnImg.raycastTarget = true;
            var btnComp = btnGO.GetComponent<Button>();
            btnComp.targetGraphic = btnImg;
            btnComp.interactable = true;

            var btnLbl = NewGO("Lbl", btnGO.transform);
            var blRT = btnLbl.GetComponent<RectTransform>();
            blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
            blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
            var btnTm = btnLbl.AddComponent<TextMeshProUGUI>();
            btnTm.text = "BEGIN ADVENTURE";
            btnTm.fontSize = 42;
            btnTm.fontStyle = FontStyles.Bold;
            btnTm.color = new Color(0.10f, 0.05f, 0.18f);
            btnTm.font = TMP_Settings.defaultFontAsset;
            btnTm.alignment = TextAlignmentOptions.Center;
            btnTm.raycastTarget = false;

            btnComp.onClick.AddListener(() => {
                Debug.Log($"[AuthPanel] ✓ BEGIN ADVENTURE tapped — saving username and entering lobby.");
                SaveAndEnter(inp);
            });

            // Focus the field so user can start typing right away
            inp.ActivateInputField();

            Debug.Log("[AuthPanel] Welcome screen shown.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // SAVE & ENTER LOBBY
        // ─────────────────────────────────────────────────────────────────
        private static void SaveAndEnter(TMP_InputField inp)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            string name = inp != null ? inp.text.Trim() : "";
            // Length validation — accept 3-12 chars
            if (name.Length < 3)
            {
                Debug.LogWarning("[AuthPanel] Name too short, ignoring.");
                FlashHintError(inp);
                return;
            }
            // Content moderation — usernames have stricter rules
            var verdict = Sparq.Safety.ContentModerator.InspectUsername(name);
            if (!verdict.Allowed)
            {
                Debug.LogWarning($"[AuthPanel] Username blocked: {verdict.UserFacingMessage}");
                FlashHintError(inp);
                // TODO: show toast with verdict.UserFacingMessage
                return;
            }
            // Save to PlayerData
            var data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.playerName = name;
                try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
                Debug.Log($"[AuthPanel] Saved playerName='{name}'.");
            }
            // Hand off to hero picker (first launch) or directly to lobby
            // (returning user who already chose a class).
            Hide();
            RouteToHeroOrLobby();
        }

        /// <summary>
        /// After the username step, decide whether the user still needs to
        /// pick a hero class. Empty heroClass → forced picker, then lobby.
        /// Otherwise straight to lobby. Centralized so ShowOrSkipToLobby
        /// can use the same logic for returning-but-no-hero users.
        /// </summary>
        private static void RouteToHeroOrLobby()
        {
            var data = Sparq.Core.SaveService.Data;
            string heroClass = data?.heroClass ?? "";
            if (string.IsNullOrEmpty(heroClass))
            {
                Debug.Log("[AuthPanel] No hero class yet — showing forced picker.");
                try
                {
                    Sparq.UI.HeroSelectPanel.Show(forcePick: true, onPicked: () => {
                        try { Sparq.UI.HomeLobbyPanel.Show(); }
                        catch (System.Exception ex)
                        { Debug.LogError($"[AuthPanel] Lobby launch after hero pick failed: {ex.Message}"); }
                    });
                }
                catch (System.Exception ex)
                {
                    // If the picker itself fails, don't strand the user — fall through to lobby.
                    Debug.LogError($"[AuthPanel] Hero picker failed, going straight to lobby: {ex.Message}");
                    try { Sparq.UI.HomeLobbyPanel.Show(); } catch {}
                }
            }
            else
            {
                try { Sparq.UI.HomeLobbyPanel.Show(); } catch (System.Exception ex)
                { Debug.LogError($"[AuthPanel] Lobby launch failed: {ex.Message}"); }
            }
        }

        // Flash a red border on the input to show invalid name
        private static void FlashHintError(TMP_InputField inp)
        {
            if (inp == null) return;
            var img = inp.GetComponent<Image>();
            if (img == null) return;
            var orig = img.color;
            img.color = new Color(0.80f, 0.20f, 0.20f, 1f);
            // Restore after a brief flash
            inp.StartCoroutine(RestoreColorAfter(img, orig, 0.4f));
        }
        private static System.Collections.IEnumerator RestoreColorAfter(Image img, Color orig, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (img != null) img.color = orig;
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────
        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        /// <summary>
        /// Load a sprite that works in BOTH the editor and a built APK:
        /// - First tries Resources.Load (works in both — assumes the asset is
        ///   copied/duplicated into an Assets/Resources/ folder).
        /// - Falls back to a Resources Texture2D wrapped in a runtime Sprite
        ///   (common — PNGs often import as Texture2D, not Sprite).
        /// - Editor-only fallback: AssetDatabase.LoadAssetAtPath against the
        ///   original asset path so the screen still works while a Resources
        ///   copy is being staged.
        /// </summary>
        private static Sprite LoadSpriteSafe(string resourcesPath, string editorAssetPath)
        {
            // Runtime + editor path — works in the built APK.
            var sp = Resources.Load<Sprite>(resourcesPath);
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>(resourcesPath);
            if (tex != null)
                return Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);

#if UNITY_EDITOR
            // Editor-only fallback if the Resources copy hasn't been imported yet.
            try
            {
                var dbSp = Sparq.Core.SpriteLoader.Load(editorAssetPath);
                if (dbSp != null) return dbSp;
                var dbTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(editorAssetPath);
                if (dbTex != null)
                    return Sprite.Create(dbTex,
                        new Rect(0, 0, dbTex.width, dbTex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            catch {}
#endif
            return null;
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
